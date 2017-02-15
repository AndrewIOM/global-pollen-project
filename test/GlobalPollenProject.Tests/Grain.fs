module GlobalPollenProject.Core.Tests.Grain

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

[<Fact>]
let ``When users are identifying an unknown grain``() =

    let grainId = GrainId (Guid.NewGuid())
    let identifier = UserId (Guid.NewGuid())
    let taxon = TaxonId (Guid.NewGuid())

    Given a [ GrainSubmitted { Id = grainId; Images = []; Owner = identifier } ]
    |> When ( IdentifyUnknownGrain { Id = grainId; IdentifiedBy = identifier; Taxon = taxon })
    |> Expect [ GrainIdentified { Id = grainId; IdentifiedBy = identifier; Taxon = taxon } ]
