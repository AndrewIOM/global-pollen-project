[<AutoOpen>]
module Specification

open Xunit
open Microsoft.EntityFrameworkCore
open EventStore
open System
open GlobalPollenProject.Core.CommandHandlers
open GlobalPollenProject.Core
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
     Assert.Equal<'Event>(result,expected)
let ExpectInvalidOp (aggregate, events, command) =
    printfn "Given: %A" events
    printfn "When: %A" command
    printfn "Expects: Invalid Op"

    (fun () ->
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle command
        |> ignore)
    |> Assert.Throws<System.InvalidOperationException> |> ignore
    
let ExpectInvalidArg (aggregate, events, command) =
    printfn "Given: %A" events
    printfn "When: %A" command
    printfn "Expects: Invalid Arg"

    (fun () ->
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle command
        |> ignore)
    |> Assert.Throws<System.ArgumentException> |> ignore