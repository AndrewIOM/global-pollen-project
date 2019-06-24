module EventStore

open System
open System.Net
open EventStore.ClientAPI
open Serialisation
open GlobalPollenProject.Core.Aggregate

type IEventStoreConnection with
    member this.AsyncConnect() = this.ConnectAsync() |> Async.AwaitTask
    member this.AsyncReadStreamEventsForward stream start count resolveLinkTos =
        this.ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos)
        |> Async.AwaitTask
    member this.AsyncAppendToStream stream expectedVersion events =
        this.AppendToStreamAsync(stream, expectedVersion, events)
        |> Async.AwaitTask

let serialise (event:'a) = 
    let typeName, data = Serialisation.serialiseEventToBytes event
    EventData(Guid.NewGuid(), typeName, true, data, null)
let deserialise<'TEvent> (event:ResolvedEvent) : 'TEvent option = Serialisation.deserialiseEventFromBytes event.Event.EventType event.Event.Data

let connect host port username userpass = 
    async {
        let ipAddress = 
            System.Net.Dns.GetHostAddressesAsync(host) 
            |> Async.AwaitTask 
            |> Async.RunSynchronously
            |> Array.map (fun x -> x.MapToIPv4()) |> Array.head
        let endpoint = IPEndPoint(ipAddress, port)
        let esSettings = ConnectionSettings.Create()
                            .UseConsoleLogger()
                            .SetDefaultUserCredentials(SystemData.UserCredentials(username, userpass))
                            .Build()
        let s = EventStoreConnection.Create(esSettings, endpoint, "GPP Web")
        do! Async.AwaitTask ( s.ConnectAsync() )
        return s }

let rec private readAll (connection : IEventStoreConnection)
                        (from : Position) : seq<RecordedEvent> =
    seq {
        let sliceTask = connection.ReadAllEventsForwardAsync(from, 100, true)
        let slice = sliceTask |> Async.AwaitTask |> Async.RunSynchronously
        if slice.Events.Length > 0 then
            for resolvedEvent in slice.Events do
                yield resolvedEvent.OriginalEvent
            yield! readAll connection slice.NextPosition
    }

type EventStore(store:IEventStoreConnection) =

    let saveEvent = new Event<string * obj * DateTime>()

    member this.SaveEvent = saveEvent.Publish 

    member this.Subscribe stream (projection: obj -> unit) =
        async {
        do! Async.AwaitTask <| store.SubscribeToStreamAsync(stream, true, (fun s e -> deserialise<GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event> e |> Option.iter projection), (fun s r e -> printfn "Subscription disconnected")) |> Async.Ignore
        return store }
        |> Async.RunSynchronously

    member this.ReadStream<'a> streamId version count = 
        async {
            let! slice = store.AsyncReadStreamEventsForward streamId (int64 version) count true

            let events = 
                slice.Events 
                |> Seq.choose deserialise<'a>
                |> Seq.toList
            
            let nextEventNumber = 
                if slice.IsEndOfStream 
                then None 
                else Some <| int slice.NextEventNumber

            return events, int slice.LastEventNumber, nextEventNumber }

    member this.Checkpoint () =
        let e = 
            store.ReadAllEventsBackwardAsync(Position.End, 1, false)
            |> Async.AwaitTask
            |> Async.RunSynchronously
        printfn "Checkpoint: %s" (e.FromPosition.ToString())
        e.FromPosition.ToString() |> int

    member this.AppendToStream getTime streamId expectedVersion newEvents = 
        async {
            let serializedEvents = [| for event in newEvents -> serialise event |]
            do! Async.Ignore <| store.AsyncAppendToStream streamId (int64 expectedVersion) serializedEvents
            newEvents |> List.iter (fun e -> saveEvent.Trigger(streamId ,upcast e, getTime())) }

    member this.ReplayDomainEvents() =
        let events = readAll store Position.Start
        events |> Seq.iter (fun e -> saveEvent.Trigger(e.EventStreamId, Serialisation.deserialiseEventByName e.EventType e.Data, e.Created))

    member this.MakeCommandHandler aggName aggregate deps =
        let read = this.ReadStream<'TEvent>
        let append = this.AppendToStream deps.GetTime
        Aggregate.makeHandler aggregate aggName deps read append

    member this.MakeReadModelGetter (deserialize:byte array -> _) =
        fun streamId -> async {
            let! eventsSlice = store.ReadStreamEventsBackwardAsync(streamId, Int64.Parse("-1"), 1, false) |> Async.AwaitTask
            if eventsSlice.Status <> SliceReadStatus.Success then return None
            elif eventsSlice.Events.Length = 0 then return None
            else 
                let lastEvent = eventsSlice.Events.[0]
                if lastEvent.Event.EventNumber = Int64.Parse("0") then return None
                else return Some(deserialize(lastEvent.Event.Data))    
        }