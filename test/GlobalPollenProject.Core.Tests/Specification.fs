[<AutoOpen>]
module Specification

open Xunit
open System
open GlobalPollenProject.Core.Aggregate
open GlobalPollenProject.Core.DomainTypes

let domainDefaultDeps = 
    let generateGuid() = Guid.NewGuid()
    let logger message = printfn "Logged message"
    let calcIdentity taxon = None
    let validate = fun unit -> Some (TaxonId (generateGuid()))

    {GenerateId          = generateGuid
     Log                 = logger
     ValidateTaxon       = validate
     GetGbifId           = fun unit -> (None |> Ok)
     GetNeotomaId        = fun unit -> (None |> Ok)
     GetTime             = (fun x -> DateTime(2017,1,1))
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