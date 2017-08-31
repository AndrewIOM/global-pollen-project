module GlobalPollenProject.Web.App

open System
open System.IO
open System.Text
open System.Security.Claims
open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Giraffe.Middleware
open Giraffe.Razor.HttpHandlers
open Giraffe.Razor.Middleware

open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.Shared.Identity.Services
open GlobalPollenProject.App.UseCases
open ReadModels

open FunctionalModelState

/////////////////////////
/// Helpers
/////////////////////////

let errorHandler (ex : Exception) (logger : ILogger) (ctx : HttpContext) =
    logger.LogError(EventId(0), ex, "An unhandled exception has occurred while executing the request.")
    ctx |> (clearResponse >=> setStatusCode 500 >=> text ex.Message)

let authScheme = "Cookie"
let accessDenied = setStatusCode 401 >=> razorHtmlView "AccessDenied" None
let mustBeLoggedIn = requiresAuthentication accessDenied
let mustBeAdmin ctx = requiresRole "Admin" accessDenied ctx

let currentUserId (ctx:HttpContext) () =
    async {
        let manager = ctx.GetService<UserManager<ApplicationUser>>()
        let! user = manager.GetUserAsync(ctx.User) |> Async.AwaitTask
        return Guid.Parse user.Id
    } |> Async.RunSynchronously

let renderView name model =
    warbler (fun x -> razorHtmlView name model)

let toViewResult view ctx result =
    match result with
    | Ok model -> ctx |> renderView view model
    | Error e -> ctx |> (clearResponse >=> setStatusCode 500 >=> renderView "Error" None)

let notFoundResult ctx =
    ctx |> (clearResponse >=> setStatusCode 400 >=> renderView "NotFound" None)

let toApiResult ctx result =
    match result with
    | Ok list -> json list ctx
    | Error e -> 
        ctx |>
        (setStatusCode 400 >=>
            match e with
            | Validation valErrors -> json <| { Message = "Invalid request"; Errors = valErrors }
            | InvalidRequestFormat -> json <| { Message = "Your request was not in a valid format"; Errors = [] }
            | _ -> json <| { Message = "Internal error"; Errors = [] } )

let bindJson<'a> (ctx:HttpContext) =
    try ctx.BindJson<'a>() |> Async.RunSynchronously |> Ok
    with
    | _ -> Error InvalidRequestFormat

let jsonRequestToApiResponse<'a> appService ctx =
    bindJson<'a> ctx
    |> bind validateModel
    |> Result.bind appService
    |> toApiResult ctx

let queryRequestToApiResponse<'a,'b> (appService:'a->Result<'b,ServiceError>) (ctx:HttpContext) =
    ctx.BindQueryString<'a>()
    |> validateModel
    |> Result.bind appService
    |> toApiResult ctx

/////////////////////////
/// Custom HTTP Handlers
/////////////////////////

let loginHandler redirectUrl =
    fun (ctx: HttpContext) ->
        async {
            let! loginRequest = ctx.BindForm<LoginRequest>()
            
            let isValid,errors = validateModel' loginRequest
            match isValid with
            | false -> return! ctx |> razorHtmlViewWithModelState "Account/Login" errors loginRequest
            | true ->
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! result = signInManager.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, loginRequest.RememberMe, lockoutOnFailure = false) |> Async.AwaitTask
                if result.Succeeded then
                   let logger = ctx.GetLogger()
                   logger.LogInformation "User logged in."
                   return! redirectTo false redirectUrl ctx
                else
                   return! renderView "Account/Login" loginRequest ctx
        }

let registerHandler (ctx:HttpContext) =
    async {
        let! model = ctx.BindForm<NewAppUserRequest>()
        let userManager = ctx.GetService<UserManager<ApplicationUser>>()
        let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
        let user = ApplicationUser(UserName = model.Email, Email = model.Email)
        let! result = userManager.CreateAsync(user, model.Password) |> Async.AwaitTask
        match result.Succeeded with
        | true ->
            let id() = Guid.Parse user.Id
            let register = User.register model id
            match register with
            | Error msg -> return! renderView "Account/Register" model ctx
            | Ok r -> 
                do! signInManager.SignInAsync(user, isPersistent = false) |> Async.AwaitTask
                (ctx.GetLogger()).LogInformation "User created a new account with password."
                return! redirectTo true "/" ctx
        | false -> 
            return! renderView "Account/Register" model ctx
    }

