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
and UploadSlideImage = {Id:SlideId; Image:Image}
and AddSlide = 
    {Collection:        CollectionId
     ExistingId:        string option
     Taxon:             TaxonIdentification
     Place:             SamplingLocation option
     OriginalFamily:    string
     OriginalGenus:     string
     OriginalSpecies:   string
     OriginalAuthor:    string
     Time:              Age option }

type Event =
| DigitisationStarted of DigitisationStarted
| CollectionPublished of CollectionId * DateTime * ColVersion
| SlideRecorded of SlideRecorded
| SlideImageUploaded of SlideId * Image
| SlideFullyDigitised of SlideId
| SlideGainedIdentity of SlideId * TaxonId

and DigitisationStarted = {Id: CollectionId; Name: string; Owner: UserId; Description: string}
and SlideRecorded = {Id: SlideId; OriginalFamily: string; OriginalGenus: string; OriginalSpecies: string; Taxon: TaxonIdentification}

type State =
| Initial
| Draft of RefState

and RefState = {
    Owner: UserId
    Curators: UserId list
    Name: string
    Description: string option
    CurrentVersion: ColVersion
    Slides: SlideState list}
and SlideState = {
    Id: string
    OriginalFamily: string
    OriginalGenus: string
    OriginalSpecies: string
    Identification: IdentificationStatus
    Images: Image list
}

let create (command:CreateCollection) state =
    [DigitisationStarted {Id = command.Id; Name = command.Name; Owner = command.Owner; Description = command.Description }]

let publish (id:CollectionId) state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | Draft c ->
        match c.Slides.Length with
        | 0 -> invalidOp "Cannot publish an empty collection"
        | _ -> [CollectionPublished (id, DateTime.Now, c.CurrentVersion |> ColVersion.increment ) ]

let addSlide (command:AddSlide) calcIdentity state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | Draft c ->
        let slideId = 
            match command.ExistingId with
            | None -> sprintf "GPP%i" (c.Slides.Length + 1)
            | Some i -> i
        let identity = calcIdentity [command.Taxon]
        match identity with
        | Some taxon -> 
            [SlideRecorded {
                Id = SlideId (command.Collection,slideId)
                Taxon = command.Taxon
                OriginalFamily = command.OriginalFamily
                OriginalGenus = command.OriginalGenus
                OriginalSpecies = command.OriginalSpecies }; 
             SlideGainedIdentity ((SlideId (command.Collection,slideId)),taxon) ]
        | None -> 
            [SlideRecorded {
                Id = SlideId (command.Collection,slideId)
                Taxon = command.Taxon
                OriginalFamily = command.OriginalFamily
                OriginalGenus = command.OriginalGenus
                OriginalSpecies = command.OriginalSpecies } ]

let uploadImage (command:UploadSlideImage) state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | Draft c -> [SlideImageUploaded (command.Id, command.Image)]
 
let handle deps = 
    function
    | CreateCollection c -> create c
    | Publish c -> publish c
    | AddSlide c -> addSlide c deps.CalculateIdentity
    | UploadSlideImage c -> uploadImage c

let getSlideId (SlideId (c,x)) = x

type State with
    static member Evolve state = function

        | CollectionPublished (id,time,version) ->
            match state with
            | Initial -> invalidOp "Collection is empty"
            | Draft c -> Draft { c with CurrentVersion = version }

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
            | Draft c ->
                let newSlide = {
                    Id = getSlideId event.Id
                    Identification = Partial [event.Taxon]
                    Images = []
                    OriginalFamily = event.OriginalFamily
                    OriginalGenus = event.OriginalGenus
                    OriginalSpecies = event.OriginalSpecies
                }
                Draft { c with Slides = newSlide :: c.Slides }

        | SlideGainedIdentity (id,tid) -> state

        | SlideFullyDigitised event ->
            match state with
            | Initial -> invalidOp "Collection has not been started"
            | Draft c ->
                let slide = c.Slides |> List.tryFind (fun s -> s.Id = getSlideId event)
                match slide with
                | Some s -> state
                | None -> invalidOp "Slide does not exist"

        | SlideImageUploaded (id,image) ->
            match state with
            | Initial -> invalidOp "Collection has not been started"
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
