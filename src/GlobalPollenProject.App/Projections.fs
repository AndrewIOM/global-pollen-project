module GlobalPollenProject.App.Projections

open System
open System.Collections.Generic
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregates
open GlobalPollenProject.Core.Composition
open ReadStore
open ReadModels

let readModelErrorHandler() =
    invalidOp "The read model is corrupt or out-of-sync. Rebuild now."

let deserialise<'a> json = 
    let unwrap (ReadStore.Json j) = j
    Serialisation.deserialiseCli<'a> (unwrap json)

let serialise s = 
    let result = Serialisation.serialiseCli s
    match result with
    | Ok r -> Ok <| ReadStore.Json r
    | Error e -> Error e

module Checkpoint =

    let init setKey =
        ReadStore.RepositoryBase.setKey 0 "Checkpoint" setKey serialise

    let getCurrentVersion getKey =
        ReadStore.RepositoryBase.getKey "Checkpoint" getKey deserialise<int>

    let increment getKey setKey () =
        let incrementCheck current = 
            ReadStore.RepositoryBase.setKey (current + 1) "Checkpoint" setKey serialise
            |> Result.bind (fun x -> Ok (current + 1))
        getCurrentVersion getKey
        |> Result.bind incrementCheck


module GrainLocation =

    let insertLocation (submitted:Grain.GrainSubmitted) =
        // Convert domain to dto (spatial)
        // Add to redis list
        Ok()

    let handle (e:string*obj) =
        match snd e with
        | :? Grain.Event as e ->
            match e with
            | Grain.Event.GrainSubmitted s -> insertLocation s
            | _ -> Ok()
        | _ -> Ok()

module Statistics =

    // Statistic:GrainTotal
    // Statistic:SlideTotal
    // Statistic:Representation:Families:gppCount
    // Statistic:Representation:Families:backboneCount
    // Statistic:BackboneTaxa:Total

    // Should have a statistic for each taxonomic group (family, genus) - current/total

    // let init get set =
    //     RepositoryBase.

    let x = 2.

module MasterReferenceCollection =

    // TaxonSummary
    // TaxonDetail

    // TaxonSummary index
    // Custom TaxonSummary index that only contains those with fully digitised slides

    let initTaxonSummary (backboneTaxon:BackboneTaxon) : TaxonSummary =
        {
            Id = backboneTaxon.Id
            Family = backboneTaxon.Family
            Genus = backboneTaxon.Genus
            Species = backboneTaxon.Species
            LatinName = backboneTaxon.LatinName
            Authorship = backboneTaxon.NamedBy
            Rank = backboneTaxon.Rank
            SlideCount = 0
            GrainCount = 0
            ThumbnailUrl = ""
        }

    let initTaxonDetail (backboneTaxon:BackboneTaxon) parent : TaxonDetail =
        {
            Id = backboneTaxon.Id
            Family = backboneTaxon.Family
            Genus = backboneTaxon.Genus
            Species = backboneTaxon.Species
            LatinName = backboneTaxon.LatinName
            Authorship = backboneTaxon.NamedBy
            Rank = backboneTaxon.Rank
            Slides = []
            Grains = []
            Parent = parent
            Children = []
        }

    let getBackboneParent getSortedListKey getKey deserialise backboneTaxon =
        match backboneTaxon.Rank with
        | "family" -> None |> Ok
        | "genus" -> 
            ReadStore.TaxonomicBackbone.tryFindByLatinName backboneTaxon.Family (Some backboneTaxon.Genus) None getSortedListKey getKey deserialise
            |> lift (fun x -> Some { Id = x.Id ; Name = x.LatinName })
        | "species" ->
            ReadStore.TaxonomicBackbone.tryFindByLatinName backboneTaxon.Family (Some backboneTaxon.Genus) (Some backboneTaxon.Species) getSortedListKey getKey deserialise
            |> lift (fun x -> Some { Id = x.Id ; Name = x.LatinName })
        | _ -> Error "Invalid taxonomic rank"

    let importTaxon taxonId getBackboneTaxon set getSortedListKey getKey deserialise =
        let save r = RepositoryBase.setSingle taxonId r set serialise
        let saveSummary =
            getBackboneTaxon taxonId
            |> lift initTaxonSummary
            |> bind save
        let saveDetail =
            getBackboneTaxon taxonId
            |> lift initTaxonDetail
            |> bind save
        saveSummary |> ignore
        saveDetail

    let removeTaxonIfEmpty taxonId getBackboneTaxon =
        getBackboneTaxon taxonId
        // Fetch taxon
        // Check if empty
        // If true, remove it (how to remove redis key?)
        Ok()


    let assignSlideToTaxon taxonId slideId =
        // Create taxon if doesn't exist and get it
        // Increment taxonSummary slide count
        // Increment all parent node taxonSummary slide counts
        // 
        Ok()

    let assignGrainToTaxon taxonId grain =
        // 
        Ok()

    let removeSlideFromTaxon taxonId slideId =
        Ok()

    let removeGrainFromTaxon taxonId slideId =
        Ok()


    let handle (e:string*obj) =
        match snd e with
        | :? ReferenceCollection.Event as e ->
            match e with
            | ReferenceCollection.Event.CollectionPublished (id,date,ver) -> invalidOp "Cool" //Get collection and push all slides to appropriate taxa (recursively)
            | _ -> Ok()
        | :? Grain.Event as e ->
            match e with
            | Grain.Event.GrainIdentityConfirmed e -> assignGrainToTaxon e.Taxon e.Id
            | Grain.Event.GrainIdentityChanged e -> invalidOp "Help" //Get current taxon and remove grain from this taxon. Assign to new taxon.
            | Grain.Event.GrainIdentityUnconfirmed e -> invalidOp "Help" //Get current taxon and remove grain from this taxon
            | _ -> Ok()
        | _ -> Ok()


