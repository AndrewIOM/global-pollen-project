module Handlers

open System
open Giraffe.Tasks
open Giraffe.HttpHandlers
open Giraffe.HttpContextExtensions
open Giraffe.Razor.HttpHandlers
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open GlobalPollenProject.Core.Composition

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

/////////////////////
/// Validation
/////////////////////

let bindJson<'a> (ctx:HttpContext) =
    try ctx.BindJson<'a>() |> Async.AwaitTask |> Async.RunSynchronously |> Ok
    with
    | _ -> Error InvalidRequestFormat

let bindQueryString<'a> (ctx:HttpContext) =
    try ctx.BindQueryString<'a>() |> Ok
    with
    | _ -> Error InvalidRequestFormat

/////////////////////
/// API Helpers
/////////////////////

let toApiResult next ctx result =
    match result with
    | Ok list -> json list next ctx
    | Error e -> 
        (setStatusCode 400 >=>
            match e with
            | Validation valErrors -> json <| { Message = "Invalid request"; Errors = valErrors }
            | InvalidRequestFormat -> json <| { Message = "Your request was not in a valid format"; Errors = [] }
            | _ -> json <| { Message = "Internal error"; Errors = [] } ) next ctx

/////////////////////
/// View Helpers
/////////////////////

let renderView name model =
    warbler (fun x -> razorHtmlView name model)

let serviceErrorToView err next ctx =
    match err with
    | ServiceError.NotFound -> ctx |> (clearResponse >=> setStatusCode 404 >=> renderView "NotFound" None) next
    | _ -> ctx |> (clearResponse >=> setStatusCode 500 >=> renderView "Error" None) next

let toViewResult view next ctx result =
        match result with
        | Ok model -> renderView view model next ctx
        | Error e -> serviceErrorToView e next ctx