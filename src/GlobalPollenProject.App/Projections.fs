module GlobalPollenProject.App.Projections

open System
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregates
open GlobalPollenProject.Core.Composition
open ReadStore
open ReadModels

type EventMessage = string*obj*DateTime

let thumbnailSize = 300.
let webImageSize = 2000.

let toEvent (s,o,d) = o
let toTime (s,o,d) = d

let readModelErrorHandler() =
    invalidOp "The read model is corrupt or out-of-sync. Rebuild now."

let inline deserialise< ^a> json = 
    let unwrap (Json j) = j
    Serialisation.deserialise< ^a> (unwrap json)

let serialise s = 
    let result = Serialisation.serialise s
    match result with
    | Ok r -> Ok <| Json r
    | Error e -> Error e

module Result = 

    let toOption r =
        match r with
        | Ok x -> Some x
        | Error _ -> None


module Checkpoint =

    let init setKey =
        RepositoryBase.setKey 0 "Checkpoint" setKey serialise

    let getCurrentVersion getKey =
        RepositoryBase.getKey "Checkpoint" getKey deserialise<int>

    let increment getKey setKey () =
        let incrementCheck current = 
            RepositoryBase.setKey (current + 1) "Checkpoint" setKey serialise
            |> Result.bind (fun x -> Ok (current + 1))
        getCurrentVersion getKey
        |> Result.bind incrementCheck


module GrainLocation =

    let insertLocation (submitted:Grain.GrainSubmitted) =
        // Use redis geoadd on point coordinate
        // Convert domain to dto (spatial)
        // Add to redis list
        Ok()

    let handle (e:EventMessage) =
        match e |> toEvent with
        | :? Grain.Event as e ->
            match e with
            | Grain.Event.GrainSubmitted s -> insertLocation s
            | _ -> Ok()
        | _ -> Ok()

module Statistics =

    // Statistic:UnknownSpecimenTotal
    // Statistic:UnknownSpecimenRemaining
    // Statistic:UnknownSpecimenIdentificationsTotal
    // Statistic:GrainTotal
    // Statistic:SlideTotal
    // Statistic:SlideDigitisedTotal
    // Statistic:Representation:Families:GPP
    // Statistic:BackboneTaxa:Total
    // Statistic:Taxon:SpeciesTotal

    let init set =
        RepositoryBase.setKey 0 "Statistic:UnknownSpecimenTotal" set serialise |> ignore
        RepositoryBase.setKey 0 "Statistic:UnknownSpecimenRemaining" set serialise |> ignore
        RepositoryBase.setKey 0 "Statistic:UnknownSpecimenIdentificationsTotal" set serialise |> ignore
        RepositoryBase.setKey 0 "Statistic:Grain:Total" set serialise |> ignore
        RepositoryBase.setKey 0 "Statistic:SlideTotal" set serialise |> ignore
        RepositoryBase.setKey 0 "Statistic:SlideDigitisedTotal" set serialise |> ignore
        RepositoryBase.setKey 0 "Statistic:Representation:Families:GPP" set serialise |> ignore
        RepositoryBase.setKey 0 "Statistic:Taxon:SpeciesTotal" set serialise

    let incrementStat key get set =
        match RepositoryBase.getKey<int> key get deserialise with
        | Ok i -> RepositoryBase.setKey (i + 1) key set serialise
        | Error e -> RepositoryBase.setKey 1 key set serialise

    let decrementStat key get set =
        RepositoryBase.getKey<int> key get deserialise
        |> bind (fun i -> RepositoryBase.setKey (i - 1) key set serialise)

    let handle get getSortedList set setSortedList (e:EventMessage) =
        match e |> toEvent with
        | :? Grain.Event as e ->
            match e with
            | Grain.Event.GrainSubmitted e -> 
                incrementStat "Statistic:UnknownSpecimenTotal" get set |> ignore
                incrementStat "Statistic:UnknownSpecimenRemaining" get set
            | Grain.Event.GrainIdentityConfirmed e ->
                decrementStat "Statistic:UnknownSpecimenRemaining" get set
            | Grain.Event.GrainIdentified e ->
                incrementStat "Statistic:UnknownSpecimenIdentificationsTotal" get set
            | Grain.Event.GrainIdentityUnconfirmed e ->
                incrementStat "Statistic:UnknownSpecimenRemaining" get set
            | _ -> Ok()
        | _ -> Ok()


