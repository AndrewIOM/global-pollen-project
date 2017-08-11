module GlobalPollenProject.Core.Aggregates.ReferenceCollection

open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregate
open System

type Command =
| CreateCollection of CreateCollection
| AddSlide of AddSlide
| UploadSlideImage of UploadSlideImage
| Publish of CollectionId

and CreateCollection = {Id:CollectionId; Name:string; Owner:UserId; Description: string}
and UploadSlideImage = {Id:SlideId; Image:Image; DateTaken:DateTime }
and AddSlide = 
    {Collection:        CollectionId
     Taxon:             TaxonIdentification
     OriginalFamily:    string
     OriginalGenus:     string
     OriginalSpecies:   string
     OriginalAuthor:    string
     ExistingId:        string option
     Place:             SamplingLocation option
     Time:              Age option
     PrepMethod:        ChemicalTreatment option
     PrepDate:          int<CalYr> option
     Mounting:          MountingMedium option }

type Event =
| DigitisationStarted of DigitisationStarted
| CollectionPublished of CollectionId * DateTime * ColVersion
| SlideRecorded of SlideRecorded
| SlideImageUploaded of SlideId * Image
| SlideFullyDigitised of SlideId
| SlideGainedIdentity of SlideId * TaxonId

and DigitisationStarted = {Id: CollectionId; Name: string; Owner: UserId; Description: string}
and SlideRecorded = {
    Id: SlideId
    OriginalFamily: string
    OriginalGenus: string
    OriginalSpecies: string
    OriginalAuthor: string
    Place: SamplingLocation option
    Time: Age option
    PrepMethod: ChemicalTreatment option
    PrepDate: int<CalYr> option
    Mounting: MountingMedium option
    Taxon: TaxonIdentification }

type State =
| Initial
| Draft of RefState         // Unpublished or published, with outstanding changes
| Published of RefState     // Published version with no current changes

and RefState = {
    Owner: UserId
    Curators: UserId list
    Name: string
    Description: string option
    CurrentVersion: ColVersion
    Slides: SlideState list}
and SlideState = {
    Id: string
    IsFullyDigitised: bool
    OriginalFamily: string
    OriginalGenus: string
    OriginalSpecies: string
    OriginalAuthor: string
    Identification: IdentificationStatus
    Images: Image list
    Place: SamplingLocation option
    Time: Age option
    PrepMethod: ChemicalTreatment option
    PrepDate: int<CalYr> option
    Mounting: MountingMedium option }

let getSlideId (SlideId (c,x)) = x

let isFullyDigitised (slide:SlideState) =
    match slide.Identification with
    | Confirmed _ ->
        match slide.Images |> List.length with
        | l when l > 0 -> true
        | _ -> false
    | _ -> false

let create (command:CreateCollection) state =
    [DigitisationStarted {Id = command.Id; Name = command.Name; Owner = command.Owner; Description = command.Description }]

let publish getTime (id:CollectionId) state =
    match state with
    | Initial -> "This collection does not exist" |> invalidOp
    | Published c -> "The collection" + c.Name + "ha no pending changes for publication" |> invalidOp
    | Draft c ->
        match c.Slides.Length with
        | 0 -> invalidOp "Cannot publish an empty collection"
        | _ -> [CollectionPublished (id, getTime(), c.CurrentVersion |> ColVersion.increment ) ]

let addSlide (command:AddSlide) calcIdentity state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | Published c
    | Draft c ->
        let slideId = 
            match command.ExistingId with
            | Some i ->
                match c.Slides |> List.tryFind (fun s -> s.Id = i) with
                | Some _ -> invalidOp "Slide ID already in use"
                | None -> i
            | None ->
                let lastId = 
                    match c.Slides.Length with
                    | 0 -> 0
                    | _ ->
                        c.Slides 
                        |> List.map ( fun s -> match s.Id with | Prefix "GPP" rest -> int rest | _ -> 0 )
                        |> List.max
                sprintf "GPP%i" (lastId + 1)
        let identity = calcIdentity [command.Taxon]
        let recordedEvent = SlideRecorded {
                Id = SlideId (command.Collection,slideId)
                Taxon = command.Taxon
                OriginalFamily = command.OriginalFamily
                OriginalGenus = command.OriginalGenus
                OriginalSpecies = command.OriginalSpecies
                OriginalAuthor = command.OriginalAuthor
                Mounting = command.Mounting
                Place = command.Place
                Time = command.Time
                PrepMethod = command.PrepMethod
                PrepDate = command.PrepDate }
        match identity with
        | Some taxon -> [ recordedEvent; SlideGainedIdentity ((SlideId (command.Collection,slideId)),taxon) ]
        | None -> [ recordedEvent ]

