[<AutoOpen>]
module Specification

open Xunit
open Microsoft.EntityFrameworkCore
open EventStore
open System
open GlobalPollenProject.Core.CommandHandlers
open GlobalPollenProject.Core

let defaultDependencies = 
    let generateGuid() = Guid.NewGuid()
    let logger message = printfn "Logged message"
    let calcIdentity taxon = None
    let upload image = SingleImage (Url.create "https://globalpollenproject.org/sometesturl.jpg")
    let validate = fun unit -> Some (TaxonId (generateGuid()))

    {GenerateId          = generateGuid;
     Log                 = logger;
     UploadImage         = upload;
     ValidateTaxon       = validate;
     GetGbifId           = fun unit -> None;
     GetNeotomaId        = fun unit -> None;
     CalculateIdentity   = calcIdentity }

let Given aggregate dep (events: 'a list) = aggregate, events, dep
let When command (aggregate, events, dep) = aggregate, events, dep, command
let Expect expected (aggregate, events, deps, command) =
     printfn "Given: %A" events
     printfn "When: %A" command
     printfn "Expects: %A" expected

     let result = 
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle deps command
     Assert.Equal<'Event>(result,expected)
let ExpectInvalidOp (aggregate, events, deps, command) =
    printfn "Given: %A" events
    printfn "When: %A" command
    printfn "Expects: Invalid Op"

    (fun () ->
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle deps command
        |> ignore)
    |> Assert.Throws<System.InvalidOperationException> |> ignore
    
let ExpectInvalidArg (aggregate, events, deps, command) =
    printfn "Given: %A" events
    printfn "When: %A" command
    printfn "Expects: Invalid Arg"

    (fun () ->
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle deps command
        |> ignore)
    |> Assert.Throws<System.ArgumentException> |> ignore