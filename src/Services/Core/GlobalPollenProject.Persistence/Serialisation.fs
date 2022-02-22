module Serialisation

open Microsoft.FSharp.Reflection
open Microsoft.FSharpLu.Json
open GlobalPollenProject.Core.Aggregates

let serialise (o:'a) = 
    try Compact.Legacy.serialize o |> Ok
    with
    | _ -> Error "Could not serialise record"

let inline deserialise< ^T> json = 
    try Compact.Legacy.deserialize< ^T> json |> Ok
    with 
    | _ -> Error "Could not deserialise JSON"

let serialiseEventToBytes (e:'a) =
    let case,_ = FSharpValue.GetUnionFields(e, typeof<'a>)
    let json = Compact.Legacy.serialize e |> System.Text.Encoding.UTF8.GetBytes
    case.Name, json

let inline deserialiseEventFromBytes< ^E> eventType (data:byte[]) =
    use stream = new System.IO.MemoryStream(data)
    try Compact.Legacy.deserializeStream< ^E> stream
    with
    | e -> 
        invalidOp (sprintf "There was a fatal problem when deserialising an event (%s). The error was: %s" eventType e.Message)

let fromString<'a> (s:string) =
    match FSharpType.GetUnionCases typeof<'a> |> Array.filter (fun case -> case.Name = s) with
    |[|_|] -> Some s//(FSharpValue.MakeUnion(case,[||]) :?> 'a)
    |_ -> None

let deserialiseEventByName eventName (data:byte[]) : obj =
    if fromString<Calibration.Event> eventName |> Option.isSome 
        then deserialiseEventFromBytes<Calibration.Event> eventName data :> obj
    else if fromString<Grain.Event> eventName |> Option.isSome 
        then deserialiseEventFromBytes<Grain.Event> eventName data :> obj
    else if fromString<ReferenceCollection.Event> eventName |> Option.isSome 
        then deserialiseEventFromBytes<ReferenceCollection.Event> eventName data :> obj
    else if fromString<Taxonomy.Event> eventName |> Option.isSome 
        then deserialiseEventFromBytes<Taxonomy.Event> eventName data :> obj
    else if fromString<User.Event> eventName |> Option.isSome 
        then deserialiseEventFromBytes<User.Event> eventName data :> obj
    else None :> obj // Handles non-domain eventstore events e.g. $statistics
