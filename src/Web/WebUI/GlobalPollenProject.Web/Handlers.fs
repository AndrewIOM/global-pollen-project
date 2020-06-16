module Handlers

open Giraffe.Core
open Giraffe.ModelBinding
open Giraffe.ResponseWriters
open FSharp.Control.Tasks.V2.ContextInsensitive
open GlobalPollenProject.Web
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Reflection
open System
open System.IO
open System.Reflection
open System.ComponentModel
open Urls
open ReadModels
open Connections

///////////////////////////
/// User Profile Retrieval
///////////////////////////

module LoadProfile =

    open Microsoft.AspNetCore.Identity

    let parseGuid (i:string) =
        match System.Guid.TryParse i with
        | (true,g) -> Ok g
        | (false,g) -> Error InvalidRequestFormat

    let resultToOption r =
        match r with
        | Ok o -> Some o
        | Error _ -> None

    let currentUserId (ctx:HttpContext) () =
        async {
            return { Firstname = "Cool" |> Some; Lastname = "Cool" |> Some }
            // let manager = ctx.GetService<UserManager<ApplicationUser>>()
            // let! user = manager.GetUserAsync(ctx.User) |> Async.AwaitTask
            // return Guid.Parse user.Id
        } |> Async.RunSynchronously

    // let getPublicProfile userId =
    //     userId
    //     |> parseGuid
    //     |> Result.bind User.getPublicProfile
    //     |> resultToOption

    // let loggedInUser (ctx:HttpContext) =
    //     async {
    //         let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //         if signInManager.IsSignedIn(ctx.User) then 
    //             let manager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let! user = manager.GetUserAsync(ctx.User) |> Async.AwaitTask
    //             return getPublicProfile user.Id
    //         else return None
    //     }


/////////////////////
/// View Handlers
/////////////////////

let htmlView v : HttpHandler =
    fun next ctx ->
        let userProfile =
            if ctx.User.Identity.IsAuthenticated
            then Some { Curator = false; FirstName = "LoggedIn"; Groups = []; IsPublic = false; LastName = "McLoggedIn"; Score = 200.; Title = "Mr"; UserId = Guid.NewGuid()}
            else None
        htmlView (v userProfile) next ctx

let renderView next ctx v =
    htmlView v next ctx

let serviceErrorToView err next ctx =
    match err with
    | ServiceError.NotFound -> ctx |> (clearResponse >=> setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound) next
    | _ -> ctx |> (clearResponse >=> setStatusCode 500 >=> htmlView HtmlViews.StatusPages.error) next

let renderViewResult v next ctx result =
    match result with
    | Ok r -> htmlView (v r) next ctx
    | Error e -> serviceErrorToView e next ctx

let errorHandler (ex : Exception) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> htmlView HtmlViews.StatusPages.error

/////////////////////
/// Validation
/////////////////////

let inline tryBindJson< ^T> (ctx:HttpContext) =
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

let viewOrError view model : HttpHandler =
    fun next ctx ->
        match model with
        | Ok m -> view m next ctx
        | Error e -> serviceErrorToView e next ctx

let coreAction' action (ctx:HttpContext) =
    task {
        let core = ctx.GetService<CoreMicroservice>()
        let! result = action |> core.Apply
        return result
    }

let coreAction action view next (ctx:HttpContext) =
    task {
        let! result = coreAction' action ctx
        return! renderViewResult view next ctx result
    }

let coreApiAction action : HttpHandler =
    fun next ctx ->
        task {
            let! result = coreAction' action ctx
            return! toApiResult next ctx result
        }

/// Pass-through query string model to core action and return API result
let apiResultFromQuery<'a,'b> (coreAction:'a->CoreFunction<'b>) : HttpHandler =
    let error str = text str
    let success model : HttpHandler =
        fun next ctx ->
            task {
                let core = ctx.GetService<CoreMicroservice>()
                let! result = coreAction model |> core.Apply
                return! toApiResult next ctx result
            }
    tryBindQuery<'a> error None success
