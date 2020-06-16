module GlobalPollenProject.App.App

open System
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Giraffe

open GlobalPollenProject.Core.Composition

module U =  UseCases
module R = ReadModels

/////////////////////
/// Giraffe Handlers
/////////////////////

let inline bindJson< ^T> (ctx:HttpContext) =
    let body = ctx.Request.Body
    use reader = new IO.StreamReader(body, true)
    let reqBytes = reader.ReadToEndAsync() |> Async.AwaitTask |> Async.RunSynchronously
    match Serialisation.deserialise< ^T> reqBytes with
    | Ok o -> Ok o
    | Error e -> Error InvalidRequestFormat

let apiResult next ctx result =
    match result with
    | Ok list -> json list next ctx
    | Error e -> 
        (setStatusCode 400 >=>
            match e with
            | Validation valErrors -> json <| { Message = "Invalid request"; Errors = valErrors }
            | InvalidRequestFormat -> json <| { Message = "Your request was not in a valid format"; Errors = [] }
            | InMaintenanceMode -> json <| { Message = "Maintenance in progress"; Errors = [] }
            | _ -> json <| { Message = "Internal error"; Errors = [] } ) next ctx

let inline postApi< ^T, ^R> (action:^T->Result< ^R,ServiceError>) : HttpHandler =
    fun next ctx ->
        bindJson< ^T> ctx
        |> bind action
        |> apiResult next ctx

let errorHandler errors = json errors

let inline apif< ^T, ^R> (action:^T->Result< ^R,ServiceError>) : HttpHandler =
    tryBindQuery< ^T> errorHandler None (fun model next ctx -> model |> action |> (fun r -> json r next ctx))

let inline api< ^R> (action:unit->Result< ^R,ServiceError>) : HttpHandler =
    fun next ctx -> action() |> (fun r -> json r next ctx)

/////////////////////
/// Authentication
/////////////////////

let getCurrentUser () = invalidOp "Cool"

/////////////////////////////
/// Giraffe Router
/////////////////////////////

// TODO
// - Authorise routes using tokens as per microservices book

open ReadModels
open System.IdentityModel.Tokens.Jwt
open Microsoft.AspNetCore.Authentication.JwtBearer

let opt (s:string) =
    match s.Length with
    | 0 -> None
    | _ -> Some s

let notInMaintenanceMode next ctx : HttpFuncResult =
    match U.inMaintenanceMode with
    | true -> apiResult next ctx (Error InMaintenanceMode)
    | false -> next ctx

