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

    let grainProjections = function
        | GrainSubmitted event ->
            // Do file upload here using file upload service, to get thumbnail
            let thumbUrl = 
                match event.Images.Head with
                | SingleImage x -> x
                | FocusImage (u,s,c) -> u.Head

            readStore.GrainSummaries.Add { Id= unwrapId event.Id; Thumbnail= unwrapUrl thumbUrl } |> ignore
            readStore.SaveChanges() |> ignore
            printfn "Unknown grain submitted! It has %i images" event.Images.Length

        | GrainIdentified event ->
            printfn "Grain identitied"

        | GrainIdentityConfirmed event ->
            printfn "Grain identity confirmed"

        | GrainIdentityChanged event ->
            printfn "Grain identity changed"

        | GrainIdentityUnconfirmed event ->
            printfn "This grain lost its ID!"


    eventStream
    |> Observable.choose (function (id,ev) -> filter<GlobalPollenProject.Core.Aggregates.Grain.Event> ev)
    |> Observable.subscribe grainProjections
