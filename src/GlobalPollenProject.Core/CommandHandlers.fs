module GlobalPollenProject.Core.CommandHandlers

open GlobalPollenProject.Core.Types

let create aggregate aggregateName readStream (appendToStream) =

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
        load id
        |> mapsnd (aggregate.handle command)
        ||> save id
