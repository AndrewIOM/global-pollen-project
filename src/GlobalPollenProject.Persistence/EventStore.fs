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
    let typeName, data = Serialisation.serializeUnion event
    EventData(Guid.NewGuid(), typeName, true, data, null)
let deserialise<'TEvent> (event:ResolvedEvent) : 'TEvent option = Serialisation.deserializeUnion event.Event.EventType event.Event.Data

let connect ip port username userpass = 
    async {
        let ipAddress = IPAddress.Parse(ip)
        let endpoint = IPEndPoint(ipAddress, port)
        let esSettings = ConnectionSettings.Create()
                            .UseConsoleLogger()
                            .SetDefaultUserCredentials(SystemData.UserCredentials(username, userpass))
                            .Build()
        let s = EventStoreConnection.Create(esSettings, endpoint, "GPP Web")
        do! Async.AwaitTask ( s.ConnectAsync() )
        return s }

let subscribe (projection: obj -> unit) (getStore: Async<IEventStoreConnection>) =
    async {
    let! store = getStore
    do! Async.AwaitTask <| store.SubscribeToStreamAsync("$ce-ReferenceCollection", true, (fun s e -> deserialise<GlobalPollenProject.Core.Aggregates.ReferenceCollection.Event> e |> Option.iter projection), (fun s r e -> printfn "Subscription disconnected")) |> Async.Ignore
    return store }
    |> Async.RunSynchronously

let readStream<'a> (store: IEventStoreConnection) streamId version count = 
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

let appendToStream (store: IEventStoreConnection) streamId expectedVersion newEvents = 
    async {
        let serializedEvents = [| for event in newEvents -> serialise event |]
        do! Async.Ignore <| store.AsyncAppendToStream streamId (int64 expectedVersion) serializedEvents
        newEvents |> List.iter (fun e -> savedEvent.Trigger(streamId,upcast e)) }

let makeCommandHandler (conn:IEventStoreConnection) aggName aggregate deps =
    let read = readStream<'TEvent> conn
    let append = appendToStream conn 
    
    Aggregate.makeHandler aggregate aggName deps read append

let makeReadModelGetter (conn:IEventStoreConnection) (deserialize:byte array -> _) =
    fun streamId -> async {
        let! eventsSlice = conn.ReadStreamEventsBackwardAsync(streamId, Int64.Parse("-1"), 1, false) |> Async.AwaitTask
        if eventsSlice.Status <> SliceReadStatus.Success then return None
        elif eventsSlice.Events.Length = 0 then return None
        else 
            let lastEvent = eventsSlice.Events.[0]
            if lastEvent.Event.EventNumber = Int64.Parse("0") then return None
            else return Some(deserialize(lastEvent.Event.Data))    
    }