module GlobalPollenProject.App.ProjectionHandler

open System
open System.Collections.Generic
open GlobalPollenProject.Core.DomainTypes
open ReadStore
open ReadModels

let readModelErrorHandler() =
    invalidOp "The read model is corrupt or out-of-sync. Rebuild now."

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

module UnknownGrainProjections =

    open GlobalPollenProject.Core.Aggregates.Grain

    let submit setReadModel (e:GrainSubmitted) =
        let thumbUrl = 
            match e.Images.Head with
            | SingleImage x -> x
            | FocusImage (u,s,c) -> u.Head

        let summary = { 
            Id = Converters.DomainToDto.unwrapGrainId e.Id 
            Thumbnail = Url.unwrap thumbUrl
            HasTaxonomicIdentity = false }

        let detail = {
            Id = Converters.DomainToDto.unwrapGrainId e.Id
            Images = [ { Url = "https://acm.im/cool.png" } ]
            FocusImages = []
            Identifications = []
            ConfirmedFamily = ""
            ConfirmedGenus = ""
            ConfirmedSpecies = "" }

        ReadStore.RepositoryBase.setSingle (summary.Id.ToString()) summary |> ignore
        ReadStore.RepositoryBase.setSingle (detail.Id.ToString()) detail |> ignore

    let identified e =

        // Load detail read model for unknown grain
        // Add this identification as a morphological identification
        ()

    let identityChanged e = 

        // match e with
        // | GrainIdentityConfirmed ce ->
        //     ce.Taxon
        // | -> ()
        ()

    let route set = function
    | GrainSubmitted e -> submit set e
    | GrainIdentified e -> identified e
    | GrainIdentityConfirmed e -> identityChanged e
    | GrainIdentityChanged e -> identityChanged e
    | GrainIdentityUnconfirmed e -> identityChanged e


module ReferenceMaterialProjections =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection


    let start set serialise (e:DigitisationStarted) =
        let refId : Guid = Converters.DomainToDto.unwrapRefId e.Id
        let userId = Converters.DomainToDto.unwrapUserId e.Owner
        let summary = { Id = refId; User = userId; Name = e.Name; Description = e.Description; SlideCount = 0 }
        let detail = { Id = refId; User = userId; Name = e.Name; Status = "Draft"; Version = 1; Description = e.Description; Slides = [] }
        ReadStore.RepositoryBase.setSingle (refId.ToString()) summary set serialise |> ignore
        ReadStore.RepositoryBase.setSingle (refId.ToString()) detail set serialise |> ignore

    let publish get deserialise e =
        // Make all of the public-facing read models here

        ()

    let recordSlide get setSingle (e:SlideRecorded) =

        // Validate Taxon and get details
        let backboneTaxon =
            let id = 
                match e.Taxon with
                | Botanical id -> id
                | Environmental id -> id
                | Morphological id -> id
            let taxon = ReadStore.TaxonomicBackbone.getById id get deserialise 
            match taxon with
            | Ok t -> t
            | Error e -> readModelErrorHandler()

        // Get collection and add new slide
        let colId : Guid = Converters.DomainToDto.unwrapSlideId e.Id |> fst |> Converters.DomainToDto.unwrapRefId
        let stale = ReadStore.RepositoryBase.getSingle<ReferenceCollection> (colId.ToString()) get deserialise
        match stale with
        | Error e -> readModelErrorHandler()
        | Ok rc ->
            let slide = rc.Slides |> List.tryFind (fun x -> x.CollectionSlideId = (Converters.DomainToDto.unwrapSlideId e.Id |> snd))
            match slide with
            | None -> readModelErrorHandler()
            | Some s ->
                let newSlide : Slide = {    Id = Guid.NewGuid()
                                            CollectionId = colId
                                            CollectionSlideId = e.Id |> Converters.DomainToDto.unwrapSlideId |> snd
                                            Taxon = None
                                            IdentificationMethod = "Botanical" //TODO remove hard-coding here
                                            FamilyOriginal = backboneTaxon.Family
                                            GenusOriginal = backboneTaxon.Genus
                                            SpeciesOriginal = backboneTaxon.Species
                                            Images = []
                                            IsFullyDigitised = false }
                
                let withSlide = { rc with Slides = newSlide :: rc.Slides }
                ReadStore.RepositoryBase.setSingle (colId.ToString()) withSlide setSingle serialise |> ignore

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


    let route getKey setKey setList serialise deserialise = function
    | DigitisationStarted e -> start setKey serialise e
    | CollectionPublished e -> publish getKey deserialise e
    | SlideRecorded e -> recordSlide getKey setKey e
    | SlideImageUploaded (id,image) -> slideImage id image
    | SlideFullyDigitised sid -> markDigitised sid
    | SlideGainedIdentity (sid,tid) -> slideIdentityChanged sid tid


module TaxonomicSystemProjections =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let getById getKey id =
        match id with
        | Some id ->
            let u : Guid = Converters.DomainToDto.unwrapTaxonId id
            match ReadStore.RepositoryBase.getSingle<BackboneTaxon> (u.ToString()) getKey deserialise with
            | Ok t -> t
            | Error e -> readModelErrorHandler()
        | None -> readModelErrorHandler()

    let addToBackbone getKey getListKey setKey setSortedList serialise deserialise (event:Imported) =

        let getFamily familyName =
            ReadStore.TaxonomicBackbone.tryFindByLatinName familyName None None getListKey getKey deserialise

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
                let family = getById getKey event.Parent
                family.LatinName,Converters.DomainToDto.unwrapLatin ln,"", "Genus", Converters.DomainToDto.unwrapLatin ln,""
            | Species (g,s,n) -> 
                let species = sprintf "%s %s" (Converters.DomainToDto.unwrapLatin g) (Converters.DomainToDto.unwrapEph s)
                let genus = getById getKey event.Parent
                let family = getFamily genus.Family
                match family with
                | Ok f ->
                    f.LatinName, genus.LatinName, species,"Species", species,Converters.DomainToDto.unwrapAuthor n
                | Error e -> readModelErrorHandler()

        let status,alias =
            match event.Status with
            | Accepted -> "accepted",""
            | Doubtful -> "doubtful",""
            | Misapplied id -> "misapplied",id.ToString()
            | Synonym id -> "synonym",id.ToString()

        let projection = 
            {   Id = Converters.DomainToDto.unwrapTaxonId event.Id
                Family = family
                Genus = genus
                Species = species
                LatinName = ln
                NamedBy = namedBy
                TaxonomicStatus = status
                TaxonomicAlias = alias
                Rank = rank
                ReferenceName = reference
                ReferenceUrl = referenceUrl }
        ReadStore.TaxonomicBackbone.import setKey setSortedList serialise projection |> ignore
        printfn "ReadModel: Imported %s" projection.LatinName

    let link e =
        ()

    let route getKey getListKey setKey setList serialise deserialise = function
    | Imported e -> addToBackbone getKey getListKey setKey setList serialise deserialise e
    | EstablishedConnection e -> link e

let router (get:GetFromKeyValueStore) (getList:GetListFromKeyValueStore) (set:SetStoreValue) (setSortedList:SetEntryInSortedList) (e:string * obj) =
    match snd e with
    | :? GlobalPollenProject.Core.Aggregates.Grain.Event as e -> UnknownGrainProjections.route set e
    | :? GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event as e -> ReferenceMaterialProjections.route get set setSortedList serialise deserialise e
    | :? GlobalPollenProject.Core.Aggregates.Taxonomy.Event as e -> TaxonomicSystemProjections.route get getList set setSortedList serialise deserialise e
    | _ -> ()
