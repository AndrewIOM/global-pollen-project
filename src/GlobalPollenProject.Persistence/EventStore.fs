module EventStore

open System
open Microsoft.EntityFrameworkCore
open Serialisation

exception WrongExpectedVersionException

let filter<'TEvent> ev = 
    match box ev with
    | :? 'TEvent as tev -> Some tev
    | _ -> None

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

let serialise (event:'a) : SerialisedEvent = 
    let typeName, data = serializeUnion event
    {
        Id = (Guid.NewGuid())
        Type = typeName
        Payload = data 
    }

let deserialise (event:EventInfo) : (obj * int) option = 
    match deserializeUnion event.EventType event.EventPayload with
    | Some x -> Some (x, event.StreamVersion)
    | None -> None

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
        printfn "Connected to Entity Framework EventStore"

type SqlEventStore() = 

    let mutable context = new SqlEventStoreContext()

    let saveEvent = new Event<string * obj>()

    member this.SaveEvent = 
        saveEvent.Publish 

    member this.ReadStream<'TEvent> streamId version count = 

        let result = query {
            for e in context.Events do
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
            
            events 
            |> List.choose deserialise
            |> List.choose filter<'TEvent>,
            lastEventNumber,
            if lastEventNumber < version + count 
                then None 
                else Some (lastEventNumber+1)

    member this.Save stream expectedVersion events = 

        let eventsWithVersion =
            events
            |> List.mapi (fun index event -> (event, expectedVersion + index + 1))

        let convertToEfType (event: obj * int) =
            let serialisedEvent = serialise (fst event)
            {
                Id = serialisedEvent.Id
                StreamId = stream
                StreamVersion = snd event
                OccurredAt = DateTime.Now
                EventType = serialisedEvent.Type
                EventPayload = serialisedEvent.Payload
            }

        match (this.ReadStream stream expectedVersion 1) with
        | (_,v,_) when v = expectedVersion -> 
            context.Events.AddRange (eventsWithVersion |> List.map convertToEfType)
            context.SaveChanges() |> ignore
        
        | (_,v,_) when expectedVersion = -1 ->
            context.Events.AddRange (eventsWithVersion |> List.map convertToEfType)
            context.SaveChanges() |> ignore

        | _ -> raise WrongExpectedVersionException

        events |> List.iter (fun e -> saveEvent.Trigger(stream,e))