module Grain =

    open GlobalPollenProject.Core.Aggregates.Grain

    let submit setReadModel (e:GrainSubmitted) =
        let thumbUrl = 
            match e.Images.Head with
            | SingleImage x -> x
            | FocusImage (u,s,c) -> u.Head

        let summary = { 
            Id = Converters.DomainToDto.unwrapGrainId e.Id 
            Thumbnail = Url.unwrap thumbUrl }

        let detail = {
            Id = Converters.DomainToDto.unwrapGrainId e.Id
            Images = [ { Url = "https://acm.im/cool.png" } ]
            FocusImages = []
            Identifications = []
            ConfirmedFamily = ""
            ConfirmedGenus = ""
            ConfirmedSpecies = "" }

        ReadStore.RepositoryBase.setSingle (summary.Id.ToString()) summary setReadModel serialise |> ignore
        ReadStore.RepositoryBase.setSingle (detail.Id.ToString()) detail setReadModel serialise

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

    let handle set (e:string*obj) =
        match snd e with
        | :? Grain.Event as e ->
            match e with
            | Grain.Event.GrainSubmitted e -> submit set e
            | Grain.Event.GrainIdentified e -> invalidOp "Cool"
            | Grain.Event.GrainIdentityChanged e -> invalidOp "Help"
            | Grain.Event.GrainIdentityConfirmed e -> invalidOp "Help"
            | Grain.Event.GrainIdentityUnconfirmed e -> invalidOp "Help"
        | _ -> Ok()


module Slide =

    let rcUpdate = function
    | ReferenceCollection.Event.CollectionPublished (id,time,version) -> Ok()
    | _ -> Ok()

    let handle (e:string*obj) =
        match snd e with
        | :? ReferenceCollection.Event as e -> rcUpdate e
        | _ -> Ok()


module TaxonomicBackbone =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let getById getKey id =
        match id with
        | Some id ->
            let u : Guid = Converters.DomainToDto.unwrapTaxonId id
            match ReadStore.RepositoryBase.getSingle<BackboneTaxon> (u.ToString()) getKey deserialise with
            | Ok t -> t
            | Error e -> readModelErrorHandler()
        | None -> readModelErrorHandler()

    let addToBackbone getKey getSortedListKey setKey setSortedList serialise deserialise (event:Imported) =

        let getFamily familyName =
            ReadStore.TaxonomicBackbone.tryFindByLatinName familyName None None getSortedListKey getKey deserialise

        let reference, referenceUrl =
            match event.Reference with
            | None -> "", ""
            | Some r -> 
                match r with
                | ref,Some u -> ref,Url.unwrap u
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
        ReadStore.TaxonomicBackbone.import setKey setSortedList serialise projection

    let connect getSingle (e:EstablishedConnection) =
        // This connection should occur on GPP MRC taxa, not on backbone taxa?
        Ok()

    let handle get getSortedList set setSortedList (e:string*obj) =
        match snd e with
        | :? Taxonomy.Event as e -> 
            match e with
            | Taxonomy.Event.Imported t -> addToBackbone get getSortedList set setSortedList serialise deserialise t
            | Taxonomy.Event.EstablishedConnection c -> connect get c
        | _ -> Ok()


module ReferenceCollectionReadOnly =

    let publishCollection = Ok()

    let handle (e:string*obj) =
        match snd e with
        | :? ReferenceCollection.Event as e ->
            match e with
            | ReferenceCollection.Event.CollectionPublished (id,time,version) -> invalidOp "Cool"
            | _ -> Ok()
        | _ -> Ok()


module UserProfile =

    let registered user =
        Ok()

    let handle (e:string*obj) =
        match snd e with
        | :? User.Event as e ->
            match e with
            | User.Event.JoinedClub (x,y) -> invalidOp "Cool"
            | User.Event.ProfileHidden x -> invalidOp "Cool"
            | User.Event.ProfileMadePublic x -> invalidOp "Cool"
            | User.Event.UserRegistered x -> invalidOp "Cool"
        | _ -> Ok()