module MasterReferenceCollection =

    // TaxonSummary:{Guid}              : TaxonSummary
    // TaxonSummary:{rank}:index        : Guid list
    // TaxonDetail:{Guid}               : TaxonDetail
    // Taxon:{Family}:{Genus}:{species} : Guid
    // Autocomplete:Taxon:{Rank}        : string list

    type SlideDiff =
    | Add of SlideDetail                
    | Remove of SlideDetail
    | Replace of SlideDetail * SlideDetail
    | NoChange
    
    type TaxonReadModel = {
        Summary: TaxonSummary
        Detail: TaxonDetail
    }

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
            DirectChildren = []
        }

    let initTaxonDetail (backboneTaxon:BackboneTaxon) parent backboneChildCount : TaxonDetail =
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
            ReferenceName = ""
            ReferenceUrl = ""
            NeotomaId = 0
            GbifId = 0
            EolId = 0
            EolCache =
                    { CommonEnglishName =      ""
                      PhotoUrl =               ""
                      PhotoAttribution =       ""
                      Description =            ""
                      DescriptionAttribution = ""
                      Retrieved =              DateTime()
            }
            BackboneChildren = backboneChildCount }

    let getLatinName (s:SlideDetail) =
        match s.Rank with
        | "Family" -> s.CurrentFamily
        | "Genus" -> s.CurrentGenus
        | "Species" -> s.CurrentSpecies
        | _ -> ""

    let slideDetailToSummary slide = {
        ColId       = slide.CollectionId
        SlideId     = slide.CollectionSlideId
        LatinName   = getLatinName slide
        Rank        = slide.Rank
        Thumbnail   = slide.Thumbnail
    }

    let toParentNode bbTaxon =
        match bbTaxon.Rank with
        | "Family" -> None |> Ok
        | "Genus" -> Some { Id = bbTaxon.FamilyId; Name = bbTaxon.Family; Rank = "Family" } |> Ok
        | "Species" -> Some { Id = bbTaxon.GenusId.Value; Name = bbTaxon.Genus; Rank = "Genus" } |> Ok //TODO handle nullable<Guid>
        | _ -> Error "The taxonomic backbone is corrupt"

    let getBackboneHeirarchy get bbTaxon =
        let rec traverse (taxa:BackboneTaxon list) =
            let parent = taxa.Head |> toParentNode
            match parent with
            | Ok p ->
                match p with
                | Some n -> 
                    let parentTaxon = TaxonomicBackbone.getById (TaxonId n.Id) get deserialise
                    match parentTaxon with
                    | Ok t -> t :: taxa |> traverse
                    | Error e -> Error e
                | None -> Ok taxa
            | Error e -> Error e
        traverse [ bbTaxon ]

    let generateLookupValue (taxon:BackboneTaxon) =
        match taxon.Rank with 
        | "Family" -> taxon.Family
        | "Genus" -> sprintf "%s:%s" taxon.Genus taxon.Family
        | "Species" -> sprintf "%s:%s:%s" taxon.Species taxon.Genus taxon.Family
        | _ -> "Unknown"

    let getRankKey (t:BackboneTaxon) =
        match t.Rank with
        | "Family" -> sprintf "Taxon:%s" t.Family
        | "Genus" -> sprintf "Taxon:%s:%s" t.Family t.Genus
        | "Species" -> sprintf "Taxon:%s:%s:%s" t.Family t.Genus t.Species
        | _ -> invalidOp "Invalid rank"

    let getTaxon' get taxonId =
        let id : Guid = taxonId |> Converters.DomainToDto.unwrapTaxonId
        let summary = RepositoryBase.getSingle<TaxonSummary> (id.ToString()) get deserialise
        let detail = RepositoryBase.getSingle<TaxonDetail> (id.ToString()) get deserialise
        match summary with
        | Error e -> Error e
        | Ok s ->
            match detail with
            | Error e -> Error e
            | Ok d -> { Summary = s; Detail = d } |> Ok

    let setTaxon get set setSortedList backboneId (readModel:TaxonReadModel) =
        let id : Guid = backboneId |> Converters.DomainToDto.unwrapTaxonId
        let bbTaxon = TaxonomicBackbone.getById backboneId get deserialise
        match bbTaxon with
        | Error e -> Error e
        | Ok t ->
            RepositoryBase.setSingle (id.ToString()) readModel.Summary set serialise |> ignore
            RepositoryBase.setSortedListItem (generateLookupValue t) ("TaxonSummary:" + readModel.Summary.Rank) 0. setSortedList |> ignore
            RepositoryBase.setSingle (id.ToString()) readModel.Detail set serialise |> ignore
            RepositoryBase.setSortedListItem t.LatinName ("Autocomplete:Taxon:" + t.Rank) 0. setSortedList |> ignore
            RepositoryBase.setSortedListItem (generateLookupValue t) ("Autocomplete:Taxon") 0. setSortedList |> ignore
            RepositoryBase.setKey (id.ToString()) (getRankKey t) set serialise

    let getBackboneChildCount get bbTaxon =
        match bbTaxon.TaxonomicStatus with
        | "accepted" ->
            match bbTaxon.Rank with
            | "Family" -> RepositoryBase.getKey ("Statistic:BackboneTaxa:" + bbTaxon.Family) get deserialise
            | "Genus" -> RepositoryBase.getKey ("Statistic:BackboneTaxa:" + bbTaxon.Family + ":" + bbTaxon.Genus) get deserialise
            | "Species" -> 0 |> Ok
            | _ -> Error "Invalid rank specified"
        | _ -> Error "Cannot currently import taxa that are not accepted into MRC"

    let incrementTotal rank get set =
        match rank with
        | "Family" -> Statistics.incrementStat "Statistic:Taxon:FamilyTotal" get set
        | "Genus" -> Statistics.incrementStat "Statistic:Taxon:GenusTotal" get set
        | "Species" -> Statistics.incrementStat "Statistic:Taxon:SpeciesTotal" get set
        | _ -> Ok()


    let initTaxon get set backboneId : Result<TaxonReadModel,string> =
        let bbTaxon = TaxonomicBackbone.getById backboneId get deserialise
        let parentNode = bbTaxon |> bind toParentNode
        let summary = bbTaxon |> lift initTaxonSummary
        let bbTaxonChildCount = bbTaxon |> bind (getBackboneChildCount get)
        let detail = 
            initTaxonDetail 
            <!> bbTaxon 
            <*> parentNode 
            <*> bbTaxonChildCount
        match summary with
        | Error e -> Error e
        | Ok s ->
            match detail with
            | Error e -> Error e
            | Ok d -> 
                incrementTotal d.Rank get set |> ignore
                { Summary = s; Detail = d } |> Ok

    let getTaxon get set backboneId : Result<TaxonReadModel,string> =
        let existing = getTaxon' get backboneId
        match existing with 
        | Ok s -> existing
        | Error e -> initTaxon get set backboneId

    let appendChild (child:TaxonReadModel) (taxon:TaxonReadModel) =
        let newChild = { Id = child.Summary.Id; Name = child.Summary.LatinName; Rank = child.Summary.Rank }
        match taxon.Detail.Children |> List.tryFind (fun c -> c.Name = child.Summary.LatinName) with
        | Some c -> taxon
        | None ->
            { taxon with 
                Summary = { taxon.Summary with DirectChildren = newChild :: taxon.Summary.DirectChildren }; 
                Detail = { taxon.Detail with Children = newChild :: taxon.Detail.Children } }

    let getHeirarchy get set t =
        let heirarchy = getBackboneHeirarchy get t
        match heirarchy with
        | Error e -> Error e
        | Ok h ->
            let getParent (c:TaxonReadModel) (p:Guid) =
                getTaxon get set (p |> TaxonId)
                |> lift (appendChild c)
            let rec getParents (remaining:BackboneTaxon list) (stashed:TaxonReadModel list) child =
                match remaining |> List.length with
                | 0 -> child :: stashed |> Ok
                | _ ->
                    let parent = getParent child remaining.Head.Id
                    match parent with
                    | Error e -> Error e
                    | Ok p -> getParents remaining.Tail (child :: stashed) p
            let reversed = h |> List.rev
            getTaxon get set (reversed.Head.Id |> TaxonId)
            |> bind (getParents reversed.Tail [])

    let isMultiMatch l =
        match l |> List.length with
        | 0 -> Error "No matches: unknown taxon specified"
        | 1 -> Ok l.Head
        | _ -> Error "Multiple taxon matches: cannot place this record in the pollen taxonomy"

    /// Gets the currently accepted backbone taxon for this ID. If the taxon
    /// is currently a synonym, it will return the current accepted
    /// taxon. If the backbone taxon is currently disputed, it will
    /// return an error.
    let currentAcceptedTaxon get backboneId =
        let bbTaxon = TaxonomicBackbone.getById backboneId get deserialise
        match bbTaxon with
        | Error e -> Error e
        | Ok t ->
            match t.TaxonomicStatus with
            | "accepted" -> Some t |> Ok
            | "misapplied"
            | "synonym" ->
                let m =[t] 
                        |> TaxonomicBackbone.tryTrace t.Rank t.NamedBy get deserialise
                        |> Result.bind isMultiMatch
                match m with
                | Ok t -> Some t |> Ok
                | Error _ -> None |> Ok
            | _ -> None |> Ok

    let setDiff previous current =
        set current - set previous |> Set.toList

    let toSlideIds = List.map (fun s -> s.CollectionSlideId)

    let getAddedSlides previous current =
        let addedIds = setDiff (previous |> toSlideIds) (current |> toSlideIds)
        current
        |> List.filter(fun s -> addedIds |> List.contains s.CollectionSlideId)
        |> List.map Add

    let getRemovedSlides previous current =
        let removedIds = setDiff (current |> toSlideIds) (previous |> toSlideIds)
        previous
        |> List.filter(fun s -> removedIds |> List.contains s.CollectionSlideId)
        |> List.map Remove

    let getChangedSlides (previous:SlideDetail list) (current:SlideDetail list) =
        previous
        |> List.map (fun prev -> prev, current |> List.tryFind (fun c -> c.CollectionSlideId = prev.CollectionSlideId))
        |> List.choose(fun (p,c) -> match c with | None -> None | Some s -> Some (p,s))
        |> List.map(fun (p,c) -> Replace (p,c))

    let computeDifferences (previousVersion:ReferenceCollectionDetail) (currentVersion:EditableRefCollection) : SlideDiff list =
        let prev = previousVersion.Slides |> List.filter (fun s -> s.IsFullyDigitised)
        let curr = currentVersion.Slides |> List.filter (fun s -> s.IsFullyDigitised)
        let added = getAddedSlides prev curr
        let changed = getChangedSlides prev curr
        let removed = getRemovedSlides prev curr
        [added; changed; removed]
        |> List.concat

    let addSlide get set setSortedList (s:SlideDetail) = 
        match s.CurrentTaxonId with
        | None -> Error "Taxon did not exist"
        | Some bbId ->
            let slideSummary = slideDetailToSummary s
            let add (slide:SlideSummary) (taxon:TaxonReadModel) =
                let summary = { taxon.Summary with SlideCount = taxon.Summary.SlideCount + 1; ThumbnailUrl = slide.Thumbnail }
                let detail = { taxon.Detail with Slides = slide :: taxon.Detail.Slides }
                { Summary = summary; Detail = detail }
            Statistics.incrementStat "Statistic:SlideDigitisedTotal" get set |> ignore
            bbId
            |> TaxonId
            |> currentAcceptedTaxon get
            |> bind (fun current ->
                match current with
                | None -> Ok()
                | Some t ->
                    t
                    |> getHeirarchy get set
                    |> lift (List.map (add slideSummary))
                    |> bind (mapResult (fun rm -> setTaxon get set setSortedList (rm.Summary.Id |> TaxonId) rm))
                    |> lift ignore )

    let removeSlide get set setSortedList s = 
        match s.CurrentTaxonId with
        | None -> Error "Taxon did not exist"
        | Some bbId ->
            let slideSummary = slideDetailToSummary s
            let remove (slide:SlideSummary) (taxon:TaxonReadModel) =
                let summary = { taxon.Summary with SlideCount = taxon.Summary.SlideCount - 1 }
                let detail = { taxon.Detail with Slides = taxon.Detail.Slides |> List.filter (fun s -> not (s = slide)) }
                { Summary = summary; Detail = detail }
            Statistics.decrementStat "Statistic:SlideDigitisedTotal" get set |> ignore
            currentAcceptedTaxon get (bbId |> TaxonId)
            |> bind (fun current ->
                match current with
                | None -> Ok()
                | Some t ->
                    getHeirarchy get set t
                    |> lift (List.map (remove slideSummary))
                    |> bind (mapResult (fun rm -> setTaxon get set setSortedList (rm.Summary.Id |> TaxonId) rm))
                    |> lift ignore )

    let updateSlide get set setSortedList (oldSlide:SlideDetail) (newSlide:SlideDetail) = 
        match oldSlide = newSlide with
        | true -> Ok()
        | false ->
            // TODO proper error handling here
            removeSlide get set setSortedList oldSlide |> ignore
            addSlide get set setSortedList newSlide |> ignore
            Ok()

    let execute get set setSortedList diff =
        match diff with
        | NoChange -> Ok()
        | Add s -> addSlide get set setSortedList s
        | Remove s -> removeSlide get set setSortedList s
        | Replace (o,n) -> updateSlide get set setSortedList o n

    let publishCollection id version get getSortedList set setSortedList =
        let colId : Guid = id |> Converters.DomainToDto.unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) get deserialise<EditableRefCollection>
        let ver = version |> Converters.DomainToDto.unwrapColVer
        match col with
        | Error e -> Error e
        | Ok c ->
            match ver with
            | 1 ->
                c.Slides
                |> List.filter (fun s -> not s.Voided)
                |> List.filter (fun s -> s.IsFullyDigitised)
                |> List.map Add
                |> mapResult (execute get set setSortedList)
                |> lift ignore
            | _ ->
                let previousVersion = RepositoryBase.getKey<ReferenceCollectionDetail> (sprintf "ReferenceCollectionDetail:%s:V%i" (colId.ToString()) ((ver) - 1)) get deserialise
                match previousVersion with
                | Error e -> Error e
                | Ok prevC ->
                    computeDifferences prevC c
                    |> mapResult (execute get set setSortedList)
                    |> lift ignore

    let establishConnection get set id externalId =
        let getExisting (id:Guid) = RepositoryBase.getSingle<TaxonDetail> (id.ToString()) get deserialise
        let save (taxon:TaxonDetail) = RepositoryBase.setSingle (taxon.Id.ToString()) taxon set serialise
        let updateId taxon =
            match externalId with
            | Taxonomy.ThirdPartyTaxonId.NeotomaId i -> {taxon with NeotomaId = i}
            | Taxonomy.ThirdPartyTaxonId.GbifId i -> {taxon with GbifId = i}
            | Taxonomy.ThirdPartyTaxonId.EncyclopediaOfLifeId i -> 
                let cache = ExternalLink.getEncyclopediaOfLifeCacheData i
                match cache with
                | Some c -> { taxon with EolId = i; EolCache = c }
                | None -> { taxon with EolId = i }
        id
        |> Converters.DomainToDto.unwrapTaxonId
        |> getExisting
        |> lift updateId
        |> bind save

    let addGrain (grainId:GrainId) (taxonId:TaxonId) =
        // Get taxon and add grain for it and every parent level
        Ok()

    let removeGrainFromMrc (grainId:GrainId) =

        // Figure out the current taxon in MRC
        // Get taxon and remove grain for it and every parent
        Ok()

    let handle get getSortedList set setSortedList (e:EventMessage) =
        match e |> toEvent with
        | :? ReferenceCollection.Event as e -> 
            match e with
            | ReferenceCollection.Event.CollectionPublished (id,date,ver) -> publishCollection id ver get getSortedList set setSortedList
            | _ -> Ok()
        | :? Grain.Event as e ->
            match e with
            | Grain.Event.GrainIdentityConfirmed e -> addGrain e.Id e.Taxon
            | Grain.Event.GrainIdentityChanged e -> 
                removeGrainFromMrc e.Id |> ignore
                addGrain e.Id e.Taxon
            | Grain.Event.GrainIdentityUnconfirmed e -> removeGrainFromMrc e.Id
            | _ -> Ok()
        | :? Taxonomy.Event as e ->
            match e with
            | Taxonomy.Event.EstablishedConnection (id,exId) -> establishConnection get set id exId
            | _ -> Ok()
        | _ -> Ok()


