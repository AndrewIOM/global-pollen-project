module GlobalPollenProject.Core.Aggregates.ReferenceCollection

open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregate
open System

type RevisionNotes = LongformText
type ImageNumber = int
type SpecimenDelineation = CartesianBox

type Institution = {
    Name: ShortText
    Web: Url option
}

type ContactDetail =
| Email of EmailAddress

type Curator = {
    Forenames: FirstName list
    Surname: Surname
    Contact: ContactDetail
}
type Digitiser = Person
type PreppedBy = Person

type CollectionLocation =
| Institutional of Institution
| Personal

type AccessToMaterial =
| DigitialOnly
| PrimaryLocation of CollectionLocation
| ReplicateLocations of CollectionLocation * (CollectionLocation list)

type Command =
| CreateCollection          of CreateCollection
| AddSlide                  of AddSlide
| VoidSlide                 of SlideId
| UploadSlideImage          of UploadSlideImage
| Publish                   of CollectionId
| IssuePublicationDecision  of CollectionId * PublicationDecision * UserId
| CreditDigitiser           of CollectionId * Digitiser
| SpecifyCurator            of CollectionId * Curator * AccessToMaterial
| RegisterReplicate         of CollectionId * Institution
| DelineateSpecimensOnImage of DelineateSpecimensOnImage

and CreateCollection = {
    Id:CollectionId
    Name:string
    Owner:UserId
    Description: string }

and AddSlide = 
    {Collection:            CollectionId
     Taxon:                 TaxonIdentification
     OriginalFamily:        string
     OriginalGenus:         string
     OriginalSpecies:       string
     OriginalAuthor:        string
     ExistingId:            string option
     Place:                 SamplingLocation option
     Time:                  Age option
     PrepMethod:            ChemicalTreatment option
     PrepDate:              int<CalYr> option
     Mounting:              MountingMedium option
     PreparedBy:            Person }

and UploadSlideImage = {Id:SlideId; Image:Image; YearTaken:int<CalYr> option }

and PublicationDecision =
| Approved
| RevisionRequired of RevisionNotes 

and DelineateSpecimensOnImage = {
    Slide: SlideId
    Image: ImageNumber
    By: UserId
    Delineations: SpecimenDelineation list
}

type Event =
| DigitisationStarted       of DigitisationStarted
| PublicAccessAssigned      of CollectionId * Curator * AccessToMaterial
| RequestedPublication      of CollectionId
| RevisionAdvised           of CollectionId * RevisionNotes
| CollectionPublished       of CollectionId * DateTime * ColVersion
| SlideRecorded             of SlideRecorded
| SlideVoided               of SlideId
| SlideImageUploaded        of SlideId * Image * int<CalYr> option
| SlideFullyDigitised       of SlideId
| SlideGainedIdentity       of SlideId * TaxonId
| SlidePrepAcknowledged     of SlideId * PreppedBy
| SpecimensDelineated       of SlideId * ImageNumber * UserId * (SpecimenDelineation list)
| SpecimenConfirmed         of SlideId * ImageNumber * SpecimenDelineation

and DigitisationStarted = {
    Id: CollectionId
    Name: string
    Owner: UserId
    Description: string }

and SlideRecorded = {
    Id:                     SlideId
    OriginalFamily:         string
    OriginalGenus:          string
    OriginalSpecies:        string
    OriginalAuthor:         string
    Place:                  SamplingLocation option
    Time:                   Age option
    PrepMethod:             ChemicalTreatment option
    PrepDate:               int<CalYr> option
    Mounting:               MountingMedium option
    Taxon:                  TaxonIdentification }

type State =
| Initial
| Draft of RefState
| PublicationRequested of RefState
| InRevision of RefState * RevisionNotes
| Published of RefState

and RefState = {
    Name: string
    Description: string option
    CurrentVersion: ColVersion
    Owner: UserId
    Digitisers: UserId list
    Curator: Curator option
    PhysicalLocation: AccessToMaterial option
    Slides: Map<string, SlideState> }
