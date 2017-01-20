module GlobalPollenProject.Core.CommandHandlers

open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.Aggregates.Grain

// 'b is currently version
let grainHandler readStream appendToStream =

    let streamId grainId = sprintf "Grain-%s" (grainId.ToString())
    let load grainId =
        let rec fold state version =
            let events, lastEvent, nextEvent = readStream (streamId grainId) version 500
            let state = List.fold Aggregates.Grain.State.Evolve state (List.map fst events)
            match nextEvent with
            | None -> lastEvent, state
            | Some n -> fold state n
        fold Aggregates.Grain.InitialState 0

    let save grainId expectedVersion events = appendToStream (streamId grainId) expectedVersion events

    let inline mapsnd f (v,s) = v, f s
    fun command ->
        let id = Aggregates.Grain.getId command

        load id
        |> mapsnd (Aggregates.Grain.handle command)
        ||> save id


let create aggregate aggregateName readStream appendToStream =

    let streamId grainId = sprintf "%s-%s" aggregateName (grainId.ToString())
    let load grainId =
        let rec fold state version =
            let events, lastEvent, nextEvent = readStream (streamId grainId) version 500
            let state = List.fold aggregate.evolve state (List.map fst events)
            match nextEvent with
            | None -> lastEvent, state
            | Some n -> fold state n
        fold aggregate.initial 0

    let save grainId expectedVersion events = appendToStream (streamId grainId) expectedVersion events

    let inline mapsnd f (v,s) = v, f s
    fun command ->
        let id = aggregate.getId command

        // let result = load id
        // let result2 = mapsnd (Aggregates.Grain.handle command) result // Version * event list
        // let result3 = fst result2,(snd result2) |> List.map (fun x -> x :> IDomainEvent) //map events to obj list
        // result3 ||> save id
        load id
        |> mapsnd (Aggregates.Grain.handle command)
        ||> save id
