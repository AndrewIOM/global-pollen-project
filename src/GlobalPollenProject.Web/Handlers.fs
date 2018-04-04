module Handlers

open Giraffe.Core
open Giraffe.ModelBinding
open Giraffe.ResponseWriters
open Giraffe.Razor.HttpHandlers
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Reflection
open System
open System.IO
open System.Reflection
open System.ComponentModel

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

/////////////////////
/// Validation
/////////////////////

let inline bindJson< ^T> (ctx:HttpContext) =
    let body = ctx.Request.Body
    use reader = new StreamReader(body, true)
    let reqBytes = reader.ReadToEndAsync() |> Async.AwaitTask |> Async.RunSynchronously
    match Serialisation.deserialise< ^T> reqBytes with
    | Ok o -> Ok o
    | Error e -> Error InvalidRequestFormat

/////////////////////
/// Query String Decode
/////////////////////

type HttpContext with

    member this.TryGetQueryStringValueDecoded (key : string) =
        match this.Request.Query.TryGetValue key with
        | true, value -> Some (value.ToString())
        | _           -> None

    member this.DecodeAndBindQueryString<'T>() =
        let obj   = Activator.CreateInstance<'T>()
        let props = obj.GetType().GetProperties(BindingFlags.Instance ||| BindingFlags.Public)
        props
        |> Seq.iter (fun p ->
            match this.TryGetQueryStringValueDecoded p.Name with
            | None            -> ()
            | Some queryValue ->

                let isOptionType =
                    p.PropertyType.GetTypeInfo().IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() = typedefof<Option<_>>

                let propertyType =
                    if isOptionType then
                        p.PropertyType.GetGenericArguments().[0]
                    else
                        p.PropertyType

                let propertyType =
                    if propertyType.GetTypeInfo().IsValueType then
                        (typedefof<Nullable<_>>).MakeGenericType([|propertyType|])
                    else
                        propertyType

                let converter = TypeDescriptor.GetConverter propertyType

                let value = converter.ConvertFromInvariantString(queryValue)

                if isOptionType then
                    let cases = FSharpType.GetUnionCases(p.PropertyType)
                    let value =
                        if isNull value then
                            FSharpValue.MakeUnion(cases.[0], [||])
                        else
                            FSharpValue.MakeUnion(cases.[1], [|value|])
                    p.SetValue(obj, value, null)
                else
                    p.SetValue(obj, value, null))
        obj

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

let toGiraffeView next ctx v =
    htmlView v next ctx

let toGiraffeViewResult next ctx result =
    match result with
    | Ok v -> htmlView v next ctx
    | Error e -> serviceErrorToView e next ctx