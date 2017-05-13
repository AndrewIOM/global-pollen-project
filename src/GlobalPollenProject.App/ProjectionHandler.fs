module GlobalPollenProject.App.ProjectionHandler

open System
open System.Collections.Generic
open GlobalPollenProject.Core.DomainTypes
open ReadStore
open ReadModels

module UnknownGrainProjections =

    open GlobalPollenProject.Core.Aggregates.Grain

    let submit setReadModel (e:GrainSubmitted) =
        let thumbUrl = 
            match e.Images.Head with
            | SingleImage x -> x
            | FocusImage (u,s,c) -> u.Head
        let summary = { Id = Converters.DomainToDto.unwrapGrainId e.Id; Thumbnail = Url.unwrap thumbUrl }
        ReadStore.RepositoryBase.setSingle (summary.Id.ToString()) summary |> ignore

    let identified e =
        ()

    let identityChanged e = 
        ()

    let route set = function
    | GrainSubmitted e -> submit set e
    | GrainIdentified e -> identified e
    | GrainIdentityConfirmed e -> identityChanged e
    | GrainIdentityChanged e -> identityChanged e
    | GrainIdentityUnconfirmed e -> identityChanged e


module ReferenceMaterialProjections =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection

    let start e =
        // let summary = { Id = unwrapRefId e.Id; User = unwrapUserId e.Owner; Name = e.Name; Description = e.Description; SlideCount = 0 }
        // let detail = { Id = unwrapRefId e.Id; User = unwrapUserId e.Owner; Name = e.Name; Status = "Draft"; Version = 1; Description = e.Description; Slides = [] }
        // setKey (unwrapRefId e.Id) summary
        // setKey (unwrapRefId e.Id) detail
        ()

    let publish e =
        ()

    let recordSlide e =
        ()
    //     let colId (SlideId (colId,slideId)) = colId
    //     let col = readStore.ReferenceCollection.Include(fun x -> x.Slides) |> Seq.find (fun c -> c.Id = unwrapRefId (colId slide.Id))
    //     let backboneTaxon =
    //         let id =
    //             match slide.Taxon with
    //             | Botanical id -> id
    //             | Environmental id -> id
    //             | Morphological id -> id
    //         let taxon = readStore.BackboneTaxon |> Seq.tryFind (fun x -> x.Id = (unwrapTaxonId id))
    //         match taxon with
    //         | Some t -> t
    //         | None -> invalidOp "Taxon was not submitted with the slide"
    //     let newSlide = { Id = Guid.NewGuid()
    //                         CollectionId = unwrapRefId <| colId slide.Id
    //                         CollectionSlideId = "SL01"
    //                         Taxon = Unchecked.defaultof<TaxonSummary>
    //                         IdentificationMethod = "Botanical"
    //                         FamilyOriginal = backboneTaxon.Family
    //                         GenusOriginal = backboneTaxon.Genus
    //                         SpeciesOriginal = backboneTaxon.Species
    //                         Images = List<SlideImage>()
    //                         IsFullyDigitised = false }
    //     col.Slides.Add(newSlide)
    //     // let updated = { col with Slides = List<SlideImage>(col.Slides |> Seq.append [|newSlide|]) }
    //     readStore.ReferenceCollection.Update(col) |> ignore
    //     readStore.SaveChanges() |> ignore

    let slideImage slideId image =
        ()

    let markDigitised slideId =
        ()

    let slideIdentityChanged slideId taxonId =
        ()
    // | SlideGainedIdentity (slideId, taxonId) ->
    //     let col = readStore.ReferenceCollection.Include(fun x -> x.Slides) |> Seq.find (fun c -> c.Id = unwrapRefId (fst (unwrapSlideId slideId)))
    //     let backboneTaxon = readStore.BackboneTaxon |> Seq.find (fun t -> t.Id = unwrapTaxonId taxonId)
    //     let existingGppTaxon = readStore.TaxonSummary |> Seq.tryFind (fun t -> t.Id = unwrapTaxonId taxonId)
    //     let taxon = 
    //         match existingGppTaxon with
    //         | Some t -> t
    //         | None ->
    //             let gppTaxon = {
    //                 Id = backboneTaxon.Id
    //                 Family = backboneTaxon.Family
    //                 Genus = backboneTaxon.Genus
    //                 Species = backboneTaxon.Species
    //                 LatinName = backboneTaxon.LatinName
    //                 Rank = backboneTaxon.Rank
    //                 SlideCount = 1
    //                 GrainCount = 0
    //                 ThumbnailUrl = "" }
    //             readStore.TaxonSummary.Add(gppTaxon) |> ignore
    //             gppTaxon
    //     // let slide = col.Slides |> Seq.find (fun x -> x.CollectionSlideId = snd (unwrapSlideId slideId))
    //     // let updatedSlideList = col.Slides |> List.map (fun x -> if x.CollectionSlideId = snd (unwrapSlideId slideId) then { x with Taxon = taxon } else x)
    //     // readStore.ReferenceCollection.Update( { col with Slides = updatedSlideList } ) |> ignore
    //     readStore.SaveChanges() |> ignore


    let route = function
    | DigitisationStarted e -> start e
    | CollectionPublished e -> publish e
    | SlideRecorded e -> recordSlide e
    | SlideImageUploaded (id,image) -> slideImage id image
    | SlideFullyDigitised sid -> markDigitised sid
    | SlideGainedIdentity (sid,tid) -> slideIdentityChanged sid tid


