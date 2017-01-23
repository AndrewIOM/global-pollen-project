[<AutoOpen>]
module Specifications

open GlobalPollenProject.Core.Aggregates.Grain

let Given (events: Event list) = events
let When (command: Command) events = events, command
let Expect (expected: Event list) (events, command) =
    printGiven events
    printWhen command
    printExpect expected

    events
    |> List.fold evolve State.initial
    |> handle command
    |> should equal expected

let ExpectThrows<'Ex> (events, command) =
    printGiven events
    printWhen command
    printExpectThrows typeof<'Ex>


    (fun () ->
        events
        |> List.fold evolve State.InitialState
        |> handle command
        |> ignore)
    |> should throw typeof<'Ex>