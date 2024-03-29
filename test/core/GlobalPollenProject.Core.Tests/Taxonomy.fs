module TaxonomyTests

open Xunit
open GlobalPollenProject.Core.DomainTypes    
open GlobalPollenProject.Core.Aggregate
open GlobalPollenProject.Core.Aggregates.Taxonomy

let a = {
    initial = State.InitialState
    evolve = State.Evolve
    handle = handle
    getId = getId 
}
let Given = Given a domainDefaultDeps

module ``When importing a taxon`` =

    let taxonId = TaxonId (domainDefaultDeps.GenerateId())
    let parentId = TaxonId (domainDefaultDeps.GenerateId())

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
        let dep = { domainDefaultDeps with ValidateTaxon = fun unit -> None }
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
                                     Identity = Species ((LatinName "Betula"),(SpecificEphitet "nana"),(Scientific "L."))
                                     Status = Accepted
                                     Parent = Some parentId
                                     Reference = None })
        |> Expect [Imported { Id = taxonId
                              Group = Angiosperm
                              Identity = Species ((LatinName "Betula"),(SpecificEphitet "nana"),(Scientific "L."))
                              Status = Accepted
                              Parent = Some parentId
                              Reference = None }]


    // module ``When revising a taxon's status`` =
        
    //     [<Fact>]
    //     let ``It is moved between higher ranks`` () =
    //         invalidOp "Not implemented"
            