and SlideState = {
    Id: string
    IsFullyDigitised: bool
    OriginalFamily: string
    OriginalGenus: string
    OriginalSpecies: string
    OriginalAuthor: string
    Identification: IdentificationStatus
    Images: Map<int,ImageState>
    Place: SamplingLocation option
    Time: Age option
    PrepMethod: ChemicalTreatment option
    PrepDate: int<CalYr> option
    Mounting: MountingMedium option }
and ImageState = {
    Image: Image
    YearTaken: int<CalYr> option
    Delineations: SpecimenDelineation list
    DelineatedBy: UserId list
    Specimens: SpecimenDelineation list
}

let getSlideId (SlideId (_,x)) = x

let isFullyDigitised (slide:SlideState) =
    match slide.Identification with
    | Confirmed _ ->
        match slide.Images.Count with
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
    | PublicationRequested _ -> "Publication has already been requested" |> invalidOp
    | InRevision (c,_)
    | Draft c ->
        match c.Slides.Count with
        | 0 -> invalidOp "Cannot publish an empty collection"
        | _ ->
            match c.Curator with
            | None -> invalidOp "Cannot publish a collection without knowing the curator"
            | Some _ -> [ RequestedPublication id ]

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
    | PublicationRequested _ -> "This collection is under review for publication and cannot accept changes" |> invalidOp
    | Published c
    | InRevision (c,_)
    | Draft c ->
        let slideId = 
            match command.ExistingId with
            | Some i ->
                match c.Slides |> Map.tryFind i with
                | Some _ -> invalidOp "Slide ID already in use"
                | None -> i
            | None ->
                let lastId = 
                    match c.Slides.Count with
                    | 0 -> 0
                    | _ ->
                        c.Slides.Keys
                        |> Seq.map ( fun k -> match k with | Prefix "GPP" rest -> int rest | _ -> 0 )
                        |> Seq.max
                sprintf "GPP%i" (lastId + 1)
        let identity = 
            match calcIdentity [command.Taxon] with
            | Ok t -> t
            | Error _ -> invalidOp "Could not calculate identity"
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
        let prepEvent = 
            match command.PreparedBy with
            | Person.Person _ -> [ SlidePrepAcknowledged (SlideId (command.Collection,slideId), command.PreparedBy) ]
            | Person.Unknown -> []
        match identity with
        | Some taxon -> List.append [ recordedEvent; SlideGainedIdentity ((SlideId (command.Collection,slideId)),taxon) ] prepEvent
        | None -> List.append [ recordedEvent ] prepEvent

let uploadImage (command:UploadSlideImage) state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | PublicationRequested _ -> "This collection is under review for publication and cannot accept changes" |> invalidOp
    | Published c
    | InRevision (c,_)
    | Draft c -> 
        match command.Id |> getSlideId |> (fun k -> Map.tryFind k c.Slides) with
        | None -> invalidOp "Slide does not exist"
        | Some s ->
            let imageState = { Image = command.Image; YearTaken = command.YearTaken; Delineations = [] ; DelineatedBy = []; Specimens = [] }
            let imgN = if s.Images.Count > 0 then ((s.Images.Keys |> Seq.max) + 1) else 1
            match isFullyDigitised {s with Images = s.Images |> Map.add imgN imageState } && not s.IsFullyDigitised with
            | true -> [SlideImageUploaded (command.Id, command.Image, command.YearTaken); SlideFullyDigitised command.Id]
            | false -> [SlideImageUploaded (command.Id, command.Image, command.YearTaken)]

let voidSlide slideId state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | PublicationRequested c -> "This collection is under review for publication and cannot accept changes" |> invalidOp
    | Published c
    | InRevision (c,_)
    | Draft c -> 
        let slide = c.Slides |> Map.tryFind (getSlideId slideId)
        match slide with
        | None -> invalidOp "Slide does not exist"
        | Some _ -> [ SlideVoided slideId ]

let specifyCurator id curator access state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | PublicationRequested c -> "This collection is under review for publication and cannot accept changes" |> invalidOp
    | Published _
    | InRevision (_,_)
    | Draft _ -> [ PublicAccessAssigned (id,curator,access) ]

