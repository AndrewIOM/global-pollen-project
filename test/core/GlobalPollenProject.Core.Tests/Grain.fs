module GrainTests

open System
open Xunit
open GlobalPollenProject.Core.DomainTypes    
open GlobalPollenProject.Core.Aggregate
open GlobalPollenProject.Core.Aggregates.Grain
open GlobalPollenProject.Core.Dependencies

let a = {
    initial = State.InitialState
    evolve = State.Evolve
    handle = handle
    getId = getId 
}

let Given = Given a { domainDefaultDeps with CalculateIdentity = calculateTaxonomicIdentity TaxonomicIdentityTests.testBackboneUpper
                                             GetImageDimension = fun i -> { Height = 800<pixels>; Width = 600<pixels> } |> Ok }

let grainId = GrainId (Guid.NewGuid())
let slideId = SlideId (CollectionId (Guid.NewGuid()), "GPP1")
let currentUser = UserId (Guid.NewGuid())
let testImage = SingleImage (RelativeUrl "https://sometest.com/someimage.png",{Point1 = 2,3; Point2 = 5,8; MeasuredDistance = 4.<um>})
let latlon = Latitude 1.0<DD>, Longitude 1.0<DD>
let time = CollectionDate 1995<CalYr>
let testTaxon = TaxonId TaxonomicIdentityTests.commonGenus
let identification = Botanical (testTaxon, Unknown, Person (["Test"], "McTest"))

module ``When submitting an unknown grain`` =

    [<Fact>]
    let ``It should be flagged as unidentified`` () =
        Given []
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = [testImage]; Temporal = Some time; Spatial = latlon } )
        |> Expect [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = Some time; Spatial = Site latlon } ]

    [<Fact>]
    let ``The same grain cannot be submitted twice`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = Some time; Spatial = Site latlon } ]
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = [testImage]; Temporal = Some time; Spatial = latlon } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``At least one image must be submitted`` () =
        Given []
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = []; Temporal = Some time; Spatial = latlon } )
        |> ExpectInvalidArg


module ``When identifying an unknown grain`` =

    let identifier = UserId (Guid.NewGuid())
    let taxon = TaxonId (TaxonomicIdentityTests.commonGenus)

    [<Fact>]
    let ``The grain gains an identification`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier; Temporal = Some time; Spatial = Site latlon } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = identifier; Taxon = taxon })
        |> Expect [ GrainIdentified { Id = grainId; IdentifiedBy = identifier; Taxon = taxon } ]

    [<Fact>]
    let ``A user cannot submit more than one identification at any time`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier; Temporal = Some time; Spatial = Site latlon }
                GrainIdentified { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``The taxonomic identity is confirmed after 70% agreement with three or more IDs`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier; Temporal = Some time; Spatial = Site latlon }
                GrainIdentified { Id = grainId; IdentifiedBy = UserId (Guid.NewGuid()); Taxon = taxon }
                GrainIdentified { Id = grainId; IdentifiedBy = UserId (Guid.NewGuid()); Taxon = taxon } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } )
        |> Expect [ GrainIdentified { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon }
                    GrainIdentityConfirmed { Id = grainId; Taxon = taxon } ]


