module GlobalPollenProject.Core.Aggregates.ReferenceCollection

open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregate
open System

type RevisionNotes = string

type Command =
| CreateCollection of CreateCollection
| AddSlide of AddSlide
| UploadSlideImage of UploadSlideImage
| Publish of CollectionId
| IssuePublicationDecision of CollectionId * PublicationDecision * UserId

and CreateCollection = {Id:CollectionId; Name:string; Owner:UserId; Description: string}
and UploadSlideImage = {Id:SlideId; Image:Image; YearTaken:int<CalYr> option }
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

and PublicationDecision =
| Approved
| RevisionRequired of RevisionNotes 

type Event =
| DigitisationStarted of DigitisationStarted
| RequestedPublication of CollectionId
| CollectionPublished of CollectionId * DateTime * ColVersion
| SlideRecorded of SlideRecorded
| SlideImageUploaded of SlideId * Image * int<CalYr> option
| SlideFullyDigitised of SlideId
| SlideGainedIdentity of SlideId * TaxonId
| RevisionAdvised of CollectionId * RevisionNotes

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
| Draft of RefState
| PublicationRequested of RefState
| InRevision of RefState * RevisionNotes
| Published of RefState

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
    Images: ImageState list
    Place: SamplingLocation option
    Time: Age option
    PrepMethod: ChemicalTreatment option
    PrepDate: int<CalYr> option
    Mounting: MountingMedium option }
and ImageState = {
    Image: Image
    YearTaken: int<CalYr> option
}

let getSlideId (SlideId (c,x)) = x

let isFullyDigitised (slide:SlideState) =
    match slide.Identification with
    | Confirmed _ ->
        match slide.Images |> List.length with
        | l when l > 0 -> true
        | _ -> false
    | _ -> false

let create (command:CreateCollection) state =
    match state with
    | Initial ->
        [DigitisationStarted {Id = command.Id; Name = command.Name; Owner = command.Owner; Description = command.Description }]
    | _ -> invalidOp "Collection already exists"

let publish (id:CollectionId) state =
    match state with
    | Initial -> "This collection does not exist" |> invalidOp
    | Published c -> "The collection" + c.Name + "ha no pending changes for publication" |> invalidOp
    | PublicationRequested c -> "Publication has already been requested" |> invalidOp
    | InRevision (c,_)
    | Draft c ->
        match c.Slides.Length with
        | 0 -> invalidOp "Cannot publish an empty collection"
        | _ -> [ RequestedPublication id ]

let issueDecision getTime (id:CollectionId) decision userId state =
    match state with
    | Initial -> "This collection does not exist" |> invalidOp
    | Draft c
    | InRevision (c,_)
    | Published c -> "No decision is currently required" |> invalidOp
    | PublicationRequested c ->
        match decision with
        | Approved -> 
            [CollectionPublished (id, getTime(), c.CurrentVersion |> ColVersion.increment ) ]
        | RevisionRequired msg -> 
            [RevisionAdvised (id,msg)]

let addSlide (command:AddSlide) calcIdentity state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | PublicationRequested c -> "This collection is under review for publication and cannot accept changes" |> invalidOp
    | Published c
    | InRevision (c,_)
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
    | PublicationRequested c -> "This collection is under review for publication and cannot accept changes" |> invalidOp
    | Published c
    | InRevision (c,_)
    | Draft c -> 
        let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId command.Id)
        match slide with
        | None -> invalidOp "Slide does not exist"
        | Some s ->
            let imageState = { Image = command.Image; YearTaken = command.YearTaken }
            match isFullyDigitised {s with Images = imageState :: s.Images } with
            | true -> [SlideImageUploaded (command.Id, command.Image, command.YearTaken); SlideFullyDigitised command.Id]
            | false -> [SlideImageUploaded (command.Id, command.Image, command.YearTaken)]

let handle deps = 
    function
    | CreateCollection c -> create c
    | Publish c -> publish c
    | AddSlide c -> addSlide c deps.CalculateIdentity
    | UploadSlideImage c -> uploadImage c
    | IssuePublicationDecision (c,dec,userId) -> issueDecision deps.GetTime c dec userId

type State with
    static member Evolve state = function

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

        | RequestedPublication id ->
            match state with
            | Initial
            | Published _
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | InRevision (s,_)
            | Draft s -> PublicationRequested s

        | CollectionPublished (id,time,version) ->
            match state with
            | Initial
            | InRevision _
            | Published _ -> invalidOp "Invalid state transition"
            | Draft c
            | PublicationRequested c -> Published { c with CurrentVersion = version }

        | RevisionAdvised (id,note) ->
            match state with
            | Initial
            | Published _
            | InRevision _
            | Draft _ -> invalidOp "Invalid state transition"
            | PublicationRequested s -> 
                InRevision (s,note)
        
        | SlideRecorded event ->
            match state with
            | Initial -> invalidOp "You must create a collection before adding a slide to it"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
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
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
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
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
            | Draft c ->
                let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId event)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let updatedSlide = { s with IsFullyDigitised = true }
                    let updatedSlides = c.Slides |> List.map (fun x -> if x.Id = s.Id then updatedSlide else x)
                    Draft { c with Slides = updatedSlides }

        | SlideImageUploaded (id,image,year) ->
            match state with
            | Initial -> invalidOp "Collection has not been started"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
            | Draft c ->
                let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId id)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let newImage = { Image = image; YearTaken = year }
                    let newSlides = c.Slides |> List.map (fun x -> if x.Id = (getSlideId id) then { x with Images = newImage :: x.Images } else x)
                    Draft { c with Slides = newSlides }

let getId = 
    let unwrap (CollectionId e) = e
    let unwrapSlideId (SlideId (c,x)) = c
    function
    | CreateCollection c -> unwrap c.Id
    | AddSlide c -> unwrap c.Collection
    | Publish c -> unwrap c
    | UploadSlideImage c -> unwrapSlideId c.Id |> unwrap
    | IssuePublicationDecision (c,_,_) -> unwrap c