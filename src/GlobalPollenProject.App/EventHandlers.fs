[<AutoOpen>]
module EventHandlers

open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.Aggregates.Grain
open GlobalPollenProject.Core.Aggregates.Taxonomy
open GlobalPollenProject.Core.Aggregates.ReferenceCollection
open ReadStore

open System
open System.Collections.Generic
open Microsoft.EntityFrameworkCore

let private filter<'TEvent> ev = 
    match box ev with
    | :? 'TEvent as tev -> Some tev
    | _ -> None

let projectionHandler (readStore:EntityFramework.ReadContext) getTaxon getTaxonByName (eventStream:IObservable<string*obj>) =

    // ID Unwrappers
    let unwrapGrainId (GrainId e) = e
    let unwrapTaxonId (TaxonId e) = e
    let unwrapUserId (UserId e) = e
    let unwrapRefId (CollectionId e) = e
    let unwrapSlideId (SlideId (e,f)) = e,f
    let unwrapLatin (LatinName ln) = ln
    let unwrapId (TaxonId id) = id
    let unwrapEph (SpecificEphitet e) = e
    let unwrapAuthor (Scientific a) = a

    let grain = function
        | GrainSubmitted event ->
            // Do file upload here using file upload service, to get thumbnail
            let thumbUrl = 
                match event.Images.Head with
                | SingleImage x -> x
                | FocusImage (u,s,c) -> u.Head

            readStore.GrainSummary.Add { Id= unwrapGrainId event.Id; Thumbnail= Url.unwrap thumbUrl } |> ignore
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

    let taxonomy = function
        | Imported event ->

            let getParent getTaxon parentId : BackboneTaxon = 
                match parentId with
                | Some parent -> 
                    match getTaxon parent with
                    | Some parent -> parent
                    | None -> invalidOp "There was no parent. Rebuild the projections database now."
                | None -> invalidOp "The taxon is a genus, but did not have a parent"

            let family,genus,species,rank,ln,namedBy =
                match event.Identity with
                | Family ln -> 
                    unwrapLatin ln,"","", "Family", unwrapLatin ln,""
                | Genus ln ->
                    let family = getParent getTaxon event.Parent
                    family.LatinName,unwrapLatin ln,"", "Genus", unwrapLatin ln,""
                | Species (g,s,n) -> 
                    let species = sprintf "%s %s" (unwrapLatin g) (unwrapEph s)
                    let genus = getParent getTaxon event.Parent
                    let family = getTaxonByName genus.Family "Family" ""
                    match family with
                    | Some f -> f.LatinName, genus.LatinName, species,"Species", species,unwrapAuthor n
                    | None -> invalidOp "The backbone taxonomy is corrupted. Rebuild required."
            
            let reference, referenceUrl =
                match event.Reference with
                | None -> "", ""
                | Some r -> 
                    match r with
                    | ref,Some u -> ref,unwrap u
                    | ref,None -> ref,""

            let projection = {  Id = unwrapTaxonId event.Id
                                Family = family
                                Genus = genus
                                Species = species
                                LatinName = ln
                                NamedBy = namedBy
                                Rank = rank
                                ReferenceName = reference
                                ReferenceUrl = referenceUrl }

            readStore.BackboneTaxon.AddAsync projection |> Async.AwaitTask |> Async.RunSynchronously |> ignore
            readStore.SaveChanges() |> ignore
            printfn "Taxon imported: %s" ln

        | EstablishedConnection event ->
            printfn "Taxon connected to Neotoma and GBIF"

    let reference = function
        | DigitisationStarted e -> 
            readStore.ReferenceCollectionSummary.AddAsync { 
                Id = unwrapRefId e.Id
                User = unwrapUserId e.Owner
                Name = e.Name
                Description = e.Description
                SlideCount = 0 } |> Async.AwaitTask |> Async.RunSynchronously |> ignore
            readStore.ReferenceCollection.AddAsync {
                Id = unwrapRefId e.Id
                User = unwrapUserId e.Owner
                Name = e.Name
                Status = "Draft"
                Version = 1
                Description = e.Description
                Slides = List<Slide>() } |> Async.AwaitTask |> Async.RunSynchronously |> ignore
            readStore.SaveChanges() |> ignore

        | CollectionPublished cid -> printfn ""
        | SlideRecorded slide -> 
            let colId (SlideId (colId,slideId)) = colId
            let col = readStore.ReferenceCollection.Include(fun x -> x.Slides) |> Seq.find (fun c -> c.Id = unwrapRefId (colId slide.Id))
            let backboneTaxon =
                let id =
                    match slide.Taxon with
                    | Botanical id -> id
                    | Environmental id -> id
                    | Morphological id -> id
                let taxon = readStore.BackboneTaxon |> Seq.tryFind (fun x -> x.Id = (unwrapTaxonId id))
                match taxon with
                | Some t -> t
                | None -> invalidOp "Taxon was not submitted with the slide"
            let newSlide = { Id = Guid.NewGuid()
                             CollectionId = unwrapRefId <| colId slide.Id
                             CollectionSlideId = "SL01"
                             Taxon = Unchecked.defaultof<TaxonSummary>
                             IdentificationMethod = "Botanical"
                             FamilyOriginal = backboneTaxon.Family
                             GenusOriginal = backboneTaxon.Genus
                             SpeciesOriginal = backboneTaxon.Species
                             Images = List<SlideImage>()
                             IsFullyDigitised = false }
            col.Slides.Add(newSlide)
            // let updated = { col with Slides = List<SlideImage>(col.Slides |> Seq.append [|newSlide|]) }
            readStore.ReferenceCollection.Update(col) |> ignore
            readStore.SaveChanges() |> ignore

        | SlideGainedIdentity (slideId, taxonId) ->
            let col = readStore.ReferenceCollection.Include(fun x -> x.Slides) |> Seq.find (fun c -> c.Id = unwrapRefId (fst (unwrapSlideId slideId)))
            let backboneTaxon = readStore.BackboneTaxon |> Seq.find (fun t -> t.Id = unwrapTaxonId taxonId)
            let existingGppTaxon = readStore.TaxonSummary |> Seq.tryFind (fun t -> t.Id = unwrapTaxonId taxonId)
            let taxon = 
                match existingGppTaxon with
                | Some t -> t
                | None ->
                    let gppTaxon = {
                        Id = backboneTaxon.Id
                        Family = backboneTaxon.Family
                        Genus = backboneTaxon.Genus
                        Species = backboneTaxon.Species
                        LatinName = backboneTaxon.LatinName
                        Rank = backboneTaxon.Rank
                        SlideCount = 1
                        GrainCount = 0
                        ThumbnailUrl = "" }
                    readStore.TaxonSummary.Add(gppTaxon) |> ignore
                    gppTaxon
            // let slide = col.Slides |> Seq.find (fun x -> x.CollectionSlideId = snd (unwrapSlideId slideId))
            // let updatedSlideList = col.Slides |> List.map (fun x -> if x.CollectionSlideId = snd (unwrapSlideId slideId) then { x with Taxon = taxon } else x)
            // readStore.ReferenceCollection.Update( { col with Slides = updatedSlideList } ) |> ignore
            readStore.SaveChanges() |> ignore

        | SlideImageUploaded (id,img) -> printfn ""
        | SlideFullyDigitised slideId -> printfn ""

    let project (e:string*obj) = 
        match snd e with
        | :? GlobalPollenProject.Core.Aggregates.Grain.Event as e -> grain e
        | :? GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event as e -> reference e
        | :? GlobalPollenProject.Core.Aggregates.Taxonomy.Event as e -> taxonomy e
        | _ -> ()

    eventStream
    |> Observable.subscribe project
