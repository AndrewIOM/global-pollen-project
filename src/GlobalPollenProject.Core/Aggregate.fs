module GlobalPollenProject.Core.Aggregate

open System
open GlobalPollenProject.Core.DomainTypes

type DomainError =
| NotAllowed

type Dependencies =  {
    GenerateId:        unit -> Guid
    Log:               LogMessage -> unit
    ValidateTaxon:     BackboneQuery -> TaxonId option
    GetGbifId:         TaxonId -> Result<int option,string>
    GetNeotomaId:      TaxonId -> Result<int option,string>
    GetEolId:          TaxonId -> Result<int option,string>
    GetTime:           unit -> DateTime
    CalculateIdentity: TaxonIdentification list -> TaxonId option }

type RootAggregate<'TState, 'TCommand, 'TEvent> = {
    initial:    'TState
    evolve:     'TState -> 'TEvent -> 'TState
    handle:     Dependencies -> 'TCommand -> 'TState -> 'TEvent list
    getId:      'TCommand -> RootAggregateId }

type LoadEvents = System.Type * RootAggregateId -> Async<int * obj seq>
type PersistEvent = RootAggregateId * int -> obj -> Async<unit>

module Aggregate =

    type Agent<'T> = MailboxProcessor<'T>
    let makeHandler (aggregate:RootAggregate<'TState, 'TCommand, 'TEvent>) name deps readStream appendToStream =

        let streamId id = 
            sprintf "%s-%O" name id 

        let load id =
            let rec fold state version =
                async {
                let! events, lastEvent, nextEvent = 
                    readStream (streamId id) version 500

                let state = List.fold aggregate.evolve state events
                match nextEvent with
                | None -> return lastEvent, state
                | Some n -> return! fold state n }
            fold aggregate.initial 0

        let save id expectedVersion events = 
            appendToStream (streamId id) expectedVersion events

        fun command ->
            let id = aggregate.getId command
            let version, state = load id |> Async.RunSynchronously
            let events = aggregate.handle deps command state
            save id version events |> Async.RunSynchronously

        // let start id =
        //     Agent.Start
        //     <| fun inbox ->
        //         let rec loop version state =
        //             async {
        //                 let! command = inbox.Receive()
        //                 let events = aggregate.handle deps command state
        //                 do! save id version events

        //                 let newState = List.fold aggregate.evolve state events
        //                 return! loop (version + List.length events) newState  }
        //         async {
        //             let! version, state = load id
        //             return! loop version state }
        // let forward (agent: Agent<_>) command = agent.Post command

        // let dispatcher =
        //     Agent.Start
        //     <| fun inbox ->
        //         let rec loop aggregates =
        //             async {
        //                 let! command = inbox.Receive()
        //                 let id = aggregate.getId command
        //                 match Map.tryFind id aggregates with
        //                 | Some aggregate -> 
        //                     forward aggregate command
        //                     return! loop aggregates
        //                 | None ->
        //                     let aggregate = start id
        //                     forward aggregate command
        //                     return! loop (Map.add id aggregate aggregates) }
        //         loop Map.empty

        // fun command -> 
        //     dispatcher.Post command

    // let makeHandler (aggregate:RootAggregate<'TState, 'TCommand, 'TEvent>) deps (load:LoadEvents, commit:PersistEvent) =
    //     fun command -> async {
    //         let id = aggregate.getId command
    //         let! loaded = load(typeof<'TEvent>,id)
    //         let expectedVersion = fst loaded
    //         let events = snd loaded |> Seq.cast :> 'TEvent seq
    //         let state = Seq.fold aggregate.evolve aggregate.initial (events)

    //         let save id expectedVersion events = commit (id,expectedVersion) events
    //         let inline mapsnd f (v,s) = v, f s

    //         do! state
    //             |> aggregate.handle deps command
    //             |> save id expectedVersion
    //     }


    ////////////////////