module Grain =

    open GlobalPollenProject.Core.Aggregates.Grain

    let submit getKey setReadModel setList generateCacheImage toAbsoluteUrl time (e:GrainSubmitted) =
        let thumbUrl = 
            let result = 
                match e.Images.Head with
                | SingleImage (relUrl,_) -> (generateCacheImage thumbnailSize "thumb") relUrl
                | FocusImage (frames,_,_) ->
                    match frames |> List.length with
                    | i when i > 0 -> (generateCacheImage thumbnailSize "thumb") frames.[i / 2]
                    | _ -> invalidOp "Empty focus image"
            match result with
            | Ok (u,_) -> u |> Url.unwrap
            | Error _ -> ""

        let summary = { 
            Id = Converters.DomainToDto.unwrapGrainId e.Id 
            Thumbnail = thumbUrl
            Submitted = time }

        let removeUnit (x:float<_>) = float x
        let removeUnitInt (x:int<_>) = int x
        let unwrapLat (Latitude x) = x |> removeUnit
        let unwrapLon (Longitude x) = x |> removeUnit
        let lat,lon =
            match e.Spatial with
            | Site (la,lo) -> unwrapLat la, unwrapLon lo
            | _ -> 0.,0.

        let ageType,age =
            match e.Temporal with
            | None -> "",0
            | Some a ->
                match a with
                | CollectionDate calYr -> "Calendar", calYr |> removeUnitInt
                | Radiocarbon ybp -> "Radiocarbon", ybp |> removeUnitInt
                | Lead210 ybp -> "Lead210", ybp |> removeUnitInt

        let getMag mag = 
            let calId,level = Converters.DomainToDto.unwrapMagId mag
            RepositoryBase.getSingle<Calibration> (calId.ToString()) getKey deserialise
            |> lift (fun c -> c.Magnifications |> List.tryFind (fun m -> m.Level = level))

        let cacheImage url =
            match generateCacheImage webImageSize "web" url with
            | Ok (r,m) -> r,m
            | Error _ -> readModelErrorHandler()

        let imgs =  e.Images |> List.map (Converters.DomainToDto.image cacheImage getMag toAbsoluteUrl)
        let detail = {
            Id = Converters.DomainToDto.unwrapGrainId e.Id
            Images = imgs
            Identifications = []
            Latitude = lat
            Longitude = lon
            AgeType = ageType
            Age = age
            ConfirmedRank = ""
            ConfirmedFamily = ""
            ConfirmedGenus = ""
            ConfirmedSpecies = ""
            ConfirmedSpAuth = "" }

        ReadStore.RepositoryBase.setSingle (summary.Id.ToString()) summary setReadModel serialise |> ignore
        ReadStore.RepositoryBase.setSingle (detail.Id.ToString()) detail setReadModel serialise |> ignore
        RepositoryBase.setListItem (summary.Id.ToString()) "GrainSummary:index" setList

    let identified get set (e:GrainIdentified) =
        let id : Guid = Converters.DomainToDto.unwrapGrainId e.Id
        let grain = ReadStore.RepositoryBase.getSingle<GrainDetail> (id.ToString()) get deserialise
        let taxon = ReadStore.TaxonomicBackbone.getById e.Taxon get deserialise

        let toHeirarchy (taxon:BackboneTaxon) =
            taxon.Family,taxon.Genus,taxon.Species,taxon.NamedBy,taxon.Rank

        let idMethod = "Morphological"

        let createIdentification userId idMethod heirarchy =
            let family,genus,species,namedBy,rank = heirarchy
            { User = userId |> Converters.DomainToDto.unwrapUserId 
              IdentificationMethod = idMethod
              Rank = rank
              Family = family
              Genus = genus
              Species = species
              SpAuth = namedBy }

        let identification =
            createIdentification e.IdentifiedBy idMethod
            <!> (taxon |> lift toHeirarchy)

        let addId id grain = { grain with Identifications = id :: grain.Identifications }
        let save (grain:GrainDetail) = ReadStore.RepositoryBase.setSingle (id.ToString()) grain set serialise

        addId
        <!> identification
        <*> grain
        |> bind save

    let identityChanged get set (taxon:TaxonId option) grainId = 
        let id : Guid = Converters.DomainToDto.unwrapGrainId grainId
        let grain = ReadStore.RepositoryBase.getSingle<GrainDetail> (id.ToString()) get deserialise
        let save grain = ReadStore.RepositoryBase.setSingle (id.ToString()) grain set serialise

        match taxon with
        | Some t ->
            let taxon = ReadStore.TaxonomicBackbone.getById t get deserialise
            let toHeirarchy (taxon:BackboneTaxon) =
                taxon.Family,taxon.Genus,taxon.Species,taxon.NamedBy,taxon.Rank
            let switchCurrentTaxon heirarchy grain =
                let family,genus,species,namedBy,rank = heirarchy
                { grain with ConfirmedFamily = family
                             ConfirmedGenus = genus
                             ConfirmedSpecies = species
                             ConfirmedSpAuth = namedBy
                             ConfirmedRank = rank }
            switchCurrentTaxon
            <!> (taxon |> lift toHeirarchy)
            <*> grain
            |> save
        
        | None ->
            let update grain = { grain with ConfirmedFamily = ""
                                            ConfirmedGenus = ""
                                            ConfirmedSpecies = ""
                                            ConfirmedSpAuth = ""
                                            ConfirmedRank = "" }
            grain
            |> lift update
            |> bind save


    let handle get set setList generateCacheImage toAbsoluteUrl (em:EventMessage) =
        match em |> toEvent with
        | :? Grain.Event as e ->
            match e with
            | GrainSubmitted e -> submit get set setList generateCacheImage toAbsoluteUrl (em |> toTime) e
            | GrainIdentified e -> identified get set e 
            | GrainIdentityChanged e -> identityChanged get set (Some e.Taxon) e.Id
            | GrainIdentityConfirmed e -> identityChanged get set (Some e.Taxon) e.Id
            | GrainIdentityUnconfirmed e -> identityChanged get set None e.Id
        | _ -> Ok()