module ``When a grain is derived from reference material`` =

    let confirmedIdStatus = Confirmed ([identification], testTaxon)

    [<Fact>]
    let ``the grain will be both derived and identified`` () =
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = None; Taxon = confirmedIdStatus })
        |> Expect [ GrainDerived { Id = grainId; Image = testImage; 
                                   ImageCroppedArea = None; Origin = slideId, ColVersion 1 }
                    GrainIdentifiedExternally { Id = grainId; Identification = identification } ]

    [<Fact>]
    let ``the grain must have a previously confirmed identity`` () =
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = None; Taxon = Unidentified })
        |> ExpectInvalidOp

    [<Fact>]
    let ``a crop is not required`` () =
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = None; Taxon = confirmedIdStatus })
        |> Expect [ GrainDerived { Id = grainId; Image = testImage; 
                                   ImageCroppedArea = None; Origin = slideId, ColVersion 1 }
                    GrainIdentifiedExternally { Id = grainId; Identification = identification } ]
        
    [<Fact>]
    let ``a crop cannot be outside the image bounds`` () =
        let badCrop = { TopLeft = { X = 1000<pixels>; Y = 2000<pixels>}; BottomRight = { X = 9000<pixels>; Y = 3000<pixels>}}
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = Some badCrop; Taxon = confirmedIdStatus })
        |> ExpectInvalidOp

    [<Fact>]
    let ``a crop must be fully within the image bounds`` () =
        let goodCrop = { TopLeft = { X = 100<pixels>; Y = 200<pixels>}; BottomRight = { X = 500<pixels>; Y = 700<pixels>}}
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = Some goodCrop; Taxon = confirmedIdStatus })
        |> Expect [ GrainDerived { Id = grainId; Image = testImage; 
                                   ImageCroppedArea = Some goodCrop; Origin = slideId, ColVersion 1 }
                    GrainIdentifiedExternally { Id = grainId; Identification = identification } ]

    [<Fact>]
    let ``a crop can fall on the image boundary`` () =
        let goodCrop = { TopLeft = { X = 0<pixels>; Y = 0<pixels>}; BottomRight = { X = 600<pixels>; Y = 800<pixels>}}
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = Some goodCrop; Taxon = confirmedIdStatus })
        |> Expect [ GrainDerived { Id = grainId; Image = testImage; 
                                   ImageCroppedArea = Some goodCrop; Origin = slideId, ColVersion 1 }
                    GrainIdentifiedExternally { Id = grainId; Identification = identification } ]

    [<Fact>]
    let ``a crop must have a width greater than zero`` () =
        let badCrop = { TopLeft = { X = 100<pixels>; Y = 100<pixels>}; BottomRight = { X = 100<pixels>; Y = 200<pixels>}}
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = Some badCrop; Taxon = confirmedIdStatus })
        |> ExpectInvalidOp

    [<Fact>]
    let ``a crop must have a height greater than zero`` () =
        let badCrop = { TopLeft = { X = 100<pixels>; Y = 100<pixels>}; BottomRight = { X = 200<pixels>; Y = 100<pixels>}}
        Given []
        |> When (DeriveGrainFromSlide { 
            Id = grainId; Origin = slideId, ColVersion 1; 
            Image = testImage; ImageCroppedArea = Some badCrop; Taxon = confirmedIdStatus })
        |> ExpectInvalidOp


module ``When tagging morphological traits`` =

    [<Fact>]
    let ``A user can only tag each trait once`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = Some time; Spatial = Site latlon }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = currentUser; Trait = Shape Circular } ]
        |> When(IdentifyTrait { Image = 1; Id = grainId; IdentifiedBy = currentUser; Trait = Shape Trilobate })
        |> ExpectInvalidOp

    [<Fact>]
    let ``Discrete traits require agreement of at least 70 percent to be accepted`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = Some time; Spatial = Site latlon }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Circular }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Circular }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Trilobate }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Ovular }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Circular }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Circular } ]
        |> When(IdentifyTrait { Image = 1; Id = grainId; IdentifiedBy = currentUser; Trait = Shape Circular })
        |> Expect [ 
                GrainTraitIdentified { Id = grainId; IdentifiedBy = currentUser; Trait = Shape Circular }
                GrainTraitConfirmed { Id = grainId; Trait = ConfirmedShape Circular} ]

    [<Fact>]
    let ``Discrete traits require at least three observations to be accepted`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = Some time; Spatial = Site latlon }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Circular }
                GrainTraitIdentified { Id = grainId; IdentifiedBy = UserId <| Guid.NewGuid(); Trait = Shape Circular } ]
        |> When(IdentifyTrait { Image = 1; Id = grainId; IdentifiedBy = currentUser; Trait = Shape Circular })
        |> Expect [ 
                GrainTraitIdentified { Id = grainId; IdentifiedBy = currentUser; Trait = Shape Circular }
                GrainTraitConfirmed { Id = grainId; Trait = ConfirmedShape Circular} ]

    // [<Fact>]
    // let ``Continuous traits can only be accepted if values have a unimodal distribution`` () =
    //     failwith "not finished"

    // [<Fact>]
    // let ``Continuous traits require at least 10 individual trait IDs before unimodality may be tested`` () =
    //     failwith "not finished"


module ``When questioning the validity of a grain`` =

    [<Fact>]
    let ``The grain becomes flagged as problematic for the reason given`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = Some time; Spatial = Site latlon } ]
        |> When(ReportProblem (grainId, IsMultipleSpecimen))
        |> Expect [ ProblemFlagged { Id = grainId; Flag = IsMultipleSpecimen } ]

    [<Fact>]
    let ``A specific problem is only flagged once`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = Some time; Spatial = Site latlon }
                ProblemFlagged { Id = grainId; Flag = IsMultipleSpecimen } ]
        |> When(ReportProblem (grainId, IsMultipleSpecimen))
        |> Expect []

