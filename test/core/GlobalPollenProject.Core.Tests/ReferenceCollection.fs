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

    let validDelineation = 
        {   Slide = SlideId (collection, "GPP1")
            Image = 1
            By = currentUser
            Delineations = [{
                TopLeft = { X = 352<pixels>; Y = 786<pixels> }
                BottomRight = { X = 432<pixels>; Y = 798<pixels> }
            }] }

    let delineation x1 y1 x2 y2 =
        {   Slide = SlideId (collection, "GPP1")
            Image = 1
            By = currentUser
            Delineations = [{
                TopLeft = { X = x1; Y = y1 }
                BottomRight = { X = x2; Y = y2 }
            }] }

    let delineationCombine d1 d2 = 
        {   Slide = SlideId (collection, "GPP1")
                    Image = 1
                    By = currentUser
                    Delineations = List.concat [d1; d2] }

    [<Fact>]
    let ``At least one delineation must be specified`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1) ]
        |> When ( DelineateSpecimensOnImage {Slide = SlideId (collection, "GPP1")
                                             Image = 1
                                             By = currentUser
                                             Delineations = [] } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``The image number must match one for the slide`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1) ]
        |> When ( DelineateSpecimensOnImage {Slide = SlideId (collection, "GPP1")
                                             Image = 2
                                             By = currentUser
                                             Delineations = [] } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``Each delineation must be within the bounds of the image`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1) ]
        |> When ( DelineateSpecimensOnImage {Slide = SlideId (collection, "GPP1")
                                             Image = 1
                                             By = currentUser
                                             Delineations = [{
                                                 TopLeft = { X = 500<pixels>; Y = 1500<pixels> }
                                                 BottomRight = { X = 1000<pixels>; Y = 1000<pixels> }
                                             }] } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``A user cannot delineate grains on the same image more than once`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, currentUser, validDelineation.Delineations) ]
        |> When ( DelineateSpecimensOnImage validDelineation )
        |> ExpectInvalidOp

    [<Fact>]
    let ``Grains cannot be delineated on an unpublished slide`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None) ]
        |> When ( DelineateSpecimensOnImage validDelineation )
        |> ExpectInvalidOp

    [<Fact>]
    let ``A valid delineation is recorded`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1) ]
        |> When ( DelineateSpecimensOnImage validDelineation )
        |> Expect [ SpecimensDelineated ((SlideId (collection, "GPP1")), 1, currentUser, validDelineation.Delineations) ]

    [<Fact>]
    let ``Where a delineation is in peer blind agreement, the delineation is confirmed`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), validDelineation.Delineations)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), validDelineation.Delineations) ]
        |> When ( DelineateSpecimensOnImage validDelineation )
        |> Expect [ 
            SpecimensDelineated ((SlideId (collection, "GPP1")), 1, currentUser, validDelineation.Delineations)
            SpecimenConfirmed ((SlideId (collection, "GPP1")), 1, validDelineation.Delineations.Head) ]

    [<Fact>]
    let ``Delineations are not confirmed when they overlap by more than twenty percent area`` () =
        let d1 = delineation 120<pixels> 120<pixels> 140<pixels> 140<pixels>
        let d2 = delineation 131<pixels> 120<pixels> 140<pixels> 170<pixels>
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), List.concat [d1.Delineations; d2.Delineations])
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), List.concat [d1.Delineations; d2.Delineations]) ]
        |> When ( DelineateSpecimensOnImage (delineationCombine d1.Delineations d2.Delineations) )
        |> Expect [ 
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, currentUser, List.concat [d1.Delineations; d2.Delineations])
                SpecimenConfirmed ((SlideId (collection, "GPP1")), 1, d1.Delineations.Head) ]

    [<Fact>]
    let ``Two non-overlapping delineations can be confirmed at once`` () =
        let d1 = delineation 120<pixels> 120<pixels> 140<pixels> 140<pixels>
        // 80 pixel overlap threshold.
        let d2 = delineation 350<pixels> 350<pixels> 450<pixels> 450<pixels>
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), List.concat [d1.Delineations; d2.Delineations])
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), List.concat [d1.Delineations; d2.Delineations]) ]
        |> When ( DelineateSpecimensOnImage (delineationCombine d1.Delineations d2.Delineations) )
        |> Expect [ 
            SpecimensDelineated ((SlideId (collection, "GPP1")), 1, currentUser, List.concat [ d1.Delineations; d2.Delineations])
            SpecimenConfirmed ((SlideId (collection, "GPP1")), 1, d1.Delineations.Head)
            SpecimenConfirmed ((SlideId (collection, "GPP1")), 1, d2.Delineations.Head) ]

    [<Fact>]
    let ``Delineation is confirmed where each is within 10px of another with largest possible extent`` () =
        let d1 = delineation 120<pixels> 120<pixels> 140<pixels> 140<pixels>
        let d2 = delineation 129<pixels> 129<pixels> 149<pixels> 149<pixels>
        let conf = delineation 120<pixels> 120<pixels> 149<pixels> 149<pixels>
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), d1.Delineations)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), d1.Delineations) ]
        |> When ( DelineateSpecimensOnImage d2 )
        |> Expect [ 
            SpecimensDelineated ((SlideId (collection, "GPP1")), 1, currentUser, d2.Delineations)
            SpecimenConfirmed ((SlideId (collection, "GPP1")), 1, conf.Delineations.Head) ]

    [<Fact>]
    let ``Delineation is not confirmed when maximum distances between corners is greater than 10px`` () =
        let d1 = delineation 120<pixels> 120<pixels> 140<pixels> 140<pixels>
        let d2 = delineation 131<pixels> 129<pixels> 149<pixels> 149<pixels>
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded slideRecorded
                SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage, None)
                CollectionPublished (collection, DateTime.Now, ColVersion 1)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), d1.Delineations)
                SpecimensDelineated ((SlideId (collection, "GPP1")), 1, UserId <| Guid.NewGuid(), d1.Delineations) ]
        |> When ( DelineateSpecimensOnImage d2 )
        |> Expect [ 
            SpecimensDelineated ((SlideId (collection, "GPP1")), 1, currentUser, d2.Delineations) ]
    