module TaxonomicBackbone =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    // Statistic:Representation:{Family}:{Genus}
    // Statistic:BackboneTaxa:{F/G/S/Total}

    let init set =
        RepositoryBase.setKey 0 "Statistic:BackboneTaxa:Families" set serialise |> ignore     
        RepositoryBase.setKey 0 "Statistic:BackboneTaxa:Genera" set serialise |> ignore     
        RepositoryBase.setKey 0 "Statistic:BackboneTaxa:Species" set serialise |> ignore     
        RepositoryBase.setKey 0 "Statistic:BackboneTaxa:Total" set serialise

    let getById getKey id =
        match id with
        | Some id ->
            let u : Guid = Converters.DomainToDto.unwrapTaxonId id
            match ReadStore.RepositoryBase.getSingle<BackboneTaxon> (u.ToString()) getKey deserialise with
            | Ok t -> t
            | Error e -> readModelErrorHandler()
        | None -> readModelErrorHandler()

    let addToBackbone getKey (getSortedListKey:GetSortedListFromKeyValueStore) setKey setSortedList serialise deserialise (event:Imported) =

        let getFamily familyName =
            ReadStore.TaxonomicBackbone.tryFindByLatinName familyName None None getSortedListKey getKey deserialise

        let reference, referenceUrl =
            match event.Reference with
            | None -> "", ""
            | Some r -> 
                match r with
                | ref,Some u -> ref,Url.unwrap u
                | ref,None -> ref,""

        let family,genus,species,rank,ln,namedBy,fId,gId,sId =
            match event.Identity with
            | Family ln -> 
                let fId = Converters.DomainToDto.unwrapTaxonId event.Id
                Converters.DomainToDto.unwrapLatin ln,"","", "Family", Converters.DomainToDto.unwrapLatin ln,"",fId,Nullable<Guid>(),Nullable<Guid>()
            | Genus ln ->
                let family = getById getKey event.Parent
                let fId = family.Id
                let gId = Converters.DomainToDto.unwrapTaxonId event.Id
                family.LatinName,Converters.DomainToDto.unwrapLatin ln,"", "Genus", Converters.DomainToDto.unwrapLatin ln,"",fId,Nullable<Guid>(gId),Nullable<Guid>()
            | Species (g,s,n) -> 
                let species = sprintf "%s %s" (Converters.DomainToDto.unwrapLatin g) (Converters.DomainToDto.unwrapEph s)
                let genus = getById getKey event.Parent
                let family = getFamily genus.Family
                match family with
                | Ok f ->
                    let fId = f.Id
                    let gId = genus.Id
                    let sId = Converters.DomainToDto.unwrapTaxonId event.Id
                    f.LatinName, genus.LatinName, species,"Species", species,Converters.DomainToDto.unwrapAuthor n,fId,Nullable<Guid>(gId),Nullable<Guid>(sId)
                | Error e -> readModelErrorHandler()

        let status,alias =
            match event.Status with
            | Accepted -> "accepted",""
            | Doubtful -> "doubtful",""
            | Misapplied id -> "misapplied",(id |> Converters.DomainToDto.unwrapTaxonId).ToString()
            | Synonym id -> "synonym",(id |> Converters.DomainToDto.unwrapTaxonId).ToString()

        let group =
            match event.Group with
            | TaxonomicGroup.Angiosperm -> "Angiosperm"
            | TaxonomicGroup.Bryophyte -> "Bryophyte"
            | TaxonomicGroup.Gymnosperm -> "Gymnosperm"
            | TaxonomicGroup.Pteridophyte -> "Pteridophyte"

        let projection = 
            {   Id = Converters.DomainToDto.unwrapTaxonId event.Id
                Group = group
                Family = family
                Genus = genus
                Species = species
                FamilyId = fId
                GenusId = gId
                SpeciesId = sId
                LatinName = ln
                NamedBy = namedBy
                TaxonomicStatus = status
                TaxonomicAlias = alias
                Rank = rank
                ReferenceName = reference
                ReferenceUrl = referenceUrl }
        ReadStore.TaxonomicBackbone.import setKey setSortedList serialise projection |> ignore

        match projection.TaxonomicStatus with
        | "accepted" ->
            Statistics.incrementStat "Statistic:BackboneTaxa:Total" getKey setKey |> ignore
            match projection.Rank with
            | "Family" ->
                Statistics.incrementStat "Statistic:BackboneTaxa:Families" getKey setKey
            | "Genus" ->
                Statistics.incrementStat "Statistic:BackboneTaxa:Genera" getKey setKey |> ignore
                Statistics.incrementStat ("Statistic:BackboneTaxa:" + projection.Family) getKey setKey
            | "Species" ->
                Statistics.incrementStat "Statistic:BackboneTaxa:Species" getKey setKey |> ignore
                Statistics.incrementStat ("Statistic:BackboneTaxa:" + projection.Family + ":" + projection.Genus) getKey setKey
            | _ -> Ok()
        | _ -> Ok()

    let handle get getSortedList set setSortedList (e:EventMessage) =
        match e |> toEvent with
        | :? Taxonomy.Event as e -> 
            match e with
            | Taxonomy.Event.Imported t -> addToBackbone get getSortedList set setSortedList serialise deserialise t
            | _ -> Ok()
        | _ -> Ok()


