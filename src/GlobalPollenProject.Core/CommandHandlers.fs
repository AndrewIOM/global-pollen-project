module GlobalPollenProject.Core.CommandHandlers

open GlobalPollenProject.Core.Aggregates.Grain

// NB Should have a seperate command handler per aggregate root

// Handle commands relating to grain root aggregate
module Grain =

    let grainId =
        function
        | SubmitUnknownGrain { Id = GrainId id } -> id
        | IdentifyUnknownGrain { Id = GrainId id }  -> id

    let create readStream appendToStream =

        let streamId grainId = sprintf "Grain-%s" (grainId.ToString())
        let load grainId =
            let rec fold state version =
                let events, lastEvent, nextEvent = readStream (streamId grainId) version 500
                let state = List.fold State.Evolve state (List.map fst events)
                match nextEvent with
                | None -> lastEvent, state
                | Some n -> fold state n
            fold State.InitialState 0

        let save gameId expectedVersion events = appendToStream (streamId gameId) expectedVersion events

        let inline mapsnd f (v,s) = v, f s
        fun command ->
            let id = grainId command

            load id
            |> mapsnd (handle command)
            ||> save id