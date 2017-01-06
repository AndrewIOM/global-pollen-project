module GlobalPollenProject.Core.Aggregates.Grain

open System
open GlobalPollenProject.Core.Types

// Commands
type Command =
    | SubmitUnknownGrain of SubmitUnknownGrain
    | IdentifyUnknownGrain of IdentifyUnknownGrain

and SubmitUnknownGrain = {
    Id: GrainId
    Images: Image list
}

and IdentifyUnknownGrain = {
    Id: GrainId
    Taxon: TaxonId
}


// Events
type Event =
    | GrainSubmitted of GrainSubmitted
    | GrainIdentified of GrainIdentified
    | GrainIdentityConfirmed of GrainIdentityConfirmed

and GrainSubmitted = {
    Id: GrainId
    Images: Image list
}

and GrainIdentified = {
    Id: GrainId
    Taxon: TaxonId
}

and GrainIdentityConfirmed = {
    Id: GrainId
    Taxon: TaxonId
}

and GrainIdentityChanged = {
    Id: GrainId
    Taxon: TaxonId
}

and GrainIdentityUnconfirmed = {
    Id: GrainId
}

// State tracking
type State =
    | InitialState
    | Submitted of GrainState

and GrainState = {
    Images: Image list
    IdentificationStatus: IdentificationStatus
}
and IdentificationState = {
    By: UserId
    Taxon: TaxonId
}
and IdentificationStatus =
    | Unidentified
    | Partial of IdentificationState list
    | Confirmed of IdentificationState list * TaxonId


// Decisions
let calculateIdentity (identifications: TaxonId list) =
    if identifications |> List.length < 3 then None
    else 
        let threshold = float (List.length identifications) * 0.70
        let taxon = 
            identifications
            |> List.groupBy id
            |> List.map (fun x -> fst x, (snd x).Length)
            |> List.filter (fun x -> float (snd x) > threshold)
            |> List.minBy snd
        Some (fst taxon)

let submitGrain (command: SubmitUnknownGrain) state =
    printfn "%i %s" command.Images.Length (command.Id.ToString())
    if command.Images.Length = 0 then invalidArg "images" "At least one image is required"
    match state with
    | InitialState ->
        printfn "G - Grain Submitted"
        [ GrainSubmitted { Id = command.Id
                           Images = command.Images }]
    | _ -> 
        printfn "Invalid op %s" (state.ToString())
        invalidOp "This grain has already been submitted"

let identifyGrain (command: IdentifyUnknownGrain) (state:State) =
    match state with
    | InitialState -> invalidOp "This grain does not exist"
    | _ -> 
        printfn "G - Grain Identified"
        let result = [ GrainIdentified { Id = command.Id; Taxon = command.Taxon }]
        // If unidentified, and taxon is Some, then send Confirmed event
        // If confirmed, and taxon is the same, then don't send another event
        // If confirmed, and taxon is different, then send Confirmed/Changed event
        // If confirmed, and taxon is None, then send UnConfirmed event
        result

// Handle Commands to make Decisions.
// NB We can use 'Domain services' in this function, 
// as their decision will be saved in the resulting event
let handle = 
    function
    | SubmitUnknownGrain command -> submitGrain command
    | IdentifyUnknownGrain command -> identifyGrain command

let aggregateId = function
    | SubmitUnknownGrain c -> c.Id
    | IdentifyUnknownGrain c -> c.Id

// Apply decisions already taken (rebuild)
type State with
    static member Evolve state = function

        | GrainSubmitted event -> 
            Submitted { 
              IdentificationStatus = Unidentified
              Images = event.Images }

        | GrainIdentified event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                Submitted {
                    grainState with
                        IdentificationStatus = Partial [] }

        | GrainIdentityConfirmed event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                Submitted {
                    grainState with
                        IdentificationStatus = Confirmed ([], event.Taxon) }
