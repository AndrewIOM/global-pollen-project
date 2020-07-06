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

//////////////////
/// Core / Other Services
//////////////////

/// Represents a function that accesses a core service
type CoreFunction<'a> = HttpClient -> System.UriBuilder -> Async<Result<'a,ServiceError>>

/// A connection to the core microservice gateway
type CoreMicroservice(client:HttpClient, appSettings:IOptions<AppSettings>) =
    let baseUrl = appSettings.Value.CoreUrl
    member __.Apply (fn:CoreFunction<'a>) = fn client (UriBuilder(baseUrl))


module CoreActions =

    open Responses
    open System.Reflection
    open System.Text
    open System.Net.Mime

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
            printfn "Received json from GET: %s" response
            match Serialisation.deserialise<Result<'a,ServiceError>> response with
            | Ok m -> return m
            | Error _ -> return Error InvalidRequestFormat
        }

    let CPOST<'a, 'b> (data:'a) (route:string) (c:HttpClient) (u:UriBuilder) = 
        async {
            u.Path <- route
            let json = Serialisation.serialise data
            match json with
            | Ok j ->
                let stringContent = new StringContent(j, UnicodeEncoding.UTF8, MediaTypeNames.Application.Json)
                let! response = c.PostAsync(u.Uri, stringContent) |> Async.AwaitTask
                if response.IsSuccessStatusCode
                then
                    let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask
                    printfn "Received json from POST: %s" content
                    match Serialisation.deserialise<Result<'b,ServiceError>> content with
                    | Ok m -> return m
                    | Error _ -> return Error InvalidRequestFormat
                else return Error InvalidRequestFormat
            | Error _ -> return Error InvalidRequestFormat
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
        let publicProfile (req:Guid) = CGET None <| sprintf "/api/v1/anon/User/Profile/%s" (req.ToString())
        let register (req:NewAppUserRequest) = CPOST req "/api/v1/User/Register"

    module Curate =
        let listPending () = CGET None "/api/v1/anon/Curate/Pending"
    
    module UnknownMaterial =
        let itemDetail (itemId:string) = CGET None (sprintf "/api/v1/anon/Unknown/%s" itemId)
        let list () = CGET None "/api/v1/anon/Unknown"
        let mostWanted () = CGET None "/api/v1/anon/Unknown/MostWanted"
        let submit req = CPOST<AddUnknownGrainRequest,unit> req "/api/v1/Unknown/Submit"
        let identify (req:IdentifyGrainRequest) = CPOST req "/api/v1/Unknown/Identify"
    
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