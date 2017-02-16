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

let grainId = GrainId (Guid.NewGuid())
let currentUser = UserId (Guid.NewGuid())
let testImage = SingleImage (Url "https://sometest.com/someimage.png")


module ``When submitting an unknown grain`` =

    [<Fact>]
    let ``It should be flagged as unidentified`` () =
        Given a []
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = [testImage] } )
        |> Expect [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser } ]

    [<Fact>]
    let ``The same grain cannot be submitted twice`` () =
        Given a [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = currentUser } ]
        |> When ( SubmitUnknownGrain { Id = grainId; SubmittedBy = currentUser; Images = [testImage] } )
        |> ExpectInvalidOp
        

module ``When identifying an unknown grain`` =

    let identifier = UserId (Guid.NewGuid())
    let taxon = TaxonId (Guid.NewGuid())

    [<Fact>]
    let ``The grain gains an identification`` () =
        Given a [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = identifier; Taxon = taxon })
        |> Expect [ GrainIdentified { Id = grainId; IdentifiedBy = identifier; Taxon = taxon } ]

    [<Fact>]
    let ``A user cannot submit more than one identification at any time`` () =
        Given a [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier }
                  GrainIdentified { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } )
        |> ExpectInvalidOp

    [<Fact>]
    let ``The taxonomic identity is confirmed after 70% agreement with three or more IDs`` () =
        Given a [ GrainSubmitted { Id = grainId; Images = [testImage]; Owner = identifier }
                  GrainIdentified { Id = grainId; IdentifiedBy = UserId (Guid.NewGuid()); Taxon = taxon }
                  GrainIdentified { Id = grainId; IdentifiedBy = UserId (Guid.NewGuid()); Taxon = taxon } ]
        |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon } )
        |> Expect [ GrainIdentified { Id = grainId; IdentifiedBy = currentUser; Taxon = taxon }
                    GrainIdentityConfirmed { Id = grainId; Taxon = taxon } ]
