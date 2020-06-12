module Connections

open System
open System.Net.Http
open Microsoft.Extensions.Options
open ReadModels

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
type CoreFunction<'a> = HttpClient -> System.UriBuilder -> Async<Result<'a,ServiceError>>

/// A connection to the core microservice
type CoreMicroservice(client:HttpClient, appSettings:IOptions<AppSettings>) =
    let baseUrl = appSettings.Value.CoreUrl
    member __.Apply (fn:CoreFunction<'a>) = fn client (UriBuilder(baseUrl))


module CoreActions =

    open Responses
    open System.Reflection

    let toQueryString x =
        let formatElement (pi : PropertyInfo) =
            sprintf "%s=%O" pi.Name <| pi.GetValue x
        x.GetType().GetProperties()
        |> Array.map formatElement
        |> String.concat "&"

    let CGET<'a,'b> (queryData:'b option) (route:string) (c:HttpClient) (u:UriBuilder) = 
        async {
            let queryString =
                match queryData with
                | Some data -> data |> toQueryString
                | None -> ""
            u.Query <- queryString
            u.Path <- route
            let! response = c.GetStringAsync(u.Uri) |> Async.AwaitTask
            printfn "Response was %s" response
            match Serialisation.deserialise<Result<'a,ServiceError>> response with
            | Ok m -> return m
            | Error _ -> return Error InvalidRequestFormat
        }

    let CPOST<'a, 'b> (data:'a) (route:string) (c:HttpClient) (u:UriBuilder) = 
        async {
            u.Path <- route
            let! response = c.PostAsJsonAsync(u.Uri, data) |> Async.AwaitTask
            if response.IsSuccessStatusCode
            then
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                printfn "Contents returned was %A" content
                match Serialisation.deserialise<Result<'b,ServiceError>> content with
                | Ok m -> return m
                | Error _ -> return Error InvalidRequestFormat
            else return Error InvalidRequestFormat
        }

    module MRC =
        let autocompleteTaxon (req:TaxonAutocompleteRequest) = CGET (Some req) "/api/v1/anon/MRC/Taxon/Autocomplete"
        let list (req:TaxonPageRequest) = CGET (Some req) "/api/v1/anon/MRC/Taxon"
        let getByName family genus species = CGET None (sprintf "/api/v1/anon/MRC/Taxon/%s/%s/%s" family genus species)
        let getById (guid:Guid) = CGET None (sprintf "/api/v1/anon/MRC/Taxon/Id/%s" (guid.ToString()))
        let getSlide colId slideId = CGET None (sprintf "/api/v1/anon/MRC/Collection/%s/%s" colId slideId)

    module IndividualCollections = 
        let collectionDetail colId version = CGET None (sprintf "/api/v1/anon/MRC/Collection/%s/%i" colId version)
        let collectionDetailLatest colId = CGET None (sprintf "/api/v1/anon/MRC/Collection/%s" colId)
        let list (req:PageRequest) = CGET (Some req) "/api/v1/anon/MRC/Collection"

    module Backbone =
        let search (req:BackboneSearchRequest) = CGET (Some req) "/api/v1/anon/Taxonomy/Search"
        let tryMatch (req:BackboneSearchRequest) = CGET (Some req) "/api/v1/anon/Taxonomy/Match"
        let tryTrace (req:BackboneSearchRequest) = CGET (Some req) "/api/v1/anon/Taxonomy/Trace"

    module Statistics =
        let home () = CGET None "/api/v1/anon/Statistics/Home"
        let system () = CGET None "/api/v1/anon/Statistics/System"
    
    module User =
        let publicProfile (req:Guid) = CGET (Some req) "/api/v1/anon/User/Profile"
        let register (req:NewAppUserRequest) = CPOST req "/api/v1/User/Profile"

    module Curate =
        let listPending () = CGET None "/api/v1/anon/Curate/Pending"
    
    module UnknownMaterial =
        let itemDetail (itemId:string) = CGET None (sprintf "/api/v1/anon/Unknown/%s" itemId)
        let list () = CGET None "/api/v1/anon/Unknown"
        let mostWanted () = CGET None "/api/v1/anon/Unknown/MostWanted"
        let submit (req:AddUnknownGrainRequest) = CPOST req "/api/v1/Unknown/Submit"
        let identity (req:IdentifyGrainRequest) = CPOST req "/api/v1/Unknown/Identify"
    
    module Digitise =
        let myCollections () : CoreFunction<EditableRefCollection list> = CGET None "/api/v1/Digitise/Collection"
        let getCollection (req:string) = CGET None (sprintf "/api/v1/Digitise/Collection/%s" req)
        let startCollection (req:StartCollectionRequest) = CPOST req "/api/v1/Digitise/Collection/Start"
        let publishCollection (req:string) = CPOST req (sprintf "/api/v1/Digitise/Collection/%s/Publish" req)
        let recordSlide (req:SlideRecordRequest) = CPOST req "/api/v1/Digitise/Slide/Add"
        let voidSlide (req:VoidSlideRequest) = CPOST req "/api/v1/Digitise/Slide/Void"
        let uploadImage (req:SlideImageRequest) = CPOST req "/api/v1/Digitise/Slide/AddImage"
    
    module System =
        let rebuildReadModel () = CPOST () "/api/v1/Admin/RebuildReadModel"