module ReferenceCollectionReadOnly =

    // ReferenceCollectionSummary:{Guid}            : ReferenceCollectionSummary
    // ReferenceCollectionDetail:{Guid}:V{Version}  : ReferenceCollectionDetail

    let updateSlideReadModel set slide =
        let slidePublishedId = sprintf "%s:%s" (slide.CollectionId.ToString()) slide.CollectionSlideId
        RepositoryBase.setSingle slidePublishedId slide set serialise

    let published get set setList (colId:CollectionId) time version =
        let id : Guid = colId |> Converters.DomainToDto.unwrapRefId
        let col = RepositoryBase.getSingle (id.ToString()) get deserialise<EditableRefCollection>
        match col with
        | Ok c ->
            let summary = {
                Id              = c.Id
                Name            = c.Name
                Description     = c.Description
                SlideCount      = c.SlideCount
                Published       = time
                CuratorFirstNames = c.CuratorFirstNames
                CuratorSurname  = c.CuratorSurname
                CuratorEmail    = c.CuratorEmail
                AccessMethod    = c.AccessMethod
                Institution     = c.Institution
                InstitutionUrl  = c.InstitutionUrl
                Version         = Converters.DomainToDto.unwrapColVer version
            }
            let v = Converters.DomainToDto.unwrapColVer version
            let collectors = 
                c.Slides 
                |> List.map (fun s -> s.CollectorName)
                |> List.distinct
            let getUser (id:Guid) = ReadStore.RepositoryBase.getSingle<PublicProfile> (id.ToString()) get deserialise
            let digitisers =
                c.EditUserIds
                |> List.map getUser
                |> mapResult(lift (fun u -> u.FirstName + " " + u.LastName))
            let detail = {
                Id              = c.Id
                Name            = c.Name
                Description     = c.Description
                Published       = time
                CuratorFirstNames = c.CuratorFirstNames
                CuratorSurname  = c.CuratorSurname
                CuratorEmail    = c.CuratorEmail
                AccessMethod    = c.AccessMethod
                Institution     = c.Institution
                InstitutionUrl  = c.InstitutionUrl
                Version         = v
                Slides          = c.Slides
                Digitisers      = match digitisers with | Ok u -> u | Error _ -> []
                Collectors      = collectors
            }
            RepositoryBase.setSingle (id.ToString()) summary set serialise |> ignore
            RepositoryBase.setKey detail (sprintf "ReferenceCollectionDetail:%s:V%i" (id.ToString()) v) set serialise |> ignore
            RepositoryBase.setListItem (id.ToString()) "ReferenceCollectionSummary:index" setList |> ignore
            c.Slides |> List.map (updateSlideReadModel set) |> ignore
            Ok()
        | Error e -> Error e

    let handle get set setList (e:EventMessage) =
        match e |> toEvent with
        | :? ReferenceCollection.Event as e ->
            match e with
            | ReferenceCollection.Event.CollectionPublished (id,time,version) -> published get set setList id time version
            | _ -> Ok()
        | _ -> Ok()


