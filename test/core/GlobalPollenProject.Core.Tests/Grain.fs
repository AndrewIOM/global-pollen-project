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

let Given = Given a { domainDefaultDeps with CalculateIdentity = calculateTaxonomicIdentity TaxonomicIdentityTests.testBackboneUpper }

let grainId = GrainId (Guid.NewGuid())
let slideId = SlideId (CollectionId (Guid.NewGuid()), "GPP1")
let currentUser = UserId (Guid.NewGuid())
let testImage = SingleImage (RelativeUrl "https://sometest.com/someimage.png",{Point1 = 2,3; Point2 = 5,8; MeasuredDistance = 4.<um>})
let latlon = Latitude 1.0<DD>, Longitude 1.0<DD>
let time = CollectionDate 1995<CalYr>


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


// module ``When a grain is derived from reference material`` =


//     [<Fact>]
//     let ``the grain will be both derived and identified`` () =
//         // Given []
//         // |> When (DeriveGrainFromSlide { 
//         //     Id = grainId; Origin = slideId, ColVersion 1; 
//         //     Image = testImage; ImageCroppedArea = None; Spatial = None; Temporal = None })
//         // |> Expect [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser; Temporal = None; Spatial = None } ]
//         // TODO age and site characteristics?
//         failwith "not finished"

//     [<Fact>]
//     let ``the grain must have a previously confirmed identity`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``a crop is not required`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``a crop cannot be outside the image bounds`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``a crop must be fully within the image bounds`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``a crop can fall on the image boundary`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``a crop must have a width greater than zero`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``a crop must have a height greater than zero`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``the grain carries the identification of the original slide`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``it should have a confirmed identity already`` () =
//         failwith "not finished"


// module ``When tagging morphological traits`` =

//     [<Fact>]
//     let ``A user can only tag each trait once`` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``Traits represented by continuous variables `` () =
//         failwith "not finished"

//     [<Fact>]
//     let ``Traits represented by discrete variables require agreement`` () =
//         failwith "not finished"

// module ``When questioning the validity of a grain`` =

//     [<Fact>]
//     let ``The grain becomes flagged as problematic for the reason given`` () =
//         failwith "not finished"