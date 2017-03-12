module GlobalPollenProject.Core.Aggregates.Grain

open System
open GlobalPollenProject.Core.Types

// Commands
type Command =
    | SubmitUnknownGrain of SubmitUnknownGrain
    | IdentifyUnknownGrain of IdentifyUnknownGrain

and SubmitUnknownGrain = {
    Id: GrainId
    SubmittedBy: UserId
    Images: Image list
    Temporal: Age option
    Spatial: Site
}

and IdentifyUnknownGrain = {
    Id: GrainId
    IdentifiedBy: UserId
    Taxon: TaxonId
}


// Events
type Event =
    | GrainSubmitted of GrainSubmitted
    | GrainIdentified of GrainIdentified
    | GrainIdentityConfirmed of GrainIdentityConfirmed
    | GrainIdentityChanged of GrainIdentityChanged
    | GrainIdentityUnconfirmed of GrainIdentityUnconfirmed

and GrainSubmitted = {
    Id: GrainId
    Images: Image list
    Owner: UserId
}

and GrainIdentified = {
    Id: GrainId
    Taxon: TaxonId
    IdentifiedBy: UserId
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
    Owner: UserId
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
    printfn "Evaluating grain identity. It has %i IDs" (identifications |> List.length)
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
    if command.Images.Length = 0 then invalidArg "images" "At least one image is required"
    match state with
    | InitialState ->
        [ GrainSubmitted { Id = command.Id
                           Owner = command.SubmittedBy
                           Images = command.Images }]
    | _ -> 
        invalidOp "This grain has already been submitted"

let identifyGrain (command: IdentifyUnknownGrain) (state:State) =
    match state with
    | InitialState -> invalidOp "This grain does not exist"
    | Submitted s -> 

        let evaluate oldIds newId taxon =
            printfn "Old IDs: %i." (oldIds |> List.length)
            let result = GrainIdentified { Id = command.Id; Taxon = command.Taxon; IdentifiedBy = command.IdentifiedBy }
            if oldIds |> List.exists (fun x -> x.By = command.IdentifiedBy) then invalidOp "Cannot submit a second ID"
            let ids = newId :: oldIds
            let confirmedId = calculateIdentity (ids |> List.map (fun i -> i.Taxon))
            match confirmedId with
            | Some identity ->
                let confirmed = GrainIdentityConfirmed { Id = command.Id; Taxon = identity }
                match taxon with
                | Some existing ->
                    if existing = identity then [result]
                    else [result; GrainIdentityChanged { Id = command.Id; Taxon = identity } ]
                | None -> [result;confirmed]
            | None ->
                match taxon with
                | Some existing -> [result; GrainIdentityUnconfirmed { Id = command.Id } ]
                | None -> [result]

        let newId = { By = command.IdentifiedBy; Taxon = command.Taxon }
        match s.IdentificationStatus with
        | Unidentified -> evaluate [] newId None
        | Partial ids -> evaluate ids newId None
        | Confirmed (ids,t) -> evaluate ids newId (Some t)
        

// Handle Commands to make Decisions.
// NB We can use 'Domain services' in this function, 
// as their decision will be saved in the resulting event
let handle deps = 
    function
    | SubmitUnknownGrain command -> submitGrain command
    | IdentifyUnknownGrain command -> identifyGrain command

let private unwrap (GrainId e) = e
let getId = function
    | SubmitUnknownGrain c -> unwrap c.Id
    | IdentifyUnknownGrain c -> unwrap c.Id

// Apply decisions already taken (rebuild)
type State with
    static member Evolve state = function

        | GrainSubmitted event -> 
            Submitted { 
              IdentificationStatus = Unidentified
              Owner = event.Owner
              Images = event.Images }

        | GrainIdentified event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                let newId = {By = event.IdentifiedBy; Taxon = event.Taxon}
                match grainState.IdentificationStatus with
                | Unidentified ->
                    printfn "Grain is now partially identified"
                    Submitted {
                        grainState with
                            IdentificationStatus = Partial ([newId]) }
                | Partial ids ->
                    printfn "Partially ID'd grain gained new id. Current IDs: %i" ids.Length
                    Submitted {
                        grainState with
                            IdentificationStatus = Partial (newId :: ids) }
                | Confirmed (ids,t) ->
                    Submitted {
                        grainState with
                            IdentificationStatus = Confirmed (newId :: ids,t) }

        | GrainIdentityConfirmed event ->
            match state with
            | InitialState -> invalidOp "Grain is not submitted"
            | Submitted grainState ->
                match grainState.IdentificationStatus with
                | Partial ids ->
                    Submitted {
                        grainState with
                            IdentificationStatus = Confirmed (ids, event.Taxon) }
                | _ -> invalidOp "Cannot transition from unsubmitted or confirmed to confirmed"

        | GrainIdentityChanged event ->
            state

        | GrainIdentityUnconfirmed event ->
            state