module Digitisation =

    // CollectionDrafts:{ColId}         : EditableCollection
    // CollectionAccessList:{UserId}    : ColId list

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection
    open Converters.DomainToDto

    let started (set:SetStoreValue) (setList:SetEntryInList) (e:DigitisationStarted) =
        let col = {
            Id = e.Id |> unwrapRefId
            Name = e.Name
            Description = e.Description
            EditUserIds = [ e.Owner |> unwrapUserId ]
            LastEdited = DateTime.Now //TODO remove to parameter function
            PublishedVersion = 0
            SlideCount = 0
            Slides = [] }
        let id : Guid = e.Id |> unwrapRefId
        let userId : Guid = e.Owner |> unwrapUserId
        RepositoryBase.setSingle (id.ToString()) col set serialise |> ignore
        RepositoryBase.setListItem (id.ToString()) ("CollectionAccessList:" + (userId.ToString())) setList

    let recordSlide getKey setKey (e:SlideRecorded) =
        let colId : Guid = e.Id |> unwrapSlideId |> fst |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) getKey deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let slide = {
                CollectionId = e.Id |> unwrapSlideId |> fst |> unwrapRefId
                CollectionSlideId = e.Id |> unwrapSlideId |> snd
                FamilyOriginal = e.OriginalFamily
                GenusOriginal = e.OriginalGenus
                SpeciesOriginal = e.OriginalSpecies
                CurrentTaxonId = None
                CurrentFamily = ""
                CurrentGenus = ""
                CurrentSpecies = ""
                CurrentSpAuth = ""
                IsFullyDigitised = false
                Images = [] }
            RepositoryBase.setSingle (colId.ToString()) { c with Slides = slide::c.Slides; SlideCount = c.SlideCount + 1 } setKey serialise

    let imageUploaded getKey setKey id image =
        let colId : Guid = id |> unwrapSlideId |> fst |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) getKey deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let slide = c.Slides |> List.tryFind (fun x -> x.CollectionSlideId = (id |> unwrapSlideId |> snd))
            match slide with
            | None -> readModelErrorHandler()
            | Some s ->
                let imageDto = Converters.DomainToDto.image image
                let updatedSlide = { s with Images = imageDto :: s.Images }
                let updatedSlides = c.Slides |> List.map (fun x -> if x.CollectionSlideId = s.CollectionSlideId then updatedSlide else x)
                let updatedCol = { c with Slides = updatedSlides }
                RepositoryBase.setSingle (c.ToString()) updatedCol setKey serialise

    let digitised getKey setKey id =
        let colId : Guid = id |> unwrapSlideId |> fst |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) getKey deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let slide = c.Slides |> List.tryFind (fun x -> x.CollectionSlideId = (id |> unwrapSlideId |> snd))
            match slide with
            | None -> readModelErrorHandler()
            | Some s ->
                let updatedSlide = { s with IsFullyDigitised = true }
                let updatedSlides = c.Slides |> List.map (fun x -> if x.CollectionSlideId = s.CollectionSlideId then updatedSlide else x)
                let updatedCol = { c with Slides = updatedSlides }
                RepositoryBase.setSingle (colId.ToString()) updatedCol setKey serialise

    let gainedIdentity getKey setKey id taxonId =
        let colId : Guid = id |> unwrapSlideId |> fst |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) getKey deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let slide = c.Slides |> List.tryFind (fun x -> x.CollectionSlideId = (id |> unwrapSlideId |> snd))
            match slide with
            | None -> readModelErrorHandler()
            | Some s ->
                let f,g,sp,auth = 
                    let bbTaxon = RepositoryBase.getSingle ((taxonId |> unwrapTaxonId).ToString()) getKey deserialise<BackboneTaxon>
                    match bbTaxon with
                    | Error e -> readModelErrorHandler()
                    | Ok t -> t.Family, t.Genus, t.Species, t.NamedBy
                let updatedSlide = { s with CurrentFamily = f; CurrentGenus = g; CurrentSpecies = sp; CurrentSpAuth = auth; CurrentTaxonId = taxonId |> Converters.DomainToDto.unwrapTaxonId |> Some }
                let updatedSlides = c.Slides |> List.map (fun x -> if x.CollectionSlideId = s.CollectionSlideId then updatedSlide else x)
                let updatedCol = { c with Slides = updatedSlides }
                RepositoryBase.setSingle (colId.ToString()) updatedCol setKey serialise

    let published getKey setKey id time (version:ColVersion) =
        let colId : Guid = id |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) getKey deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            RepositoryBase.setSingle (colId.ToString()) { c with PublishedVersion = ColVersion.unwrap version; LastEdited = time } setKey serialise

    let handle get getSortedList set setList (e:string*obj) =
        match snd e with
        | :? ReferenceCollection.Event as e ->
            match e with
            | ReferenceCollection.Event.DigitisationStarted e -> started set setList e
            | ReferenceCollection.Event.SlideRecorded e -> recordSlide get set e
            | ReferenceCollection.Event.SlideImageUploaded (s,i) -> imageUploaded get set s i
            | ReferenceCollection.Event.SlideFullyDigitised e -> digitised get set e
            | ReferenceCollection.Event.SlideGainedIdentity (s,t) -> gainedIdentity get set s t
            | ReferenceCollection.Event.CollectionPublished (id,d,v) -> published get set id d v
        | _ -> Ok()