let routes : HttpHandler =
    notInMaintenanceMode >=>
    choose [
        subRoute "/api/v1"
            (choose [
                // Master Reference Collection
                GET >=> route   "/MRC/Taxon/Autocomplete"   >=> apif<TaxonAutocompleteRequest,TaxonAutocompleteItem list> U.Taxonomy.autocomplete
                GET >=> route   "/MRC/Taxon"            >=> apif<TaxonPageRequest, PagedResult<TaxonSummary>> U.Taxonomy.list
                GET >=> routef  "/MRC/Taxon/%s/%s/%s"   (fun (f,g,s) n c -> U.Taxonomy.getByName f (opt g) (opt s) |> apiResult n c)
                GET >=> routef  "/MRC/Taxon/Id/%s"      (fun i n c -> U.Taxonomy.getById (Guid(i)) |> apiResult n c)
                GET >=> routef  "/MRC/Collection/%s/%s" (fun (col,s) n c -> U.Taxonomy.getSlide col s |> apiResult n c)
                GET >=> routef  "/MRC/Collection/%s/%i" (fun (col,i) n c -> U.IndividualReference.getDetail col i |> apiResult n c)
                GET >=> routef  "/MRC/Collection/%s"    (fun s n c -> U.IndividualReference.getLatestVersion s |> apiResult n c)
                GET >=> route   "/MRC/Collection"       >=> apif U.IndividualReference.list

                // Backbone
                GET >=> route  "/Taxonomy/Search"       >=> apif U.Backbone.searchNames
                GET >=> route  "/Taxonomy/Match"        >=> apif U.Backbone.tryMatch
                GET >=> route  "/Taxonomy/Trace"        >=> apif U.Backbone.tryTrace

                // Statistics
                GET >=> route   "/Statistics/Home"      >=> api U.Statistic.getHomeStatistics
                GET >=> route   "/Statistics/System"    >=> api U.Statistic.getSystemStats

                // User
                GET >=> route   "/User/Profile"         >=> apif U.User.getPublicProfile
                POST >=> route  "/User/Register"        >=> postApi (getCurrentUser |> U.User.register)

                // Curation
                GET  >=> route  "/Curate/Pending"       >=> api U.Curation.listPending
                POST >=> route  "/Cutrate/Assign"       >=> postApi U.User.grantCuration
                POST >=> route  "/Curate/Decide"        >=> postApi (getCurrentUser |> U.Curation.issueDecision)

                // Unknown Material
                GET >=> routef  "/Unknown/%s"           (fun g n c -> U.UnknownGrains.getDetail g |> apiResult n c)
                GET >=> route   "/Unknown"              >=> api U.UnknownGrains.listUnknownGrains
                GET >=> route   "/Unknown/MostWanted"   >=> api U.UnknownGrains.getTopScoringUnknownGrains
                POST >=> route  "/Unknown/Submit"       >=> postApi (U.UnknownGrains.submitUnknownGrain getCurrentUser)
                POST >=> route  "/Unknown/Identify"     >=> postApi (U.UnknownGrains.identifyUnknownGrain getCurrentUser)

                // User's equipment
                GET >=> route   "/User/Microscope"      >=> apif U.Calibrations.getMyCalibrations
                POST >=> route  "/User/Microscope/Setup"        >=> postApi (getCurrentUser |> U.Calibrations.setupMicroscope)
                POST >=> route  "/User/Microscope/Calibrate"    >=> postApi U.Calibrations.calibrateMagnification

                // Digitise
                GET >=> route   "/Digitise/Collection"          >=> apif (getCurrentUser |> U.Digitise.myCollections)
                GET >=> routef  "/Digitise/Collection/%s"       (fun col n c -> col |> U.Digitise.getCollection |> apiResult n c)
                POST >=> route  "/Digitise/Collection/Start"    >=> postApi (U.Digitise.startNewCollection getCurrentUser)
                POST >=> routef "/Digitise/Collection/%s/Publish"   (fun col n c -> U.Digitise.publish getCurrentUser col |> apiResult n c)
                POST >=> route  "/Digitise/Slide/Add"       >=> postApi U.Digitise.addSlideRecord
                POST >=> route  "/Digitise/Slide/Void"      >=> postApi U.Digitise.voidSlide
                POST >=> route  "/Digitise/Slide/AddImage"  >=> postApi U.Digitise.uploadSlideImage

                // Administration
                POST >=> route "/Admin/RebuildReadModel"    >=> postApi U.Admin.rebuildReadModel
                POST >=> route "/Admin/Users"               >=> postApi U.Admin.listUsers
            ])
        ]

/////////////////////////////
/// Define App Functions
/////////////////////////////

type Startup () =

    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear() |> ignore

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddDataProtection() |> ignore
        services.AddAuthentication(fun opt ->
            opt.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
            opt.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme )
            .AddJwtBearer(fun opt ->
                opt.Authority <- this.Configuration.GetValue<string> "IdentityUrl"
                opt.RequireHttpsMetadata <- false
                opt.Audience = "core" |> ignore ) |> ignore
        services.AddGiraffe() |> ignore

    member __.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore
            U.Admin.rebuildReadModel() |> ignore
            // Seed an extract of taxonomic names if there are none:
            match U.Backbone.searchNames { Rank = "Family"; LatinName = "Poaceae"; Family = "Poaceae"; Genus = ""; Species = ""; Authorship = "" } with
            | Ok s -> if s.Length = 0 then U.Backbone.importAll "data/plant-list-extract.txt"
            | Error _ -> ()
        app.UseStaticFiles() |> ignore
        app.UseGiraffe routes |> ignore

    member val Configuration : IConfiguration = null with get, set

let configureLogging (loggerFactory : ILoggingBuilder) =
    loggerFactory.AddConsole().AddDebug() |> ignore

let BuildWebHost args =
    WebHost
        .CreateDefaultBuilder(args)
        .UseKestrel(fun opt ->
            opt.Limits.MaxRequestBodySize <- Nullable<int64>(int64 (1024 * 1024 * 10)))
        .ConfigureLogging(configureLogging)
        .UseStartup<Startup>()
        .Build()

[<EntryPoint>]
let main args =
    BuildWebHost(args).Run()
    0