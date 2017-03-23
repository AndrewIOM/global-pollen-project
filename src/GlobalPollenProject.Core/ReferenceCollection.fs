module GlobalPollenProject.Core.Aggregates.ReferenceCollection

open GlobalPollenProject.Core.Types
open System

type Command =
| CreateCollection of CreateCollection
| AddSlide of AddSlide
| UploadSlideImage of UploadSlideImage
| Publish of CollectionId

and CreateCollection = {Id:CollectionId; Name:string; Owner:UserId}
and UploadSlideImage = {Id:SlideId; Image:Image}
and AddSlide = 
    {Id:        CollectionId
     Taxon:     TaxonIdentification
     Place:     SamplingLocation option
     Time:      Age option}

type Event =
| DigitisationStarted of DigitisationStarted
| CollectionPublished of CollectionId
| SlideRecorded of SlideRecorded
| SlideImageUploaded of SlideId * Image
| SlideFullyDigitised of SlideId

and DigitisationStarted = {Id: CollectionId; Name: string; Owner: UserId}
and SlideRecorded = {Id: SlideId; Taxon: TaxonIdentification}

type State =
| Initial
| Draft of RefState
| Complete of RefState

and RefState = {
    Owner: UserId
    Curators: UserId list
    Name: string
    Description: string option
    Slides: SlideState list}
and SlideState = {
    Identification: IdentificationStatus
    Images: Image list
}

let create (command:CreateCollection) state =
    [DigitisationStarted {Id = command.Id; Name = command.Name; Owner = command.Owner}]

let publish command state =
    match state with
    | Complete c -> invalidOp "Cannot publish an already published collection"
    | Initial -> invalidOp "This collection does not exist"
    | Draft c ->
        match c.Slides.Length with
        | 0 -> invalidOp "Cannot publish an empty collection"
        | _ -> [CollectionPublished command]

let addSlide (command:AddSlide) calcIdentity state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | Draft c ->
        let slideId = sprintf "GPP%i" (c.Slides.Length + 1)
        let identity = calcIdentity [command.Taxon]
        match identity with
        | Some taxon -> 
            [SlideRecorded {Id = SlideId (command.Id,slideId); Taxon = command.Taxon};]
        | None -> 
            [SlideRecorded {Id = SlideId (command.Id,slideId); Taxon = command.Taxon};]
    | Complete c -> invalidOp "Cannot publish an already published collection"

let uploadImage (command:UploadSlideImage) state =
    match state with
    | Complete c -> invalidOp "Cannot publish an already published collection"
    | Initial -> invalidOp "This collection does not exist"
    | Draft c -> [SlideImageUploaded (command.Id, command.Image)]
 
let handle deps = 
    function
    | CreateCollection c -> create c
    | Publish c -> publish c
    | AddSlide c -> addSlide c deps.CalculateIdentity
    | UploadSlideImage c -> uploadImage c


type State with
    static member Evolve state = function

        | CollectionPublished event ->
            state

        | DigitisationStarted event ->
            match state with
            | Initial ->
                Draft {
                    Name = event.Name
                    Owner = event.Owner
                    Description = None
                    Slides = []
                    Curators = []
                }
            | _ -> invalidOp "Digitisation has already started for this collection"
        
        | SlideRecorded event ->
            match state with
            | Initial -> invalidOp "You must create a collection before adding a slide to it"
            | Complete c -> invalidOp "You cannot add slides to completed collections"
            | Draft c ->
                let newSlide = {
                    Identification = Partial [event.Taxon]
                    Images = []
                }
                Draft { c with Slides = newSlide :: c.Slides }

        | SlideFullyDigitised event ->
            state

        | SlideImageUploaded (e,x) ->
            state


let getId = 
    let unwrap (CollectionId e) = e
    function
    | CreateCollection c -> unwrap c.Id
    | AddSlide c -> unwrap c.Id
    | Publish c -> unwrap c
    //| UploadSlideImage c -> unwrap (fst c.Id)
