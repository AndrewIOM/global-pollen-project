module GrainTests

open System
open Xunit
open GlobalPollenProject.Core.Types    
open GlobalPollenProject.Core.Aggregates.Grain

let a = {
    initial = State.InitialState
    evolve = State.Evolve
    handle = handle
    getId = getId 
}

let Given = Given a defaultDependencies

let grainId = GrainId (Guid.NewGuid())
let currentUser = UserId (Guid.NewGuid())
let testImage = SingleImage (Url "https://sometest.com/someimage.png")
let latlon = Latitude 1.0<DD>, Longitude 1.0<DD>
let time = CollectionDate 1995<CalYr>


module ``When submitting an unknown grain`` =

    [<Fact>]
    let ``It should be flagged as unidentified`` () =
        Given []
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = [testImage]; Temporal = Some time; Spatial = latlon } )
        |> Expect [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser } ]

    [<Fact>]
    let ``The same grain cannot be submitted twice`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser } ]
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = [testImage]; Temporal = Some time; Spatial = latlon } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``At least one image must be submitted`` () =
        Given []
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = []; Temporal = Some time; Spatial = latlon } )
        |> ExpectInvalidArg


module ``When identifying an unknown grain`` =

    let identifier = UserId (Guid.NewGuid())
    let taxon = TaxonId (Guid.NewGuid())

    [<Fact>]
    let ``The grain gains an identification`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = identifier; Taxon = taxon })
        |> Expect [ GrainIdentified { Id = grainId; IdentifiedBy = identifier; Taxon = taxon } ]

    [<Fact>]
    let ``A user cannot submit more than one identification at any time`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier }
                GrainIdentified { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``The taxonomic identity is confirmed after 70% agreement with three or more IDs`` () =
        Given [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier }
                GrainIdentified { Id = grainId; IdentifiedBy = UserId (Guid.NewGuid()); Taxon = taxon }
                GrainIdentified { Id = grainId; IdentifiedBy = UserId (Guid.NewGuid()); Taxon = taxon } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } )
        |> Expect [ GrainIdentified { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon }
                    GrainIdentityConfirmed { Id = grainId; Taxon = taxon } ]
