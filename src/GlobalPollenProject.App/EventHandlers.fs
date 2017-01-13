[<AutoOpen>]
module EventHandlers

open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.Aggregates.Grain
open ReadStore

open System

let private filter<'TEvent> ev = 
    match box ev with
    | :? 'TEvent as tev -> Some tev
    | _ -> None

let projectionEventHandler (eventStream:IObservable<string*obj>) =

    let readStore = new ReadContext()

    let unwrapId (GrainId e) = e
    let unwrapUrl (Url e) = e

    let subscriberFn = function
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


    eventStream
    |> Observable.choose (function (id,ev) -> filter<GlobalPollenProject.Core.Aggregates.Grain.Event> ev)
    |> Observable.subscribe subscriberFn 



// type EventHandler() =

//     let readStore = new ReadContext()

//     let unwrapId (GrainId e) = e
//     let unwrapUrl (Url e) = e

//     member this.Handle = function

//         | Grain e ->
//             match e with
//             | GrainSubmitted event ->
//                 // Do file upload here using file upload service, to get thumbnail
//                 let thumbUrl = 
//                     match event.Images.Head with
//                     | SingleImage x -> x
//                     | FocusImage x -> x.Head

//                 readStore.GrainSummaries.Add { Id= unwrapId event.Id; Thumbnail= unwrapUrl thumbUrl } |> ignore
//                 readStore.SaveChanges() |> ignore
//                 printfn "Unknown grain submitted! It has %i images" event.Images.Length

//             | GrainIdentified event ->
//                 printfn "Grain identitied"

//             | GrainIdentityConfirmed event ->
//                 printfn "Grain identity confirmed"

//         | _ -> () // Ignore other aggregates