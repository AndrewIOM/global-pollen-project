module Serialisation

open Microsoft.FSharp.Reflection
open Microsoft.FSharpLu.Json
open GlobalPollenProject.Core.DomainTypes

let serialise (o:'a) = 
    try Compact.serialize o |> Ok
    with
    | _ -> Error "Could not serialise record"

let inline deserialise< ^T> json = 
    try BackwardCompatible.deserialize< ^T> json |> Ok
    with 
    | _ -> Error "Could not deserialise JSON"

let serialiseEventToBytes (e:'a) =
    let case,_ = FSharpValue.GetUnionFields(e, typeof<'a>)
    let json = Compact.serialize e |> System.Text.Encoding.ASCII.GetBytes
    case.Name, json

let inline deserialiseEventFromBytes< ^E> eventType (data:byte[]) =
    printfn "Deserialising a domain event: %s" typeof< ^E>.FullName
    use stream = new System.IO.MemoryStream(data)
    try BackwardCompatible.deserializeStream< ^E> stream
    with
    | _ -> 
        invalidOp (sprintf "There was a fatal problem when deserialising an event (%s)" eventType)