module TaxonomicSystemProjections =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let addToBackbone getKey setKey setSortedList serialise deserialise (event:Imported) =

        let getById id =
            match id with
            | Some id ->
                let u : Guid = Converters.DomainToDto.unwrapTaxonId id
                match ReadStore.RepositoryBase.getSingle<BackboneTaxon> (u.ToString()) getKey deserialise with
                | Ok t -> t
                | Error e -> invalidOp "A parent was required but not found. The read model may be corrupted"
            | None -> invalidOp "A parent was required but not found. The read model may be corrupted"

        let getFamily familyName =
            ReadStore.TaxonomicBackbone.tryFindByLatinName familyName None None getKey deserialise

        let reference, referenceUrl =
            match event.Reference with
            | None -> "", ""
            | Some r -> 
                match r with
                | ref,Some u -> ref,unwrap u
                | ref,None -> ref,""

        let family,genus,species,rank,ln,namedBy =
            match event.Identity with
            | Family ln -> 
                Converters.DomainToDto.unwrapLatin ln,"","", "Family", Converters.DomainToDto.unwrapLatin ln,""
            | Genus ln ->
                let family = getById event.Parent
                family.LatinName,Converters.DomainToDto.unwrapLatin ln,"", "Genus", Converters.DomainToDto.unwrapLatin ln,""
            | Species (g,s,n) -> 
                let species = sprintf "%s %s" (Converters.DomainToDto.unwrapLatin g) (Converters.DomainToDto.unwrapEph s)
                let genus = getById event.Parent
                let family = getFamily genus.Family
                match family with
                | Ok f ->
                    f.LatinName, genus.LatinName, species,"Species", species,Converters.DomainToDto.unwrapAuthor n
                | Error e -> invalidOp "A parent was required but not found. The read model may be corrupted"

        let projection = 
            {   Id = Converters.DomainToDto.unwrapTaxonId event.Id
                Family = family
                Genus = genus
                Species = species
                LatinName = ln
                NamedBy = namedBy
                Rank = rank
                ReferenceName = reference
                ReferenceUrl = referenceUrl }
        ReadStore.TaxonomicBackbone.import setKey setSortedList serialise projection |> ignore
        printfn "ReadModel: Imported %s" projection.LatinName

    let link e =
        ()

    let route getKey setKey setList serialise deserialise = function
    | Imported e -> addToBackbone getKey setKey setList serialise deserialise e
    | EstablishedConnection e -> link e

// TODO Remove this reference to serialisation
let deserialise<'a> json = 
    let unwrap (ReadStore.Json j) = j
    Serialisation.deserialiseCli<'a> (unwrap json)

let serialise s = 
    let result = Serialisation.serialiseCli s
    match result with
    | Ok r -> Ok <| ReadStore.Json r
    | Error e -> Error e
// END of TODO

let router (get:GetFromKeyValueStore) (set:SetStoreValue) (setSortedList:SetEntryInSortedList) (e:string * obj) =
    match snd e with
    | :? GlobalPollenProject.Core.Aggregates.Grain.Event as e -> UnknownGrainProjections.route set e
    | :? GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event as e -> ReferenceMaterialProjections.route e
    | :? GlobalPollenProject.Core.Aggregates.Taxonomy.Event as e -> TaxonomicSystemProjections.route get set setSortedList serialise deserialise e
    | _ -> ()