module Delineate =

    let inline diff x1 x2 =
        if x1 < x2 then x2 - x1 else x1 - x2

    let maxExtent (boxes:CartesianBox list) : CartesianBox =
        boxes
        |> Seq.fold(fun box maxBox ->
            { TopLeft = { X = if box.TopLeft.X < maxBox.TopLeft.X then box.TopLeft.X else maxBox.TopLeft.X
                          Y = if box.TopLeft.Y < maxBox.TopLeft.Y then box.TopLeft.Y else maxBox.TopLeft.Y }
              BottomRight = { X = if box.BottomRight.X > maxBox.BottomRight.X then box.BottomRight.X else maxBox.BottomRight.X
                              Y = if box.BottomRight.Y > maxBox.BottomRight.Y then box.BottomRight.Y else maxBox.BottomRight.Y }}
            ) boxes.Head

    let inTolerance tol p s =
        diff p.BottomRight.X s.BottomRight.X < tol &&
        diff p.BottomRight.Y s.BottomRight.Y < tol &&
        diff p.TopLeft.X s.TopLeft.X < tol &&
        diff p.TopLeft.Y s.TopLeft.Y < tol

    let setOfRange start e =
        let removeUnit (x:int<_>) = int x
        seq { removeUnit start .. removeUnit e }
        |> Seq.map(fun i -> i * 1<pixels>)
        |> Set.ofSeq

    let overlapsByArea percent a b =
        let intersectX = Set.intersect (setOfRange a.TopLeft.X a.BottomRight.X) (setOfRange b.TopLeft.X b.BottomRight.X)
        let intersectY = Set.intersect (setOfRange a.TopLeft.Y a.BottomRight.Y) (setOfRange b.TopLeft.Y b.BottomRight.Y)
        if intersectX.IsEmpty || intersectY.IsEmpty
        then false
        else
            let areaA = (a.BottomRight.X - a.TopLeft.X) * (a.BottomRight.Y - a.TopLeft.Y)
            let overlapArea = (intersectX.MaximumElement - intersectX.MinimumElement) * (intersectY.MaximumElement - intersectY.MinimumElement)
            overlapArea >= (percent areaA)

    /// Follows rules to determine likely individual specimens based on
    /// the currently submitted bounding boxes, and the existing 'confirmed'
    /// specimen positions in X-Y space. The rules are:
    /// - Absolute tolerance: 10px
    /// - Maximum overlap of 20% between specimens
    /// - Assess submitted possible positions from oldest to newest
    let checkConfirmed alreadyIdentified sightings =
        Seq.fold(fun (confirmed,previous) s ->
            let similar = previous |> List.where(fun p -> inTolerance 10<pixels> p s)
            if similar.Length < 2
            then (confirmed, previous |> List.append [s])
            else 
                let occ = maxExtent (s :: similar)
                printfn "Found: %A (confirmed = %A) (matches 1 = %A) (matches 2 = %A)" occ confirmed (alreadyIdentified |> List.append confirmed |> List.tryFind (fun b -> inTolerance 10<pixels> occ b)) ((alreadyIdentified |> List.append confirmed |> List.tryFind (fun b -> overlapsByArea (fun i -> i/ 5) occ b)))
                match alreadyIdentified |> List.append confirmed |> List.tryFind (fun b -> inTolerance 10<pixels> occ b || overlapsByArea (fun i -> i/ 5) occ b) with
                | Some _ -> (confirmed, List.append previous [s])
                | None -> (List.append confirmed [occ], List.append previous [s])
            ) ([],[]) sightings |> fst

