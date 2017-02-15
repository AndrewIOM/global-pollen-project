[<AutoOpen>]
module Specification

open Xunit
open Microsoft.EntityFrameworkCore
open EventStore
open System
open GlobalPollenProject.Core.CommandHandlers
open GlobalPollenProject.Core

let inline fold events =
    let initial = (^S: (static member initial: ^S) ()) 
    let evolve s = (^S: (static member evolve: ^S -> (^E -> ^S)) s)
    List.fold evolve initial events

let Given aggregate (events: 'a list) = aggregate, events
let When command (aggregate, events) = aggregate, events, command
let Expect expected (aggregate, events, command) =
     printfn "Given: %A" events
     printfn "When: %A" command
     printfn "Expects: %A" expected

     let result = 
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle command
     Assert.Equal<'T>(result,expected)

// let ExpectThrows<'Ex> (aggregate, events, command) =
//     printfn "Given: %A" events
//     printfn "When: %A" command
//     printfn "Expects: %A" typeof<'Ex>

//     (fun () ->
//         fold events
//         |> aggregate.handle command
//         |> ignore)
//     |> Assert.Throws<'Ex>