let slideViewHandler (id:string) ctx =
    let split = id.Split '/'
    match split.Length with
    | 2 -> 
        let col,slide = split.[0],split.[1]
        Taxonomy.getSlide col slide
        |> toViewResult "MRC/Slide" ctx
    | _ -> notFoundResult ctx

let taxonDetail (taxon:string) ctx =
    let (f,g,s) =
        let split = taxon.Split '/'
        match split.Length with
        | 1 -> split.[0],None,None
        | 2 -> split.[0],Some split.[1],None
        | 3 -> split.[0],Some split.[1],Some split.[2]
        | _ -> "",None,None
    Taxonomy.getByName f g s
    |> toViewResult "MRC/Taxon" ctx

let individualCollectionIndex ctx =
    IndividualReference.list {Page = 1; PageSize = 20}
    |> toViewResult "Reference/Index" ctx

let individualCollection (colId:string) version ctx =
    IndividualReference.getDetail colId version
    |> toViewResult "Reference/View" ctx

let pagedTaxonomyHandler ctx =
    Taxonomy.list {Page = 1; PageSize = 20}
    |> toViewResult "MRC/Index" ctx

let listCollectionsHandler ctx =
    Digitise.myCollections (currentUserId ctx)
    |> toApiResult ctx

let startCollectionHandler (ctx:HttpContext) =
    bindJson<StartCollectionRequest> ctx
    |> bind validateModel
    |> Result.bind (Digitise.startNewCollection (currentUserId ctx))
    |> toApiResult ctx

let publishCollectionHandler (ctx:HttpContext) =
    let id = ctx.BindQueryString<IdQuery>().Id
    Digitise.publish (currentUserId ctx) id
    text "Ok" ctx

let addSlideHandler (ctx:HttpContext) =
    bindJson<SlideRecordRequest> ctx
    |> bind validateModel
    |> bind Digitise.addSlideRecord
    |> toApiResult ctx

let addImageHandler (ctx:HttpContext) =
    bindJson<SlideImageRequest> ctx
    |> bind validateModel
    |> Result.bind Digitise.uploadSlideImage
    |> toApiResult ctx

let getCollectionHandler (ctx:HttpContext) =
    ctx.BindQueryString<IdQuery>().Id.ToString()
    |> Digitise.getCollection
    |> toApiResult ctx
    
let getCalibrationsHandler (ctx:HttpContext) =
    Calibrations.getMyCalibrations (currentUserId ctx)
    |> toApiResult ctx

let setupMicroscopeHandler (ctx:HttpContext) =
    bindJson<AddMicroscopeRequest> ctx
    |> bind (Calibrations.setupMicroscope (currentUserId ctx))
    |> toApiResult ctx

let calibrateHandler (ctx:HttpContext) =
    bindJson<CalibrateRequest> ctx
    |> bind Calibrations.calibrateMagnification
    |> toApiResult ctx

let listGrains ctx =
    UnknownGrains.listUnknownGrains()
    |> toViewResult "Identify/Index" ctx

let showGrainDetail id ctx =
    UnknownGrains.getDetail id
    |> toViewResult "Identify/View" ctx

let submitGrainHandler (ctx:HttpContext) =
    bindJson<AddUnknownGrainRequest> ctx
    >>= UnknownGrains.submitUnknownGrain (currentUserId ctx)
    |> toApiResult ctx

let submitIdentificationHandler (ctx:HttpContext) =
    bindJson<IdentifyGrainRequest> ctx
    |> bind (UnknownGrains.identifyUnknownGrain (currentUserId ctx))
    |> toApiResult ctx

/////////////////////////
/// Routes
/////////////////////////

