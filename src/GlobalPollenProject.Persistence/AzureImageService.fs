module AzureImageService

open ImageSharp
open Microsoft.Extensions.Options
open Microsoft.WindowsAzure.Storage
open Microsoft.WindowsAzure.Storage.Blob
open Microsoft.Extensions.Logging
open System.IO
open System
open GlobalPollenProject.Core.Types

let calcScale maxDimension height width =
    let scale = 
        match height,width with
        | h,w when h > w -> maxDimension / h
        | _ -> maxDimension / width
    match scale with
    | 0.
    | 1. -> 1.
    | _ -> scale

let base64ToByte base64 =
    let unwrap (Base64Image i) = i
    let unwrapped = unwrap base64
    Convert.FromBase64String(unwrapped)

let upload (blob:CloudBlockBlob) memoryStream = async {
    let! exists = blob.ExistsAsync() |> Async.AwaitIAsyncResult
    if exists then invalidOp "Blob already exists" 
    blob.UploadFromStreamAsync(memoryStream) |> Async.AwaitIAsyncResult |> ignore
    return Url blob.Uri.AbsoluteUri
}

let uploadImage (container:CloudBlobContainer) fileName (stream:Stream) = 
    let mutable image = new ImageSharp.Image (stream)
    let scale = calcScale 800. (float image.Height) (float image.Width)
    let mutable memoryStream = new MemoryStream()
    image.MetaData.Quality <- 256
    let memoryStream = new MemoryStream ()
    let h = (float image.Height) * scale
    let w = (float image.Width) * scale
    image.Resize(int h,int w).SaveAsPng(memoryStream) |> ignore
    memoryStream.Position <- int64(0)
    let blob = container.GetBlockBlobReference(fileName)
    upload blob memoryStream

let container connectionString name : CloudBlobContainer =
    let storageAccount = CloudStorageAccount.Parse connectionString
    let blobClient = storageAccount.CreateCloudBlobClient ()
    let container = blobClient.GetContainerReference name
    if (container.CreateIfNotExistsAsync() |> Async.AwaitIAsyncResult |> Async.RunSynchronously) 
    then () //container.SetPermissionsAsync(BlobContainerPermissions (PublicAccess = Blob }) |> Async.AwaitTask
    container

let uploadToAzure conName connString nameGenerator image =
    match image with
    | Single i -> 
        use s = new MemoryStream(base64ToByte i)
        let url = uploadImage (container connString conName) (nameGenerator()) s |> Async.RunSynchronously
        SingleImage (url)
    | Focus (stack,s,c) ->
        let urls = 
            stack 
            |> List.map (fun x -> use s = new MemoryStream(base64ToByte x)
                                  uploadImage (container connString conName) (nameGenerator()) s |> Async.RunSynchronously)
        FocusImage (urls,s,c)
