module ReferenceCollectionTests

open System
open Xunit
open GlobalPollenProject.Core.DomainTypes    
open GlobalPollenProject.Core.Aggregate
open GlobalPollenProject.Core.Aggregates.ReferenceCollection

let a = {
    initial = State.Initial
    evolve = State.Evolve
    handle = handle
    getId = getId 
}
let Given = Given a domainDefaultDeps

// Default values for test parameters
let currentUser = UserId (Guid.NewGuid())
let collection = CollectionId (Guid.NewGuid())
let person = Person (["A"; "C"], "McTest")
let id = Botanical (TaxonId (Guid.NewGuid()), Unknown, person)
let place = Country "Nigeria"
let age = CollectionDate 1987<CalYr>
let curatorId = UserId (Guid.NewGuid())

let image = RelativeUrl "image.png"
let focusImage = FocusImage ([image; image; image; image; image],Fixed 2.3<um>,(MagnificationId (CalibrationId Guid.Empty,100)))
let singleImage = SingleImage (image, {Point1 = 2,3; Point2 = 5,8; MeasuredDistance = 4.<um>})

let slideRequest = {
    Collection          = collection
    Taxon               = id
    OriginalFamily      = "Compositae"
    OriginalGenus       = "Aster"
    OriginalSpecies     = "communis"
    OriginalAuthor      = "L."
    ExistingId          = None
    Place               = None
    Time                = None
    PrepMethod          = None
    PrepDate            = None
    Mounting            = None
    PreparedBy          = Person.Unknown }

let slideRecorded = {
    Id                  = SlideId (collection,"GPP1")
    Taxon               = id
    OriginalFamily      = "Compositae"
    OriginalGenus       = "Aster"
    OriginalSpecies     = "communis"
    OriginalAuthor      = "L."
    Place               = None
    Time                = None
    PrepMethod          = None
    PrepDate            = None
    Mounting            = None }

let collectionCurator = {
    Forenames = ["William"; "Testy"]
    Surname = "McTest"
    Contact = Email <| EmailAddress "hello@somemadeuptestaddress.com"
}

let materialAccess = Institutional { Name = ShortText "Contoso University"; Web = Some <| Url "https://globalpollenproject.org" } |> PrimaryLocation


