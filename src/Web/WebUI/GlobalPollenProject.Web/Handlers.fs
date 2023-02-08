module Handlers

open Giraffe
open GlobalPollenProject.Web
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Reflection
open System
open System.IO
open System.Reflection
open System.ComponentModel
open Connections
open ReadModels

///////////////////////////
/// User Profile Retrieval
///////////////////////////

type ApplicationUser = {
    Id: System.Guid
    Title: string option
    Firstname: string option
    Lastname: string option
    Organisation: string option
    Profile: PublicProfile option
}

module Core =
    
    /// Access a core action using DI services
    let coreAction' action (ctx:HttpContext) =
        task {
            let core = ctx.GetService<CoreMicroservice>()
            let! result = action |> core.Apply
            return result
        }

module Profile =

    open System.Security.Principal
    open System.Security.Claims

    let parseGuid (i:string) =
        match System.Guid.TryParse i with
        | (true,g) -> Some g
        | (false,g) -> None
    
    /// Access the claims provided by the external Identity.API service
    let tryParsePrincipal (principal:IPrincipal) =
        match principal with
        | :? ClaimsPrincipal as claims ->
            claims.Claims
            |> Seq.tryFind(fun x -> x.Type = "sub")
            |> Option.bind(fun c -> parseGuid c.Value)
            |> Option.map(fun i ->
                { Firstname =  claims.Claims |> Seq.tryFind(fun x -> x.Type = "given_name")  |> Option.bind(fun x -> Some x.Value)
                  Lastname = claims.Claims |> Seq.tryFind(fun x -> x.Type = "family_name") |> Option.bind(fun x -> Some x.Value)
                  Organisation = claims.Claims |> Seq.tryFind(fun x -> x.Type = "organisation") |> Option.bind(fun x -> Some x.Value)
                  Title = claims.Claims |> Seq.tryFind(fun x -> x.Type = "title") |> Option.bind(fun x -> Some x.Value)
                  Id = i
                  Profile = None })
        | _ -> None
   
    let resultToOption r =
        match r with
        | Ok o -> Some o
        | Error _ -> None
     
    /// Access profile information specific to the Pollen Core.API
    let getPublicProfile ctx userId =
        userId
        |> CoreActions.User.publicProfile
        |> fun a -> Core.coreAction' a ctx

    /// Register a fresh public profile with the Core.API
    let registerPublicProfile ctx req =
        req
        |> CoreActions.User.register
        |> fun a -> Core.coreAction' a ctx
    
    /// Gets the public-facing profile information from the Core.API.
    let getAuthenticatedUser (ctx:HttpContext) =
        task {
            if ctx.User.Identity.IsAuthenticated then
                let userFromClaims = ctx.User |> tryParsePrincipal
                match userFromClaims with
                | Some user ->
                    let! profile = getPublicProfile ctx user.Id
                    return { user with Profile = profile |> resultToOption } |> Some
                | None -> return None
            else return None
        }

/////////////////////
/// View Handlers
/////////////////////

let htmlView v : HttpHandler =
    fun next ctx ->
        task {
            let! user = Profile.getAuthenticatedUser ctx
            let profile = user |> Option.bind(fun u -> u.Profile)
            return! htmlView (v profile) next ctx
        }

let renderView next ctx v =
    htmlView v next ctx

let serviceErrorToView err next ctx =
    match err with
    | ServiceError.NotFound -> ctx |> (clearResponse >=> setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound) next
    | ServiceError.InMaintenanceMode -> ctx |> (clearResponse >=> setStatusCode 503 >=> htmlView HtmlViews.StatusPages.maintenance) next
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
            | InMaintenanceMode -> json <| "System is under maintenance. Please try again later."
            | _ -> json <| { Message = "Internal error"; Errors = [] } ) next ctx

let viewOrError view model : HttpHandler =
    fun next ctx ->
        match model with
        | Ok m -> view m next ctx
        | Error e -> serviceErrorToView e next ctx

let coreAction action view next (ctx:HttpContext) =
    task {
        let! result = Core.coreAction' action ctx
        return! renderViewResult view next ctx result
    }

let formAction<'req,'result> (action:'req -> CoreFunction<'result>) error success : HttpHandler =
    fun next ctx ->
        task {
            let core = ctx.GetService<CoreMicroservice>()
            let! model = ctx.TryBindFormAsync()
            let validated = 
                match model with
                | Ok m -> Validation.validateModel m
                | Error _ -> Error InvalidRequestFormat
            let result = validated |> Result.bind (fun m -> action m |> core.Apply |> Async.RunSynchronously)
            return! 
                match result with
                | Ok r -> success r next ctx
                | Error e -> error e next ctx
        }

let coreApiAction action : HttpHandler =
    fun next ctx ->
        task {
            let! result = Core.coreAction' action ctx
            return! toApiResult next ctx result
        }

/// Pass-through query string model to core action and return API result
let apiResultFromQuery<'a,'b> (coreAction:'a->CoreFunction<'b>) : HttpHandler =
    fun next ctx ->
        let core = ctx.GetService<CoreMicroservice>()
        task {
            let result =
                ctx
                |> bindQueryString<'a>
                |> Result.bind Validation.validateModel
                |> Result.bind (fun m -> coreAction m |> core.Apply |> Async.RunSynchronously)
            return! result |> toApiResult next ctx
        }

/// Deserialise JSON model (in request body), pass to core action, and return API result
let inline apiResultFromBody< ^a, ^b> (coreAction: ^a->CoreFunction< ^b>) : HttpHandler =
    fun next ctx ->
        let model = 
            tryBindJson< ^a> ctx
            |> Result.bind Validation.validateModel
        let core = ctx.GetService<CoreMicroservice>()
        let result =
            task {
                match model with
                | Ok m -> return! coreAction m |> core.Apply
                | Error e -> return Error e
            }
        task {
            let! r = result
            return! toApiResult next ctx r
        }