module UserProfile =

    open GlobalPollenProject.Core.Aggregates.User

    let registered set setList (user:UserRegistered) =
        let profile = {
            UserId = user.Id |> Converters.DomainToDto.unwrapUserId
            Title = user.Title
            FirstName = user.FirstName
            LastName = user.LastName
            Score = 0.
            Groups = []
            Curator = false
            IsPublic = true }
        RepositoryBase.setSingle (profile.UserId.ToString()) profile set serialise |> ignore
        RepositoryBase.setListItem (profile.UserId.ToString()) "PublicProfile:index" setList

    let updateProperty get set userId updater newValue =
        let id = userId |> Converters.DomainToDto.unwrapUserId
        let existing = RepositoryBase.getSingle<PublicProfile> (id.ToString()) get deserialise
        match existing with
        | Ok u ->
            let updated = updater u newValue
            RepositoryBase.setSingle (id.ToString()) updated set serialise
        | Error e -> Error e

    let changeVisibility get set visibility userId =
        let updater (u:PublicProfile) vis = {u with IsPublic = vis }
        updateProperty get set userId updater visibility

    let changeCuration get set curation userId =
        let updater (u:PublicProfile) c = {u with Curator = c }
        updateProperty get set userId updater curation

    let addOrganisation get set org userId =
        let updater (u:PublicProfile) o = { u with Groups = o :: u.Groups }
        updateProperty get set userId updater (org |> Converters.DomainToDto.unwrapShortText)

    let handle get set setList (e:EventMessage) =
        match e |> toEvent with
        | :? User.Event as e ->
            match e with
            | User.Event.JoinedOrganisation (i,org) -> addOrganisation get set org i
            | User.Event.ProfileHidden i -> changeVisibility get set false i
            | User.Event.ProfileMadePublic i -> changeVisibility get set true i
            | User.Event.BecameCurator i -> changeCuration get set true i
            | User.Event.UserRegistered u -> registered set setList u
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
            CuratorFirstNames = ""
            CuratorSurname = ""
            CuratorEmail = ""
            AccessMethod = ""
            Institution = ""
            InstitutionUrl = ""
            EditUserIds = [ e.Owner |> unwrapUserId ]
            LastEdited = DateTime.Now //TODO remove to parameter function
            PublishedVersion = 0
            SlideCount = 0
            Slides = []
            CommentsFromReview = ""
            AwaitingReview = false }
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
            let rank =
                if String.IsNullOrEmpty e.OriginalGenus then "Family"
                else if String.IsNullOrEmpty e.OriginalSpecies then "Genus"
                else "Species" 
            let ageType,age = Converters.DomainToDto.age e.Time
            let locationType,location = Converters.DomainToDto.location e.Place
            let prepDate = Converters.DomainToDto.prepDate e.PrepDate
            let prepMethod = Converters.DomainToDto.prepMethod e.PrepMethod
            let mount = Converters.DomainToDto.mount e.Mounting
            let collectorName = Converters.DomainToDto.collectorName e.Taxon
            let idMethod,plantIdMethod = Converters.DomainToDto.taxonomicIdentification e.Taxon
            let slide = {
                CollectionId = e.Id |> unwrapSlideId |> fst |> unwrapRefId
                CollectionSlideId = e.Id |> unwrapSlideId |> snd
                CollectorName = collectorName
                FamilyOriginal = e.OriginalFamily
                GenusOriginal = e.OriginalGenus
                SpeciesOriginal = e.OriginalSpecies
                Rank = rank
                IdMethod = idMethod
                PlantId = plantIdMethod
                CurrentTaxonId = None
                CurrentFamily = ""
                CurrentGenus = ""
                CurrentTaxonStatus = ""
                CurrentSpecies = ""
                CurrentSpAuth = ""
                IsFullyDigitised = false
                Thumbnail = ""
                Images = []
                Age = age
                AgeType = ageType
                PrepYear = prepDate
                PrepMethod = prepMethod
                Mount = mount
                Location = location
                LocationType = locationType
                PreppedBy = ""
                Voided = false }
            RepositoryBase.setSingle (colId.ToString()) { c with Slides = slide::c.Slides; SlideCount = c.SlideCount + 1 } setKey serialise

    let imageUploaded getKey setKey generateCacheImage toAbsoluteUrl id image =
        let colId : Guid = id |> unwrapSlideId |> fst |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) getKey deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let slide = c.Slides |> List.tryFind (fun x -> x.CollectionSlideId = (id |> unwrapSlideId |> snd))
            match slide with
            | None -> readModelErrorHandler()
            | Some s ->
                let thumbnailUrl = 
                    let result = 
                        match image with
                        | SingleImage (relUrl,_) -> (generateCacheImage thumbnailSize "thumb") relUrl
                        | FocusImage (frames,_,_) ->
                            match frames |> List.length with
                            | i when i > 0 -> (generateCacheImage thumbnailSize "thumb") frames.[i / 2]
                            | _ -> invalidOp "Empty focus image"
                    match result with
                    | Ok (u,_) -> u |> Url.unwrap
                    | Error _ -> ""
                let getMag mag = 
                    let calId,level = Converters.DomainToDto.unwrapMagId mag
                    RepositoryBase.getSingle<Calibration> (calId.ToString()) getKey deserialise
                    |> lift (fun c -> c.Magnifications |> List.tryFind (fun m -> m.Level = level))
                let cacheImage url =
                    match generateCacheImage webImageSize "web" url with
                    | Ok (r,s) -> r,s
                    | Error _ -> url |> toAbsoluteUrl ,1.
                let imageDto = Converters.DomainToDto.image cacheImage getMag toAbsoluteUrl image
                let updatedSlide = { s with Images = imageDto :: s.Images; Thumbnail = thumbnailUrl }
                let updatedSlides = 
                    c.Slides 
                    |> List.map (fun x -> if x.CollectionSlideId = s.CollectionSlideId then updatedSlide else x)
                    |> List.sortBy (fun s -> s.CollectionSlideId)
                let updatedCol = { c with Slides = updatedSlides }
                RepositoryBase.setSingle (colId.ToString()) updatedCol setKey serialise

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
                let f,g,sp,auth,status = 
                    let bbTaxon = RepositoryBase.getSingle ((taxonId |> unwrapTaxonId).ToString()) getKey deserialise<BackboneTaxon>
                    match bbTaxon with
                    | Error e -> readModelErrorHandler()
                    | Ok t -> t.Family, t.Genus, t.Species, t.NamedBy, t.TaxonomicStatus
                let updatedSlide = { s with CurrentTaxonStatus = status; CurrentFamily = f; CurrentGenus = g; CurrentSpecies = sp; CurrentSpAuth = auth; CurrentTaxonId = taxonId |> Converters.DomainToDto.unwrapTaxonId |> Some }
                let updatedSlides = c.Slides |> List.map (fun x -> if x.CollectionSlideId = s.CollectionSlideId then updatedSlide else x)
                let updatedCol = { c with Slides = updatedSlides }
                RepositoryBase.setSingle (colId.ToString()) updatedCol setKey serialise

    let gainedPrep getKey setKey id person =
        let colId : Guid = id |> unwrapSlideId |> fst |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) getKey deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let slide = c.Slides |> List.tryFind (fun x -> x.CollectionSlideId = (id |> unwrapSlideId |> snd))
            match slide with
            | None -> readModelErrorHandler()
            | Some s ->
                let personName = person |> Converters.DomainToDto.person
                let updatedSlide = { s with PreppedBy = personName }
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

    let requestedPublication get set setList colId =
        let id = colId |> Converters.DomainToDto.unwrapRefId
        let col = RepositoryBase.getSingle (id.ToString()) get deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let updated = { c with AwaitingReview = true }
            RepositoryBase.setListItem (id.ToString()) "Curation:InReview" setList |> ignore
            RepositoryBase.setSingle (colId.ToString()) updated set serialise

    let revision get set id note =
        let colId : Guid = id |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) get deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let updated = { c with AwaitingReview = false; CommentsFromReview = note |> Converters.DomainToDto.unwrapLongText }
            RepositoryBase.setSingle (colId.ToString()) updated set serialise

    let publicAccess get set id curator access =
        let colId : Guid = id |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) get deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            
            let curatorEmail =
                match curator.Contact with
                | Email e -> e |> Converters.DomainToDto.unwrapEmail

            let accessMethod,institution,institutionUrl =
                match access with
                | AccessToMaterial.DigitialOnly -> "digital","",""
                | PrimaryLocation p
                | ReplicateLocations (p, _) ->
                    match p with
                    | Personal -> "private","",""
                    | Institutional i ->
                        let name = Converters.DomainToDto.unwrapShortText i.Name
                        match i.Web with
                        | Some w -> "institution",name, w |> Url.unwrap
                        | None -> "institution",name,""

            let updated = { c with CuratorFirstNames = curator.Forenames |> List.fold (fun r s -> r + s + "\n") ""
                                   CuratorSurname = curator.Surname
                                   CuratorEmail = curatorEmail
                                   AccessMethod = accessMethod
                                   Institution = institution
                                   InstitutionUrl = institutionUrl  }
            RepositoryBase.setSingle (colId.ToString()) updated set serialise

    let voidSlide get set id =
        let colId : Guid = id |> unwrapSlideId |> fst |> unwrapRefId
        let col = RepositoryBase.getSingle (colId.ToString()) get deserialise<EditableRefCollection>
        match col with
        | Error e -> Error e
        | Ok c -> 
            let slide = c.Slides |> List.tryFind (fun x -> x.CollectionSlideId = (id |> unwrapSlideId |> snd))
            match slide with
            | None -> readModelErrorHandler()
            | Some s ->
                let updatedSlides = c.Slides |> List.except [s]
                let updatedCol = { c with Slides = updatedSlides }
                RepositoryBase.setSingle (colId.ToString()) updatedCol set serialise

    let handle get getSortedList set setList generateCacheImage toAbsoluteUrl (e:EventMessage) =
        match e |> toEvent with
        | :? ReferenceCollection.Event as e ->
            match e with
            | DigitisationStarted e -> started set setList e
            | SlideRecorded e -> recordSlide get set e
            | SlideImageUploaded (s,i,y) -> imageUploaded get set generateCacheImage toAbsoluteUrl s i
            | SlideFullyDigitised e -> digitised get set e
            | SlideGainedIdentity (s,t) -> gainedIdentity get set s t
            | SlidePrepAcknowledged (s,p) -> gainedPrep get set s p
            | CollectionPublished (id,d,v) -> published get set id d v
            | RequestedPublication id -> requestedPublication get set setList id
            | RevisionAdvised (id,note) -> revision get set id note
            | PublicAccessAssigned (id,curator,access) -> publicAccess get set id curator access
            | SlideVoided id -> voidSlide get set id
        | _ -> Ok()


