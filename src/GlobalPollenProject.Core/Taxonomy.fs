module GlobalPollenProject.Core.Aggregates.Taxonomy

open GlobalPollenProject.Core.Types
open System

type Command =
    | Import of Import
    // | Split of Split
    // | Clump of Clump
    // | DesignateAsSynonym of DesignateAsSynonym
    // | ConnectToNeotoma of ConnectToService
    // | ConnectToGbif of ConnectToService

and Import = {
    Id: TaxonId
    Name: LatinName
    Rank: Rank
    Parent: TaxonId option
}

// and Split = {
//     Id: TaxonId
// }

// and Clump = {
//     Id: TaxonId
// }

// and DesignateAsSynonym = {
//     Id: TaxonId
//     SynonymOf: string
// }

// and ConnectToService = {
//     Id: TaxonId
// }

type Event =
    | Created of Created
    | GainedChild of GainedChild
    // | ConnectedToExternalService of ConnectedToExternalService

and Created = {
    Id: TaxonId
    Name: LatinName
    Parent: TaxonId option
    Rank: Rank
}

and GainedChild = {
    Id: TaxonId
    Child: TaxonId
}

// and ConnectedToExternalService = {
//     Id: TaxonId
//     ServiceName: string
//     ServiceId: string
// }

// State Tracking
type State =
    | InitialState
    | ValidatedByBackbone of TaxonState

and TaxonState = {
    Name: LatinName
    Parent: TaxonId option
    Children: TaxonId list
    Rank: Rank
    Links: ExternalLink list
}

and ExternalLink = {
    ServiceName: string
    ServiceId: string
}


// Descisions
let import (command:Import) state =

    match command.Parent with
    | Some x when command.Rank = Family -> invalidArg "parent" "a family cannot have a parent taxon"
    | None when command.Rank <> Family -> invalidArg "parent" "you must specify a parent for taxa of this rank"
    | _ -> printfn "Taxon validation successful"

    [Created { Id = command.Id
               Name = command.Name
               Rank = command.Rank
               Parent = command.Parent }]

// let split command state = 
//     [Created { Id = TaxonId 4
//                Name = LatinName "Freddius"
//                Rank = Rank.Family
//                Parent = None }]

// let clump command state =
//     [Created { Id = TaxonId 4
//                Name = LatinName "Freddius"
//                Rank = Rank.Family
//                Parent = None }]

// let synonym command state = 
//     [Created { Id = TaxonId (Guid.NewGuid())
//                Name = LatinName "Freddius"
//                Rank = Rank.Family
//                Parent = None }]

// let connect command state =
//     [Created { Id = TaxonId (Guid.NewGuid())
//                Name = LatinName "Freddius"
//                Rank = Rank.Family
//                Parent = None }]

// Handle Commands to make Decisions.
// NB We can use 'Domain services' in this function, 
// as their decision will be saved in the resulting event
let handle = 
    function
    | Import command -> import command
    // | Split command -> split command
    // | Clump command -> clump command
    // | DesignateAsSynonym command -> synonym command
    // | ConnectToNeotoma command -> connect command
    // | ConnectToGbif command -> connect command

// Apply decisions already taken (rebuild)
type State with
    static member Evolve state = function

        | Created event ->
            ValidatedByBackbone {
                Name = event.Name
                Parent = event.Parent
                Children = []
                Rank = event.Rank
                Links = []
            }

        | _ -> 
            printfn "Not handled"
            state

let private unwrap (TaxonId e) = e
let getId = function
    | Import c -> unwrap c.Id
    // | DesignateAsSynonym c -> unwrap c.Id
    // | ConnectToNeotoma c -> unwrap c.Id
    // | ConnectToGbif c -> unwrap c.Id