let uploadImage (command:UploadSlideImage) state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | Published c
    | Draft c -> 
        let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId command.Id)
        match slide with
        | None -> invalidOp "Slide does not exist"
        | Some s ->
            match isFullyDigitised {s with Images = command.Image :: s.Images } with
            | true -> [SlideImageUploaded (command.Id, command.Image); SlideFullyDigitised command.Id]
            | false -> [SlideImageUploaded (command.Id, command.Image)]

let handle deps = 
    function
    | CreateCollection c -> create c
    | Publish c -> publish deps.GetTime c
    | AddSlide c -> addSlide c deps.CalculateIdentity
    | UploadSlideImage c -> uploadImage c

type State with
    static member Evolve state = function

        | CollectionPublished (id,time,version) ->
            match state with
            | Initial -> invalidOp "Collection is empty"
            | Published c -> invalidOp "Invalid state evolution"
            | Draft c -> Published { c with CurrentVersion = version }

        | DigitisationStarted event ->
            match state with
            | Initial ->
                Draft {
                    Name = event.Name
                    CurrentVersion = ColVersion.initial
                    Owner = event.Owner
                    Description = Some event.Description
                    Slides = []
                    Curators = []
                }
            | _ -> invalidOp "Digitisation has already started for this collection"
        
        | SlideRecorded event ->
            match state with
            | Initial -> invalidOp "You must create a collection before adding a slide to it"
            | Published c
            | Draft c ->
                let newSlide = {
                    IsFullyDigitised = false
                    Id = getSlideId event.Id
                    Identification = Partial [event.Taxon]
                    Images = []
                    OriginalFamily = event.OriginalFamily
                    OriginalGenus = event.OriginalGenus
                    OriginalSpecies = event.OriginalSpecies
                    OriginalAuthor = event.OriginalAuthor
                    Place = event.Place
                    Time = event.Time
                    PrepMethod = event.PrepMethod
                    PrepDate = event.PrepDate
                    Mounting = event.Mounting }
                Draft { c with Slides = newSlide :: c.Slides }

        | SlideGainedIdentity (id,tid) ->
            match state with
            | Initial -> invalidOp "Collection does not exist"
            | Published c
            | Draft c ->
                let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId id)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let currentIds = 
                        match s.Identification with
                        | Unidentified -> []
                        | Partial ids -> ids
                        | Confirmed (ids,t) -> ids
                    let updatedSlide = { s with Identification = Confirmed (currentIds,tid) }
                    let updatedSlides = c.Slides |> List.map (fun x -> if x.Id = s.Id then updatedSlide else x)
                    Draft { c with Slides = updatedSlides }

        | SlideFullyDigitised event ->
            match state with
            | Initial -> invalidOp "Collection has not been started"
            | Published c
            | Draft c ->
                let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId event)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let updatedSlide = { s with IsFullyDigitised = true }
                    let updatedSlides = c.Slides |> List.map (fun x -> if x.Id = s.Id then updatedSlide else x)
                    Draft { c with Slides = updatedSlides }

        | SlideImageUploaded (id,image) ->
            match state with
            | Initial -> invalidOp "Collection has not been started"
            | Published c
            | Draft c ->
                let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId id)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let newSlides = c.Slides |> List.map (fun x -> if x.Id = (getSlideId id) then { x with Images = image :: x.Images } else x)
                    Draft { c with Slides = newSlides }

let getId = 
    let unwrap (CollectionId e) = e
    let unwrapSlideId (SlideId (c,x)) = c
    function
    | CreateCollection c -> unwrap c.Id
    | AddSlide c -> unwrap c.Collection
    | Publish c -> unwrap c
    | UploadSlideImage c -> unwrapSlideId c.Id |> unwrap