let webApp =
    let publicApi =
        GET >=>
        choose [
            // route   "/backbone/match"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryMatch
            route   "/backbone/trace"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryTrace
            route   "/backbone/search"          >=> queryRequestToApiResponse<BackboneSearchRequest,string list> Backbone.searchNames
        ]

    let digitiseApi =
        mustBeLoggedIn >=>
        choose [
            route   "/collection"               >=> getCollectionHandler
            route   "/collection/list"          >=> listCollectionsHandler
            route   "/collection/start"         >=> startCollectionHandler
            route   "/collection/publish"       >=> publishCollectionHandler
            route   "/collection/slide/add"     >=> addSlideHandler
            route   "/collection/slide/addimage">=> addImageHandler
            route   "/calibration/list"         >=> getCalibrationsHandler
            route   "/calibration/use"          >=> setupMicroscopeHandler
            route   "/calibration/use/mag"      >=> calibrateHandler
        ]

    let accountManagement =
        choose [
            POST >=> route  "/Login"            >=> loginHandler "/"
            POST >=> route  "/Register"         >=> registerHandler
            POST >=> route  "/Logout"           >=> signOff authScheme >=> redirectTo true "/"
            GET  >=> route  "/Register"         >=> renderView "Account/Register" None
            GET  >=> route  "/Login"            >=> renderView "Account/Login" None
        ]

    let masterReferenceCollection =
        GET >=> 
        choose [   
            route   ""                          >=> pagedTaxonomyHandler
            routef  "/Slide/%s"                 slideViewHandler
            routef  "/%s"                       taxonDetail
        ]

    let individualRefCollections =
        GET >=>
        choose [
            route ""                            >=> individualCollectionIndex
            routef "/%s/%i"                     (fun (id,v) -> individualCollection id v)
        ]

    let identify =
        choose [
            POST >=> route  "/Upload"           >=> submitGrainHandler
            POST >=> route  "/Identify"         >=> submitIdentificationHandler
            GET  >=> route  ""                  >=> listGrains
            GET  >=> route  "/Upload"           >=> renderView "Identify/Add" None
            GET  >=> routef "/%s"               (fun id -> showGrainDetail id)
        ]

    // Main router
    choose [
        subRoute    "/api/v1"                   publicApi
        subRoute    "/api/v1/digitise"          digitiseApi
        subRoute    "/Account"                  accountManagement
        subRoute    "/Taxon"                    masterReferenceCollection
        subRoute    "/Reference"                individualRefCollections
        subRoute    "/Identify"                 identify
        GET >=> 
        choose [
            route   "/"                         >=> renderView "Home/Index" None
            route   "/Guide"                    >=> renderView "Home/Guide" None
            route   "/Api"                      >=> renderView "Home/Api" None
            route   "/Statistics"               >=> renderView "Statistics/Index" None
            route   "/Digitise"                 >=> mustBeLoggedIn >=> renderView "Digitise/Index" None
        ]
        setStatusCode 404 >=> renderView "NotFound" None 
    ]


/////////////////////////
/// Configuration
/////////////////////////

let configureApp (app : IApplicationBuilder) = 
    app.UseGiraffeErrorHandler(errorHandler)
    app.UseIdentity() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    let viewsFolderPath = Path.Combine(env.ContentRootPath, "Views")

    services.AddSingleton<UserDbContext>() |> ignore
    services.AddIdentity<ApplicationUser, IdentityRole>(fun opt -> 
        opt.Cookies.ApplicationCookie.LoginPath <- PathString "/Account/Login"
        opt.Cookies.ApplicationCookie.AuthenticationScheme <- "Cookie"
        opt.Cookies.ApplicationCookie.AutomaticAuthenticate <- true)
        .AddEntityFrameworkStores<UserDbContext>()
        .AddDefaultTokenProviders() |> ignore

    services.AddSingleton<IEmailSender, AuthEmailMessageSender>() |> ignore

    services.AddAuthentication() |> ignore
    services.AddDataProtection() |> ignore
    services.AddRazorEngine(viewsFolderPath) |> ignore

let configureLogging (loggerFactory : ILoggerFactory) =
    loggerFactory.AddConsole(LogLevel.Trace).AddDebug() |> ignore


[<EntryPoint>]
let main argv =
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(Action<IServiceCollection> configureServices)
        .ConfigureLogging(Action<ILoggerFactory> configureLogging)
        .Build()
        .Run()
    0