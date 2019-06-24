module Connections

open System
open Giraffe
open System.Net.Http
open Microsoft.Extensions.Options

//////////////////
/// Models
//////////////////

[<CLIMutable>]
type AppSettings = {
    CoreUrl: string
    IdentityUrl: string
}

type Login = {
    userName: string
    password: string
}

type SecurityToken = {
    auth_token: string
}

//////////////////
/// Identity Service
//////////////////

type AuthenticationService(client:HttpClient, appSettings:IOptions<AppSettings>) =

    member __.Login(loginRequest:LoginRequest) =
        async {
            let url = sprintf "%s/api/authenticate" appSettings.Value.IdentityUrl
            printfn "Connecting to: %s" url
            printfn "Login Request: %A" loginRequest
            let json = Serialisation.serialise {userName = loginRequest.Email; password = loginRequest.Password}
            match json with
            | Ok j ->
                let! response = client.PostAsync(url, new StringContent(j, Text.Encoding.UTF8)) |> Async.AwaitTask
                let! str = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "Returned %s" str
                printfn "Sent %s" (response.RequestMessage.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously)
                match Serialisation.deserialise<SecurityToken> str with
                | Ok s -> return Ok s
                | Error e -> 
                    printfn "Login error %s" e
                    return Error e
            | Error e -> return Error e
        }

    member __.Register(registerRequest:NewAppUserRequest) =
        let url = sprintf "%s/api/register" appSettings.Value.IdentityUrl
        printfn "Connecting to: %s" url
        async {
            let! response = client.PostAsJsonAsync(url, registerRequest) |> Async.AwaitTask
            let! str = response.Content.ReadAsStringAsync() |> Async.AwaitTask
            match Serialisation.deserialise<Result<string,ValidationError list>> str with
            | Ok s -> return s
            | Error e -> return invalidOp "Help"
        }

//////////////////
/// Core / Other Services
//////////////////


/// Represents a function that accesses a core service
type CoreFunction<'a> = HttpClient -> System.Uri -> Async<Result<'a,ServiceError>>


type Microservice(client:HttpClient, baseUrl:string) =

    member this.Apply (fn:CoreFunction<'a>) = fn client (Uri(baseUrl, UriKind.Absolute))





let microservice httpClient baseUrl (fn:CoreFunction<'a>) : HttpHandler =
    fun next ctx ->
        // Apply a core service with its arguments
        // Handle any errors
        let r = fn
        next ctx


module CoreAccess = 

    module Taxonomy =

        // Points of failure:
        // A. Url doesn't exist
        // B. Bad request / problem with response
        // C. Cannot deserialise response

        let getSlide collectionId slideId : CoreFunction<Responses.SlidePageViewModel> =
            fun c u -> async {
                let! response = c.GetStringAsync(u) |> Async.AwaitTask // TODO add in colId and slideid
                match Serialisation.deserialise<Result<Responses.SlidePageViewModel,ServiceError>> response with
                | Ok m -> return m
                | Error _ -> return Error ServiceError.InvalidRequestFormat
            }


// let slideHandler =
//     microservice <| CoreAccess.Taxonomy.getSlide "Cool1" "Cool2"