module ``When digitising reference material`` =

    [<Fact>]
    let ``An empty draft is initially created`` () =
        Given []
        |> When ( CreateCollection {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"})
        |> Expect [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}]

    [<Fact>]
    let ``An empty collection cannot be published`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"} ]
        |> When (Publish collection)
        |> ExpectInvalidOp

    [<Fact>]
    let ``A collection must have a curator assigned before publication can proceed`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded ]
        |> When (Publish collection)
        |> ExpectInvalidOp

    [<Fact>]
    let ``A collection with slides and a curator can be submitted for publication`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                PublicAccessAssigned (collection, collectionCurator, materialAccess)
                SlideRecorded slideRecorded ]
        |> When (Publish collection)
        |> Expect [ RequestedPublication collection ]

    [<Fact>]
    let ``A collection must gain approval from a GPP curator before publication`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                PublicAccessAssigned (collection, collectionCurator, materialAccess)
                SlideRecorded slideRecorded ]
        |> When (Publish collection)
        |> Expect [ RequestedPublication collection ]

    [<Fact>]
    let ``A GPP curator can approve a collection which publishes it`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                RequestedPublication collection ]
        |> When (IssuePublicationDecision(collection,Approved,curatorId))
        |> Expect [ CollectionPublished (collection,domainDefaultDeps.GetTime(),ColVersion.initial |> ColVersion.increment) ]

    [<Fact>]
    let ``A GPP curator can return a collection for revision to the owner`` () =
        let notes = LongformText "This collection is not very good"
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                RequestedPublication collection ]
        |> When (IssuePublicationDecision(collection,RevisionRequired notes,curatorId))
        |> Expect [ RevisionAdvised(collection,notes) ]

    [<Fact>]
    let ``A collection with no changes since the last publication cannot be published again`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                CollectionPublished (collection, domainDefaultDeps.GetTime(), ColVersion 1) ]
        |> When (Publish collection)
        |> ExpectInvalidOp

    [<Fact>]
    let ``A collection with changes since the last publication is published with incremented version number`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                CollectionPublished (collection, domainDefaultDeps.GetTime(), ColVersion 1) 
                SlideRecorded { slideRecorded with Id = SlideId (collection,"GPP2") }
                RequestedPublication collection ]
        |> When (IssuePublicationDecision(collection,Approved,curatorId))
        |> Expect [ CollectionPublished (collection,domainDefaultDeps.GetTime(),ColVersion 2) ]

module ``When uploading a slide`` =

    [<Fact>]
    let ``A slide is added to the reference collection`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Some test"} ]
        |> When (AddSlide slideRequest)
        |> Expect [ SlideRecorded slideRecorded]

    [<Fact>]
    let ``Slide ID is incremented from zero`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Some test"}
                SlideRecorded slideRecorded ]
        |> When (AddSlide slideRequest)
        |> Expect [ SlideRecorded {slideRecorded with Id = SlideId (collection,"GPP2")} ]

    [<Fact>]
    let ``The given slide ID is incremented from the last GPP# ID when not specified`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Some test"}
                SlideRecorded {slideRecorded with Id = SlideId (collection,"CUST01")} ]
        |> When (AddSlide slideRequest)
        |> Expect [ SlideRecorded {slideRecorded with Id = SlideId (collection,"GPP1")} ]

    [<Fact>]
    let ``Slide IDs must be unique`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Some test"}
                SlideRecorded slideRecorded ]
        |> When (AddSlide {slideRequest with ExistingId = Some "GPP1"} )
        |> ExpectInvalidOp

    [<Fact>]
    let ``Focus images can be uploaded after calibration`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded ]
        |> When ( UploadSlideImage {Id = SlideId (collection, "GPP1"); Image = focusImage; YearTaken = None })
        |> Expect [ SlideImageUploaded ((SlideId (collection, "GPP1")), focusImage, None)  ]

    [<Fact>]
    let ``Single images do not require a calibration set`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded ]
        |> When ( UploadSlideImage {Id = SlideId (collection, "GPP1"); Image = singleImage; YearTaken = None })
        |> Expect [ SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)  ]

module ``When correcting mistakes`` =

    [<Fact>]
    let ``A slide registered with errors can be voided in whole`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Some test"}
                SlideRecorded slideRecorded ]
        |> When (VoidSlide slideRecorded.Id )
        |> Expect [ SlideVoided slideRecorded.Id ]

    [<Fact>]
    let ``The ID of a void slide is freed-up so that another can be added`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Some test"}
                SlideRecorded slideRecorded
                SlideVoided slideRecorded.Id ]
        |> When (AddSlide slideRequest)
        |> Expect [ SlideRecorded slideRecorded]

module ``When delineating individual specimens on a slide`` =

    [<Fact>]
    let ``At least one delineation must be specified`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None) ]
        |> When ( DelineateSpecimensOnImage {Slide = SlideId (collection, "GPP1")
                                             Image = 1
                                             By = currentUser
                                             Delineations = [] } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``The image number must match one for the slide`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None) ]
        |> When ( DelineateSpecimensOnImage {Slide = SlideId (collection, "GPP1")
                                             Image = 2
                                             By = currentUser
                                             Delineations = [] } )
        |> ExpectInvalidOp

    // [<Fact>]
    // let ``Each delineation must be within the bounds of the image`` () =
    //     invalidOp "Cool"

    // [<Fact>]
    // let ``A user cannot delineate grains on the same image more than once`` () =
    //     invalidOp "Cool"

    // [<Fact>]
    // let ``Every delineation is recorded`` () =
    //     invalidOp "Cool"

    // [<Fact>]
    // let ``Where a delineation is in peer-blind agreement, the delineation is confirmed`` () =
    //     invalidOp "Cool"
