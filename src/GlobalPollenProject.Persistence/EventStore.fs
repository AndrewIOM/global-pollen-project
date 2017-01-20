module EventStore

open System
open Microsoft.EntityFrameworkCore
open Serialisation
open GlobalPollenProject.Core.Types

exception WrongExpectedVersionException

let filter<'TEvent> ev = 
    match box (fst ev) with
    | :? 'TEvent as tev -> Some (tev,(snd ev))
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

let deserialise<'TEvent> (event:EventInfo) : ('TEvent * int) option = 
    match deserializeUnion<'TEvent> event.EventType event.EventPayload with
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

    member this.Events = context.Events |> Seq.toList

    member this.SaveEvent = 
        saveEvent.Publish 

    member this.ReadStream<'a> streamId version count = 

        printfn "Type to read: %s" typeof<'a>.Name

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
            |> List.map (fun x -> (deserializeUnion<'a> x.EventType x.EventPayload), x.StreamVersion)
            |> List.choose (fun x -> match fst x with
                                     | Some e -> Some (e, (snd x))
                                     | None -> None),
            //|> List.choose filter<'a>,
            lastEventNumber,
            if lastEventNumber < version + count 
                then None 
                else Some (lastEventNumber+1)

    member this.Save stream expectedVersion (events: 'a list) = 

        let eventsWithVersion =
            events
            |> List.mapi (fun index event -> (event, expectedVersion + index + 1))

        let convertToEfType (event: 'a * int) =
            let serialisedEvent = serialise (fst event)
            {
                Id = serialisedEvent.Id
                StreamId = stream
                StreamVersion = snd event
                OccurredAt = DateTime.Now
                EventType = serialisedEvent.Type
                EventPayload = serialisedEvent.Payload
            }

        match (this.ReadStream<'a> stream expectedVersion 1) with
        | (_,v,_) when v = expectedVersion -> 
            context.Events.AddRange (eventsWithVersion |> List.map convertToEfType)
            context.SaveChanges() |> ignore
        
        | (_,v,_) when expectedVersion = -1 ->
            context.Events.AddRange (eventsWithVersion |> List.map convertToEfType)
            context.SaveChanges() |> ignore

        | _ -> raise WrongExpectedVersionException

        events |> List.iter (fun e -> saveEvent.Trigger(stream,upcast e))
