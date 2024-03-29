module AzureImageStore

open SixLabors.ImageSharp
open Microsoft.Azure.Storage
open Microsoft.Azure.Storage.Blob
open System.IO
open System
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Composition
open SixLabors.ImageSharp.PixelFormats
open SixLabors.ImageSharp.Processing

let calcScale maxDimension height width =
    let scale = 
        match height,width with
        | h,w when h > w -> maxDimension / h
        | _ -> maxDimension / width
    match scale with
    | 0.
    | 1. -> 1.
    | i when i > 1. -> 1.
    | _ -> scale

let base64ToByte base64 =
    let unwrap (Base64Image i) = i
    match unwrap base64 with
    | Prefix "data:image/png;base64," b64 -> Convert.FromBase64String b64 |> Ok
    | _ -> Error "Base 64 was not a PNG image. Only PNGs are supported"

let uploadFromStream (blob:CloudBlockBlob) stream = async {
    let! success = blob.UploadFromStreamAsync(stream) |> Async.AwaitIAsyncResult
    match success with
    | true -> return Url.create blob.Uri.AbsoluteUri |> Ok
    | false -> 
        printfn "The image did not successfully upload to Azure %A" blob
        return "The image did not successfully upload to Azure" |> Error }

let getContainer connectionString name =
    let storageAccount = CloudStorageAccount.Parse connectionString
    let blobClient = storageAccount.CreateCloudBlobClient ()
    let container = blobClient.GetContainerReference name
    if (container.CreateIfNotExistsAsync() |> Async.AwaitIAsyncResult |> Async.RunSynchronously) 
    then ()//container.SetPermissionsAsync(BlobContainerPermissions (PublicAccess = Blob })) |> Async.AwaitTask
    container

let getBlob (container:CloudBlobContainer) fileName = 
    container.GetBlockBlobReference(fileName)

let scaleImage maxDimension (stream:Stream) =
    use image = Image.Load<PixelFormats.Rgb24>(stream)
    let resizeRatio = calcScale maxDimension (float image.Height) (float image.Width)
    let memoryStream = new MemoryStream ()
    let h = (float image.Height) * resizeRatio
    let w = (float image.Width) * resizeRatio
    image.Mutate(fun i -> i.Resize(int w, int h) |> ignore)
    image.SaveAsPng(memoryStream)
    memoryStream.Position <- int64(0)
    memoryStream

let pngTojpg (stream:Stream) =
    use image = Image.Load<PixelFormats.Rgb24>(stream)
    let memoryStream = new MemoryStream ()
    image.SaveAsJpeg(memoryStream) |> ignore
    memoryStream.Position <- int64(0)
    memoryStream

let getImageDimensions' (stream:Stream) =
    use image = Image.Load<Rgba32>(stream)
    image.Height,image.Width

let getImageDimensions base64 =
    base64
    |> base64ToByte
    |> lift (fun x -> new MemoryStream(x))
    |> lift getImageDimensions'

let getScaleFactor' maxDimension (stream:Stream) =
    use image = Image.Load<PixelFormats.Rgb24>(stream)
    calcScale maxDimension (float image.Height) (float image.Width)

let getScaleFactor maxDimension base64 =
    base64
    |> base64ToByte
    |> lift (fun x -> new MemoryStream(x))
    |> lift (getScaleFactor' maxDimension)

let scaleFloatingCal scaleFactor (fc:FloatingCalibration) =
    let p1 = float (fst fc.Point1) * scaleFactor |> int, float (snd fc.Point1) * scaleFactor |> int
    let p2 = float (fst fc.Point2) * scaleFactor |> int, float (snd fc.Point2) * scaleFactor |> int
    { fc with Point1 = p1; Point2 = p2}

let uploadToAzure' maxResolution blobRef baseUrl base64 =
    base64ToByte base64
    |> lift (fun x -> new MemoryStream(x))
    |> lift (scaleImage maxResolution)
    |> bind (fun x -> uploadFromStream blobRef x |> Async.RunSynchronously)
    |> bind (Url.createRelative baseUrl)

let uploadToAzure baseUrl conName connString generateName (image:ImageForUpload) =
    let container = getContainer connString conName
    let blobRef (frame:int) = 
        (generateName() + "_" + (frame.ToString()) + ".png")
        |> getBlob container
    match image with
    | ImageForUpload.Single (i,floatingCal) ->
        let createSingle url scaleFactor = SingleImage (url, scaleFloatingCal scaleFactor floatingCal)
        createSingle
        <!> uploadToAzure' 2000. (blobRef 1) baseUrl i
        <*> getScaleFactor 2000. i
    | ImageForUpload.Focus (b64s,stepping,magId) ->
        let frames = b64s |> List.length
        let imgs =
            b64s
            |> List.mapi (fun frame img -> uploadToAzure' 2000. (blobRef frame) baseUrl img)
            |> List.choose (fun x -> match x with | Ok o -> Some o | Error e -> None )
        match imgs.Length with
        | i when i = frames -> Image.FocusImage (imgs,stepping,magId) |> Ok
        | _ -> "Couldn't upload image - not all frames were succesfully saved" |> Error

let toCachedName size (name:string) =
    name.Replace(".png","_" + size + ".jpg")

let toBlobName containerName (relative:string) =
    relative.Replace("/" + containerName + "/", "")

/// Returns the image url and the scale factor applied as a tuple
let generateCacheImage originalContainerName cacheContainerName connString maxDimension sizeName (fullSizeFile:RelativeUrl) =    
    let originalContainer = getContainer connString originalContainerName
    let fullSizeBlobRef = fullSizeFile |> Url.unwrapRelative |> toBlobName originalContainerName |> getBlob originalContainer
    let exists = fullSizeBlobRef.ExistsAsync() |> Async.AwaitTask |> Async.RunSynchronously
    match exists with
    | false -> "The specified file does not exist in Azure: " + fullSizeBlobRef.Name |> Error
    | true ->
        use memoryStream = new MemoryStream()
        fullSizeBlobRef.DownloadToStreamAsync(memoryStream) |> Async.AwaitTask |> Async.RunSynchronously
        memoryStream.Position <- int64(0)
        let cacheContainer = getContainer connString cacheContainerName 
        let thumbBlob = fullSizeFile |> Url.unwrapRelative |> toBlobName originalContainerName |> toCachedName sizeName |> getBlob cacheContainer
        printfn "Saving cache image... [%s]" thumbBlob.Name
        let scaleFactor = memoryStream |> getScaleFactor' maxDimension
        memoryStream.Position <- int64(0)
        memoryStream
        |> scaleImage maxDimension
        |> pngTojpg
        |> uploadFromStream thumbBlob
        |> Async.RunSynchronously
        |> lift (fun r -> r,scaleFactor)