let delineate (getImageDimension:Image -> Result<Dimensions,string>) (command:DelineateSpecimensOnImage) state =
    match state with
    | Initial -> invalidOp "This collection does not exist"
    | Draft _
    | InRevision _
    | PublicationRequested _ -> "Can only delineate grains in published collections" |> invalidOp
    | Published c ->
        let slide = getSlideId command.Slide |> fun k -> Map.tryFind k c.Slides
        match slide with
        | None -> invalidOp "Slide does not exist"
        | Some s ->
            let img =
                match s.Images |> Map.tryFind command.Image with
                | Some i -> i
                | None -> invalidOp <| sprintf "there is no image number %i for slide %s" command.Image s.Id
            match getImageDimension img.Image with
            | Error e -> invalidOp (sprintf "Could not get image dimensions: %s" e)
            | Ok dims ->
                if img.DelineatedBy |> List.contains command.By
                then invalidOp "Cannot identify grains on a slide twice"
                if command.Delineations |> Seq.isEmpty then invalidOp "Must specify at least one delineation"                
                let valid =
                    command.Delineations
                    |> List.where(fun d ->
                        d.TopLeft.X < d.BottomRight.X &&
                        d.TopLeft.Y < d.BottomRight.Y &&
                        (d.TopLeft.X <> d.BottomRight.X && d.TopLeft.Y <> d.BottomRight.Y) &&
                        d.TopLeft.X > 0<pixels> && d.TopLeft.Y > 0<pixels> &&
                        d.BottomRight.X > 0<pixels> && d.BottomRight.Y > 0<pixels> &&
                        d.TopLeft.X <= dims.Width && d.TopLeft.Y <= dims.Height &&
                        d.BottomRight.X <= dims.Width && d.BottomRight.Y <= dims.Height)
                if valid.Length <> command.Delineations.Length
                then invalidOp "At least one delineation was not within image bounds, was invalid, or was zero pixels"
                let e = SpecimensDelineated (command.Slide, command.Image, command.By, valid)          
                let newlyConfirmed = Delineate.checkConfirmed img.Specimens (List.concat [img.Delineations; valid])
                if newlyConfirmed |> Seq.isEmpty
                then [ e ]
                else e :: (newlyConfirmed |> List.map (fun d -> SpecimenConfirmed (command.Slide, command.Image, d)))


