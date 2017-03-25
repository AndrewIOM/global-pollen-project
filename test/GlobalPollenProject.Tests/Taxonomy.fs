module TaxonomyTests

open System
open Xunit
open GlobalPollenProject.Core.Types    
open GlobalPollenProject.Core.Aggregates.Taxonomy

let a = {
    initial = State.InitialState
    evolve = State.Evolve
    handle = handle
    getId = getId 
}
let Given = Given a defaultDependencies

module ``When importing a taxon`` =

    let taxonId = TaxonId (defaultDependencies.GenerateId())
    let parentId = TaxonId (defaultDependencies.GenerateId())

    [<Fact>]
    let ``A family cannot have a parent`` () =
        Given []
        |> When (ImportFromBackbone {Id = taxonId
                                     Group = Angiosperm
                                     Identity = Family (LatinName "Apiaceae")
                                     Status = Accepted
                                     Parent = Some parentId
                                     Reference = None })
        |> ExpectInvalidArg

    [<Fact>]
    let ``The parent taxon must exist`` () =
        let dep = { defaultDependencies with ValidateTaxon = fun unit -> None }
        Specification.Given a dep []
        |> When (ImportFromBackbone {Id = taxonId
                                     Group = Angiosperm
                                     Identity = Genus (LatinName "Betula")
                                     Status = Accepted
                                     Parent = Some parentId
                                     Reference = None })
        |> ExpectInvalidArg

    [<Fact>]
    let ``The parent taxon must be of the correct rank`` () =
        Given []
        |> When (ImportFromBackbone {Id = taxonId
                                     Group = Angiosperm
                                     Identity = Family (LatinName "Apiaceae")
                                     Status = Accepted
                                     Parent = Some parentId
                                     Reference = None })
        |> ExpectInvalidArg

    [<Fact>]
    let ``A valid taxon is imported`` () =
        Given []
        |> When (ImportFromBackbone {Id = taxonId
                                     Group = Angiosperm
                                     Identity = Species ((LatinName "Betua"),(SpecificEphitet "nana"),(Scientific "L."))
                                     Status = Accepted
                                     Parent = Some parentId
                                     Reference = None })
        |> Expect [Imported { Id = taxonId
                              Group = Angiosperm
                              Identity = Species ((LatinName "Betua"),(SpecificEphitet "nana"),(Scientific "L."))
                              Status = Accepted
                              Parent = Some parentId
                              Reference = None }]

module ``When connecting a taxon to other databases`` =

    [<Fact>]
    let ``hello`` = 2.