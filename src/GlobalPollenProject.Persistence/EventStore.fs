module EventStore

open System
open GlobalPollenProject.Core.Aggregates.Grain
open Microsoft.EntityFrameworkCore
open Serialisation

exception WrongExpectedVersionException

module SqlEventStore = 

    // A POCO representing event data in SQL row
    [<CLIMutable>]
    type EventInfo = {
        Id: Guid
        StreamId: string
        StreamVersion: int
        OccurredAt: DateTime
        EventType: string
        EventPayload: byte[] //Event serialised as json
    }

    type SerialisedEvent = {
        Id: Guid
        Type: string
        Payload: byte[]
    }

    type SqlEventStoreContext =
        inherit DbContext
        
        new() = { inherit DbContext() }
        new(options: DbContextOptions<SqlEventStoreContext>) = { inherit DbContext(options) }

        [<DefaultValue>]
        val mutable events:DbSet<EventInfo>
        member x.Events
            with get() = x.events
            and set v = x.events <- v

        override this.OnConfiguring optionsBuilder = 
            optionsBuilder.UseSqlite "Filename=./eventstore.db" |> ignore
            printfn "Opening Entity Framework EventStore"

    type SqlEventStore = 
        { mutable context : SqlEventStoreContext
          subscribers : (Event -> unit) list }

    let serialise (event:Event) : SerialisedEvent = 
        let typeName, data = serializeUnion event
        {
            Id = (Guid.NewGuid())
            Type = typeName
            Payload = data 
        }
    
    let deserialise (event:EventInfo) : (Event * int) option = 
        match deserializeUnion event.EventType event.EventPayload with
        | Some x -> Some (x, event.StreamVersion)
        | None -> None

    let create() = { context = new SqlEventStoreContext()
                     subscribers = [] }

    let subscribe subscriber store =
        { store with subscribers = subscriber :: store.subscribers } 

    let readStream store streamId version count =

        let result = query {
            for e in store.context.Events do
            where (e.StreamId = streamId)
            select e
        }

        match result |> Seq.isEmpty with
        | true -> 
            [], -1, None
        | false -> 
            let events =
                result
                |> Seq.sortBy (fun x -> x.StreamVersion)
                |> Seq.skipWhile (fun x -> x.StreamVersion < version )
                |> Seq.takeWhile (fun x -> x.StreamVersion <= version + count)
                |> Seq.toList 
            let lastEventNumber = (events |> Seq.last).StreamVersion 
            
            events |>
            List.choose deserialise,
            lastEventNumber,
            if lastEventNumber < version + count 
                then None 
                else Some (lastEventNumber+1)

    let appendToStream store stream expectedVersion (events:Event list) =

        // async {
        //     let serializedEvents = [| for event in newEvents -> serialize event |]

        //     do! Async.Ignore <| store.AsyncAppendToStream streamId expectedVersion serializedEvents }

        let eventsWithVersion =
            events
            |> List.mapi (fun index event -> (event, expectedVersion + index + 1))

        let convertToEfType (event: Event * int) =
            let serialisedEvent = serialise (fst event)
            {
                Id = serialisedEvent.Id
                StreamId = stream
                StreamVersion = snd event
                OccurredAt = DateTime.Now
                EventType = serialisedEvent.Type
                EventPayload = serialisedEvent.Payload
            }

        match (readStream store stream expectedVersion 1) with
        | (_,v,_) when v = expectedVersion -> 
            store.context.Events.AddRange (eventsWithVersion |> List.map convertToEfType)
            store.context.SaveChanges() |> ignore
        
        | (_,v,_) when expectedVersion = -1 ->
            store.context.Events.AddRange (eventsWithVersion |> List.map convertToEfType)
            store.context.SaveChanges() |> ignore

        | _ -> raise WrongExpectedVersionException

        // Side Effects:
        // For each new event, trigger every subscriber
        store.subscribers
        |> List.iter (fun s -> events |> List.iter s)


// module InMemoryEventStore =
//     type Stream = { mutable Events:  (Event * int) list }
//         with
//         static member Version stream = 
//             stream.Events
//             |> Seq.last
//             |> snd
    

//     type InMemoryEventStore = 
//         { mutable streams : Map<string,Stream> 
//           projection : Event -> unit }

//         interface IDisposable
//             with member x.Dispose() = ()                 

//     let create() = { streams = Map.empty
//                      projection = fun _ -> () }
//     let subscribe projection store =
//         { store with projection = projection} 

//     let readStream store streamId version count =
//         match store.streams.TryFind streamId with
//         | Some(stream) -> 
//             let events =
//                 stream.Events
//                 |> Seq.skipWhile (fun (_,v) -> v < version )
//                 |> Seq.takeWhile (fun (_,v) -> v <= version + count)
//                 |> Seq.toList 
//             let lastEventNumber = events |> Seq.last |> snd 
            
//             events |> List.map fst,
//                 lastEventNumber ,
//                 if lastEventNumber < version + count 
//                 then None 
//                 else Some (lastEventNumber+1)
            
//         | None -> [], -1, None

//     let appendToStream store streamId expectedVersion newEvents =
//         let eventsWithVersion =
//             newEvents
//             |> List.mapi (fun index event -> (event, expectedVersion + index + 1))

//         match store.streams.TryFind streamId with
//         | Some stream when Stream.Version stream = expectedVersion -> 
//             stream.Events <- stream.Events @ eventsWithVersion
        
//         | None when expectedVersion = -1 -> 
//             store.streams <- store.streams.Add(streamId, { Events = eventsWithVersion })        

//         | _ -> raise WrongExpectedVersionException
        
//         newEvents
//         |> Seq.iter store.projection
