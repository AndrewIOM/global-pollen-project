[<AutoOpen>]
module Specification

open Xunit
open System
open GlobalPollenProject.Core.Aggregate
open GlobalPollenProject.Core.DomainTypes

let domainDefaultDeps = 
    let generateGuid() = Guid.NewGuid()
    let logger message = printfn "Logged message"
    let calcIdentity taxon = Ok None
    let validate = fun unit -> Some (TaxonId (generateGuid()))

    {GenerateId          = generateGuid
     Log                 = logger
     Random              = System.Random()
     ValidateTaxon       = validate
     GetGbifId           = fun unit -> (None |> Ok)
     GetNeotomaId        = fun unit -> (None |> Ok)
     GetEolId            = fun unit -> (None |> Ok)
     GetTime             = (fun x -> DateTime(2017,1,1))
     GetImageDimension   = (fun img -> Ok { Height = 800<pixels>; Width = 600<pixels> })
     CalculateIdentity   = calcIdentity }

let Given aggregate dep (events: 'a list) = aggregate, events, dep

let When command (aggregate, events, dep) = aggregate, events, dep, command

let Expect expected (aggregate, events, deps, command) =
     let result = 
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle deps command
     Assert.Equal<'Event>(expected, result)

let ExpectInvalidOp (aggregate, events, deps, command) =
    (fun () ->
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle deps command
        |> ignore)
    |> Assert.Throws<System.InvalidOperationException> |> ignore
    
let ExpectInvalidArg (aggregate, events, deps, command) =
    (fun () ->
        events
        |> List.fold aggregate.evolve aggregate.initial
        |> aggregate.handle deps command
        |> ignore)
    |> Assert.Throws<System.ArgumentException> |> ignore