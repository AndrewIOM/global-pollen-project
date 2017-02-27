module ReferenceCollectionTests

open System
open Xunit
open GlobalPollenProject.Core.Types    
open GlobalPollenProject.Core.Aggregates.ReferenceCollection

let a = {
    initial = State.Initial
    evolve = State.Evolve
    handle = handle
    getId = getId 
}

let Given = Given a defaultDependencies

module ``When digitising reference material`` =

    let currentUser = UserId (Guid.NewGuid())
    let collection = CollectionId (Guid.NewGuid())
    
    [<Fact>]
    let ``An empty draft is initially created`` () =
        Given []
        |> When ( CreateCollection {Id = collection; Name = "Test Collection"; Owner = currentUser})
        |> Expect [ DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser}]

    // [<Fact>]
    // let ``Images of digitised material can be uploaded with metadata`` =
    //     Given [DigitisationStarted {Id = collection; Name = "Test Collection"; Owner = currentUser}]
    //     |> When ( AddSlide {Id = collection; Images = someimages; Taxon = sometaxon})

    // [<Fact>]
    // let ``A slide can only be added if the taxon is valid in the backbone`` =

    //     2.