let handle deps = 
    function
    | CreateCollection c -> create c
    | Publish c -> publish c
    | AddSlide c -> addSlide c deps.CalculateIdentity
    | UploadSlideImage c -> uploadImage c
    | IssuePublicationDecision (c,dec,userId) -> issueDecision deps.GetTime c dec userId
    | VoidSlide id -> voidSlide id
    | SpecifyCurator (id,curator,access) -> specifyCurator id curator access
    | DelineateSpecimensOnImage c -> delineate deps.GetImageDimension c
    | CreditDigitiser _ -> invalidOp "Not implemented"
    | RegisterReplicate _ -> invalidOp "Not implemented"

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
                    Slides = Map.empty
                    Digitisers = []
                    Curator = None
                    PhysicalLocation = None
                }
            | _ -> invalidOp "Digitisation has already started for this collection"

        | PublicAccessAssigned (_,curator,accessLevel) ->
            match state with
            | Initial -> invalidOp "Invalid state transition"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | InRevision (c,_)
            | Published c
            | Draft c ->
                Draft { c with PhysicalLocation = Some accessLevel; Curator = Some curator }

        | RequestedPublication _ ->
            match state with
            | Initial
            | Published _
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | InRevision (s,_)
            | Draft s -> PublicationRequested s

        | CollectionPublished (_,_,version) ->
            match state with
            | Initial
            | InRevision _
            | Published _ -> invalidOp "Invalid state transition"
            | Draft c
            | PublicationRequested c -> Published { c with CurrentVersion = version }

        | RevisionAdvised (_,note) ->
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
                    Images = Map.empty
                    OriginalFamily = event.OriginalFamily
                    OriginalGenus = event.OriginalGenus
                    OriginalSpecies = event.OriginalSpecies
                    OriginalAuthor = event.OriginalAuthor
                    Place = event.Place
                    Time = event.Time
                    PrepMethod = event.PrepMethod
                    PrepDate = event.PrepDate
                    Mounting = event.Mounting }
                Draft { c with Slides = c.Slides |> Map.add newSlide.Id newSlide }

        | SlideVoided id ->
            match state with
            | Initial -> invalidOp "You must create a collection before adding a slide to it"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
            | Draft c ->
                let slide = c.Slides |> Map.tryFind (getSlideId id)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let slides = c.Slides |> Map.remove s.Id
                    Draft { c with Slides = slides }

        | SlideGainedIdentity (id,tid) ->
            match state with
            | Initial -> invalidOp "Collection does not exist"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
            | Draft c ->
                let slide = c.Slides |> Map.tryFind (getSlideId id)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let currentIds = 
                        match s.Identification with
                        | Unidentified -> []
                        | Partial ids -> ids
                        | Confirmed (ids,t) -> ids
                    let updatedSlide = { s with Identification = Confirmed (currentIds,tid) }
                    let updatedSlides = c.Slides |> Map.change s.Id (fun s -> if s.IsSome then Some updatedSlide else None)
                    Draft { c with Slides = updatedSlides }

        | SlidePrepAcknowledged (id,p) ->
            match state with
            | Initial -> invalidOp "Collection does not exist"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
            | Draft c -> Draft c

        | SlideFullyDigitised event ->
            match state with
            | Initial -> invalidOp "Collection has not been started"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
            | Draft c ->
                let slide = c.Slides |> Map.tryFind (getSlideId event)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let updatedSlides = c.Slides |> Map.change s.Id (fun s -> if s.IsSome then Some { s.Value with IsFullyDigitised = true } else None)
                    Draft { c with Slides = updatedSlides }

        | SlideImageUploaded (id,image,year) ->
            match state with
            | Initial -> invalidOp "Collection has not been started"
            | PublicationRequested _ -> invalidOp "Invalid state transition"
            | Published c
            | InRevision (c,_)
            | Draft c ->
                let slide = c.Slides |> Map.tryFind (getSlideId id)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let newImage = { Image = image; YearTaken = year; Delineations = []; DelineatedBy = []; Specimens = [] }
                    let imgN = if s.Images.Count > 0 then ((s.Images.Keys |> Seq.max) + 1) else 1
                    let newSlides = c.Slides |> Map.change s.Id (fun x -> if x.IsSome then Some { x.Value with Images = x.Value.Images |> Map.add imgN newImage } else None)
                    Draft { c with Slides = newSlides }

        | SpecimensDelineated (sId, imgN, userId, dels) ->
            match state with
            | Published c ->
                let slide = c.Slides |> Map.tryFind (getSlideId sId)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let img = 
                        if s.Images |> Map.containsKey imgN
                        then s.Images |> Map.find imgN
                        else invalidOp "invalid transform: image not found"
                    let newImg = { img with DelineatedBy = userId :: img.DelineatedBy; Delineations = List.append dels img.Delineations }
                    Published { c with Slides = c.Slides |> Map.change (getSlideId sId) (fun s -> if s.IsSome then Some { s.Value with Images = s.Value.Images |> Map.add imgN newImg } else None) }
            | _ -> invalidOp "Can only delineate grains on published slides"

        | SpecimenConfirmed (sId, imgN, del) ->
            match state with
            | Published c ->
                let slide = c.Slides |> Map.tryFind (getSlideId sId)
                match slide with
                | None -> invalidOp "Slide does not exist"
                | Some s ->
                    let img = 
                        if s.Images |> Map.containsKey imgN
                        then s.Images |> Map.find imgN
                        else invalidOp "invalid transform: image not found"
                    let newImg = { img with Specimens = del :: img.Specimens }
                    Published { c with Slides = c.Slides |> Map.change (getSlideId sId) (fun s -> if s.IsSome then Some { s.Value with Images = s.Value.Images |> Map.add imgN newImg } else None) }
            | _ -> invalidOp "Can only delineate grains on published slides"

let getId = 
    let unwrap (CollectionId e) = e
    let unwrapSlideId (SlideId (c,_)) = c
    function
    | CreateCollection c -> unwrap c.Id
    | AddSlide c -> unwrap c.Collection
    | VoidSlide sid -> unwrapSlideId sid |> unwrap
    | Publish c -> unwrap c
    | UploadSlideImage c -> unwrapSlideId c.Id |> unwrap
    | IssuePublicationDecision (c,_,_) -> unwrap c
    | SpecifyCurator (id,_,_) -> unwrap id
    | DelineateSpecimensOnImage c -> unwrapSlideId c.Slide |> unwrap
    | CreditDigitiser (id,_) -> unwrap id
    | RegisterReplicate (id,_) -> unwrap id
