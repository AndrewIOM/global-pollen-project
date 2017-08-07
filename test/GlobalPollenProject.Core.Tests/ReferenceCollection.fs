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
let id = Botanical (TaxonId (Guid.NewGuid()), Book "Fake book")
let place = Country "Nigeria"
let age = CollectionDate 1987<CalYr>

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
    let ``A collection with slides can be published`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Test"}
                SlideRecorded {Id               = SlideId (collection, "GPP1")
                               OriginalFamily   = ""
                               OriginalGenus    = ""
                               OriginalSpecies  = ""
                               Taxon            = id }]
        |> When (Publish collection)
        |> Expect [ CollectionPublished (collection,domainDefaultDeps.GetTime(),ColVersion.initial |> ColVersion.increment) ]

    
module ``When uploading a slide`` =

    let image = Url.create "https://sometesturl"
    let focusImage = FocusImage ([image; image; image; image; image],Fixed 2.3<um>,(MagnificationId (CalibrationId Guid.Empty,100)))
    let singleImage = SingleImage (image, {Point1 = 2,3; Point2 = 5,8; MeasuredDistance = 4.<um>})

    [<Fact>]
    let ``A slide is added to the reference collection`` () =
        Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser; Description = "Some test"} ]
        |> When (AddSlide { Collection         = collection
                            ExistingId         = None
                            Taxon              = id
                            Place              = Some place
                            OriginalFamily     = ""
                            OriginalGenus      = ""
                            OriginalSpecies    = ""
                            OriginalAuthor     = ""
                            Time               = Some age })
        |> Expect [ SlideRecorded {Id               = SlideId (collection, "GPP1")
                                   OriginalFamily   = ""
                                   OriginalGenus    = ""
                                   OriginalSpecies  = ""
                                   Taxon            = id }]

    // [<Fact>]
    // let ``Focus images can be uploaded after calibration`` () =
    //     Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser}
    //             SlideRecorded {Id = SlideId (collection,"GPP1"); Taxon = id } ]
    //     |> When ( UploadSlideImage {Id = SlideId (collection, "GPP1"); Image = focusImage })
    //     |> Expect [ SlideImageUploaded ((SlideId (collection, "GPP1")), focusImage)  ]

    // [<Fact>]
    // let ``Single images do not require a calibration set`` () =
    //     Given [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser}
    //             SlideRecorded {Id = SlideId (collection,"GPP1"); Taxon = id } ]
    //     |> When ( UploadSlideImage {Id = SlideId (collection, "GPP1"); Image = singleImage })
    //     |> Expect [ SlideImageUploaded ((SlideId (collection, "GPP1")), singleImage)  ]