module Calibration =

    open GlobalPollenProject.Core.Aggregates.Calibration
    open Converters.DomainToDto

    // Calibration:User:{UserId}   : CalId list
    // Calibration:{CalId}         : Calibration

    let removeUnitInt (x:int<_>) = int x
    let removeUnitFloat (x:float<_>) = float x

    let setup set setList (e:SetupMicroscope) =
        let cal = Converters.DomainToDto.calibration e.Id e.User e.FriendlyName e.Microscope
        let id = (e.Id |> unwrapCalId).ToString()
        RepositoryBase.setSingle id cal set serialise |> ignore
        RepositoryBase.setListItem id ("Calibration:User:" + ((e.User |> unwrapUserId).ToString())) setList

    let calibrated get set toAbsoluteUrl e =
        let calId : Guid = e.Id |> unwrapCalId
        let cal = RepositoryBase.getSingle (calId.ToString()) get deserialise<Calibration> 
        match cal with
        | Error e -> Error e
        | Ok c ->
            let newMag = {
                Level = e.Magnification |> removeUnitInt
                Image = e.Image |> toAbsoluteUrl |> Url.unwrap
                PixelWidth = e.PixelWidth |> removeUnitFloat
            }
            let updated = { 
                c with
                    Magnifications = newMag :: c.Magnifications
                    UncalibratedMags = c.UncalibratedMags |> List.filter (fun m -> not (m = (e.Magnification |> removeUnitInt))) }
            RepositoryBase.setSingle (calId.ToString()) updated set serialise

    let handle get getList set setList toAbsoluteUrl (e:EventMessage) =
        match e |> toEvent with
        | :? Event as e ->
            match e with
            | Event.SetupMicroscope e -> setup set setList e
            | Event.CalibratedMag e -> calibrated get set toAbsoluteUrl e
        | _ -> Ok()
