[<AutoOpen>]
module EventHandler

open GlobalPollenProject.Core.Aggregates.Grain
open GlobalPollenProject.Core.Types
open ReadStore

open System

type EventHandler() =

    let readStore = new ReadContext()

    let unwrapId (GrainId e) = e
    let unwrapUrl (Url e) = e

    member this.Handle = function

        | GrainSubmitted event ->
            // Do file upload here using file upload service, to get thumbnail
            let thumbUrl = 
                match event.Images.Head with
                | SingleImage x -> x
                | FocusImage x -> x.Head

            readStore.GrainSummaries.Add { Id= unwrapId event.Id; Thumbnail= unwrapUrl thumbUrl } |> ignore
            readStore.SaveChanges() |> ignore
            printfn "Unknown grain submitted! It has %i images" event.Images.Length

        | GrainIdentified event ->
            printfn "Grain identitied"

        | GrainIdentityConfirmed event ->
            printfn "Grain identity confirmed"
