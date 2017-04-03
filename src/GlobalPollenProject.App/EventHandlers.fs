[<AutoOpen>]
module EventHandlers

open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.Aggregates.Grain
open GlobalPollenProject.Core.Aggregates.Taxonomy
open ReadStore

open System

let private filter<'TEvent> ev = 
    match box ev with
    | :? 'TEvent as tev -> Some tev
    | _ -> None

let grainProjections (eventStream:IObservable<string*obj>) =

    let readStore = new ReadContext()

    let unwrapId (GrainId e) = e

    let grainProjections = function
        | GrainSubmitted event ->
            // Do file upload here using file upload service, to get thumbnail
            let thumbUrl = 
                match event.Images.Head with
                | SingleImage x -> x
                | FocusImage (u,s,c) -> u.Head

            readStore.GrainSummaries.Add { Id= unwrapId event.Id; Thumbnail= Url.unwrap thumbUrl } |> ignore
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

let taxonomyProjections getTaxon (eventStream:IObservable<string*obj>) =

    let readStore = new ReadContext()

    let unwrapId (TaxonId e) = e

    let getParent getTaxon parentId : BackboneTaxon = 
        match parentId with
        | Some parent -> 
            match getTaxon parent with
            | Some parent -> parent
            | None -> invalidOp "There was no parent. Rebuild the projections database now."
        | None -> invalidOp "The taxon is a genus, but did not have a parent"

    let taxonomyProjections = function
        | Imported event ->

            let unwrapLatin (LatinName ln) = ln
            let unwrapId (TaxonId id) = id

            let family,genus,species,rank,ln =
                match event.Identity with
                | Family ln -> 
                    unwrapLatin ln,"","", "Family", unwrapLatin ln
                | Genus ln ->
                    let family = getParent getTaxon event.Parent
                    family.LatinName,unwrapLatin ln,"", "Genus", unwrapLatin ln
                | Species (ln,_,_) -> 
                    let genus = getParent getTaxon event.Parent
                    let family = getParent getTaxon (Some (TaxonId genus.Id) )
                    family.LatinName, genus.LatinName, unwrapLatin ln,"Species", unwrapLatin ln
            
            let reference, referenceUrl =
                match event.Reference with
                | None -> "", ""
                | Some r -> 
                    match r with
                    | ref,Some u -> ref,unwrap u
                    | ref,None -> ref,""

            let projection = {  Id = unwrapId event.Id
                                Family = family
                                Genus = genus
                                Species = species
                                LatinName = ln
                                Rank = rank
                                ReferenceName = reference
                                ReferenceUrl = referenceUrl }

            readStore.BackboneTaxa.AddAsync projection |> Async.AwaitTask |> Async.RunSynchronously |> ignore
            readStore.SaveChanges() |> ignore
            printfn "Taxon imported: %s" ln

        | EstablishedConnection event ->
            printfn "Taxon connected to Neotoma and GBIF"

    eventStream
    |> Observable.choose (function (id,ev) -> filter<GlobalPollenProject.Core.Aggregates.Taxonomy.Event> ev)
    |> Observable.subscribe taxonomyProjections