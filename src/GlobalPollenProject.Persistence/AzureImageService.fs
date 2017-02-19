module AzureImageService

//open ImageProcessorCore
// open Microsoft.Extensions.Options
// open Microsoft.WindowsAzure.Storage
// open Microsoft.WindowsAzure.Storage.Blob
// open Microsoft.Extensions.Logging
// open System.IO
// open GlobalPollenProject.Core.Types

type SavedImage = {
    Url: string
    ThumbnailUrl: string
}

let calcScale maxDimension height width =
    let scale = 
        match height,width with
        | h,w when h > w -> maxDimension / h
        | _ -> maxDimension / width
    match scale with
    | 0.
    | 1. -> 1.
    | _ -> scale

// let saveImage (container:CloudBlobContainer) maxDimension (stream:Stream) : Url =

//     let mutable newImage = new Image(stream)
//     let scale = calcScale 800. image.Height image.Width

//     let mutable memoryStream = new MemoryStream()
//     newImage.Quality <- 256
//     newImage.Resize(w * scale, h * scale).SaveAsPng(memStream)
//     memoryStream.Position <- 0

//     let blob = container.GetBlockBlobReference(saveFilePath)
//     if blob.ExistsAsync() then invalidOp "Blob already exists"
//     blob.UploadFromStreamAsync(memoryStream)
//     Url blob.Uri.AbsoluteUri


// let uploadImage base64 : SavedImage =



//     {Url = ""; ThumbnailUrl = ""}
