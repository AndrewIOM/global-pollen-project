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

module Auth =

    open System.Security.Claims
    open System.Security.Principal     
      
    let checkUserIsLoggedIn : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            if isNotNull ctx.User && ctx.User.Identity.IsAuthenticated
            then next ctx
            else setStatusCode 401 earlyReturn ctx

    let parseGuid (i:string) =
        match System.Guid.TryParse i with
        | (true,g) -> Some g
        | (false,g) -> None
    
    let userIdFromClaims (principal:IPrincipal) =
        match principal with
        | :? ClaimsPrincipal as claims ->
            printfn "Claims are %A" (claims.Claims |> Seq.toList)
            claims.Claims
            |> Seq.tryFind(fun x -> x.Type = ClaimTypes.NameIdentifier)
            |> Option.bind(fun c -> parseGuid c.Value)
        | _ -> None

    /// Access current user ID from 
    let getCurrentUser (ctx:HttpContext) : UseCases.GetCurrentUser =
        fun () ->
            let id = userIdFromClaims ctx.User
            if id.IsNone then invalidOp "Could not get user ID from claims"
            else id.Value

let readAllBytes (s:System.IO.Stream) = 
    let ms = new System.IO.MemoryStream()
    s.CopyToAsync(ms) |> Async.AwaitTask |> Async.RunSynchronously // TODO Remove Await
    ms.ToArray()

let inline bindJson< ^T> (ctx:HttpContext) =
    let json =
        ctx.Request.Body
        |> readAllBytes
        |> System.Text.Encoding.UTF8.GetString
    printfn "Request json was %s" json
    match Serialisation.deserialise< ^T> json with
    | Ok o -> Ok o
    | Error e -> Error InvalidRequestFormat

let apiResult next ctx result =
    match result with
    | Ok list -> json (Ok list) next ctx
    | Error e -> (setStatusCode 400 >=> json (Error e)) next ctx
        //(setStatusCode 400 >=>
            //match e with
            //| Validation valErrors -> json <| { Message = "Invalid request"; Errors = valErrors }
            //| InvalidRequestFormat -> json <| { Message = "Your request was not in a valid format"; Errors = [] }
            //| InMaintenanceMode -> json <| { Message = "Maintenance in progress"; Errors = [] }
            //| _ -> json <| { Message = "Internal error"; Errors = [] } ) next ctx

let inline postApi< ^T, ^R> (action:^T->Result< ^R,ServiceError>) : HttpHandler =
    fun next ctx ->
        bindJson< ^T> ctx
        |> bind action
        |> apiResult next ctx

let inline postAuthApi< ^T, ^R> (action:UseCases.GetCurrentUser-> ^T->Result< ^R,ServiceError>) : HttpHandler =
    Auth.checkUserIsLoggedIn >=>
    fun next ctx ->
        let getUser = Auth.getCurrentUser ctx
        bindJson< ^T> ctx
        |> bind (action getUser)
        |> apiResult next ctx

let errorHandler errors = json errors

let inline apif< ^T, ^R> (action:^T->Result< ^R,ServiceError>) : HttpHandler =
    tryBindQuery< ^T> errorHandler None (fun model next ctx -> model |> action |> (fun r -> json r next ctx))

let inline apiAuthf< ^T, ^R> (action:UseCases.GetCurrentUser-> ^T->Result< ^R,ServiceError>) : HttpHandler =
    Auth.checkUserIsLoggedIn >=>
    tryBindQuery< ^T> errorHandler None (fun model next ctx ->
        let getUser = Auth.getCurrentUser ctx
        model |> action getUser |> (fun r -> json r next ctx))

let inline api< ^R> (action:unit->Result< ^R,ServiceError>) : HttpHandler =
    fun next ctx -> action() |> (fun r -> json r next ctx)

let inline apiAuth< ^R> (action:UseCases.GetCurrentUser->unit->Result< ^R,ServiceError>) : HttpHandler =
    Auth.checkUserIsLoggedIn >=>
    fun next ctx ->
        action (Auth.getCurrentUser ctx) () |> (fun r -> json r next ctx)


/////////////////////////////
/// Giraffe Router
/////////////////////////////

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
                GET >=> routef   "/User/Profile/%s"     (fun g n c -> U.User.getPublicProfile (Guid.Parse(g)) |> apiResult n c)
                POST >=> route  "/User/Register"        >=> postAuthApi U.User.register

                // Curation
                GET  >=> route  "/Curate/Pending"       >=> api U.Curation.listPending
                POST >=> route  "/Cutrate/Assign"       >=> postApi U.User.grantCuration
                POST >=> route  "/Curate/Decide"        >=> postAuthApi U.Curation.issueDecision

                // Unknown Material
                GET >=> routef  "/Unknown/%s"           (fun g n c -> U.UnknownGrains.getDetail g |> apiResult n c)
                GET >=> route   "/Unknown"              >=> api U.UnknownGrains.listUnknownGrains
                GET >=> route   "/Unknown/MostWanted"   >=> api U.UnknownGrains.getTopScoringUnknownGrains
                POST >=> route  "/Unknown/Submit"       >=> postAuthApi U.UnknownGrains.submitUnknownGrain
                POST >=> route  "/Unknown/Identify"     >=> postAuthApi U.UnknownGrains.identifyUnknownGrain

                // User's equipment
                GET >=> route   "/User/Microscope"      >=> apif U.Calibrations.getMyCalibrations
                POST >=> route  "/User/Microscope/Setup"        >=> postAuthApi U.Calibrations.setupMicroscope
                POST >=> route  "/User/Microscope/Calibrate"    >=> postApi U.Calibrations.calibrateMagnification

                // Digitise
                GET >=> route   "/Digitise/Collection"          >=> apiAuth U.Digitise.myCollections
                GET >=> routef  "/Digitise/Collection/%s"       (fun col n c -> col |> U.Digitise.getCollection |> apiResult n c)
                POST >=> route  "/Digitise/Collection/Start"    >=> postAuthApi U.Digitise.startNewCollection
                POST >=> routef "/Digitise/Collection/%s/Publish"   (fun col n c -> U.Digitise.publish (Auth.getCurrentUser c) col |> apiResult n c)
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
                opt.Authority <- U.getAppSetting "IdentityUrl" //this.Configuration.GetValue<string> "IdentityUrl"
                opt.RequireHttpsMetadata <- false
                opt.Audience <- "core" ) |> ignore
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
        app.UseAuthentication() |> ignore
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