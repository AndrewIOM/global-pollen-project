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
open Giraffe.HttpHandlers
open Giraffe.Middleware
open Giraffe.ModelBinding
open Newtonsoft.Json

open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.Shared.Identity.Services

open GlobalPollenProject.App.UseCases

(* Error Handling *)

let errorHandler (ex : Exception) (ctx : HttpHandlerContext) =
    ctx.Logger.LogError(EventId(0), ex, "An unhandled exception has occurred while executing the request.")
    ctx |> (clearResponse >=> setStatusCode 500 >=> text ex.Message)

(* App *)

let authScheme = "Cookie"

let accessDenied = setStatusCode 401 >=> text "Access Denied"
let currentUserId ctx () =
    async {
        let manager = ctx.Services.GetRequiredService<UserManager<ApplicationUser>>()
        let! user = manager.GetUserAsync(ctx.HttpContext.User) |> Async.AwaitTask
        return Guid.Parse user.Id
    } |> Async.RunSynchronously

let mustBeUser = requiresAuthentication accessDenied
let mustBeLoggedIn = requiresAuthentication (challenge authScheme)

let mustBeAdmin = 
    requiresAuthentication accessDenied 
    >=> requiresRole "Admin" accessDenied

let loginHandler =
    fun ctx ->
        async {
            let! loginRequest = bindForm<LoginRequest> ctx
            let userManager = ctx.Services.GetRequiredService<UserManager<ApplicationUser>>()
            let signInManager = ctx.Services.GetRequiredService<SignInManager<ApplicationUser>>()
            
            let! result = signInManager.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, loginRequest.RememberMe, lockoutOnFailure = false) |> Async.AwaitTask
            if result.Succeeded then
               ctx.Logger.LogInformation "User logged in."
               ctx.HttpContext.Response.Redirect "/"
               return! text "Couldn't log in" ctx
           else
               return! razorHtmlView "/Account/Login" loginRequest ctx
        }

let registerHandler ctx =
    async {
        let! model = bindForm<NewAppUserRequest> ctx
        let userManager = ctx.Services.GetRequiredService<UserManager<ApplicationUser>>()
        let signInManager = ctx.Services.GetRequiredService<SignInManager<ApplicationUser>>()
        let user = ApplicationUser(UserName = model.Email, Email = model.Email)
        let! result = userManager.CreateAsync(user, model.Password) |> Async.AwaitTask
        match result.Succeeded with
        | true ->
            let id() = Guid.Parse user.Id
            let register = User.register model id
            match register with
            | Error msg -> return! razorHtmlView "/Account/Register" model ctx
            | Ok r -> 
                do! signInManager.SignInAsync(user, isPersistent = false) |> Async.AwaitTask
                ctx.Logger.LogInformation "User created a new account with password."
                // Redirect to another route here...
                return! text "Account successfully registered" ctx
        | false -> 
            return! razorHtmlView "/Account/Register" model ctx
    }

let userHandler =
    fun ctx ->
        text ctx.HttpContext.User.Identity.Name ctx

// let importFromBackboneHandler =
//     fun ctx ->
//         Backbone.importAll "/Users/andrewmartin/Documents/Global Pollen Project/Plant List Offline/taxa.txt"
//         text "Import successful" ctx

let showUserHandler id =
    fun ctx ->
        mustBeAdmin >=>
        text (sprintf "User ID: %i" id)
        <| ctx

let showTaxonHandler family genus species =
    fun ctx ->
        let appResult = Taxonomy.getByName family genus species
        match appResult with
        | Ok t -> razorHtmlView "Taxon/View" t ctx
        | Error e -> text "Not found" ctx

let backboneSearchHandler =
    fun ctx ->
        async {
            let! request = bindQueryString<BackboneSearchRequest> ctx
            let appResult = Backbone.search request
            return! json appResult ctx
        }

let taxonListHandler ctx =
    let name = ctx.HttpContext.Request.Query.["name"].ToString()
    let appResult = Taxonomy.list {Page = 1; PageSize = 20}
    json appResult ctx

let pagedTaxonomyHandler ctx =
    let appResult = Taxonomy.list {Page = 1; PageSize = 20}
    razorHtmlView "Taxon/Index" appResult ctx

let listCollectionsHandler ctx =
    let result = Digitise.myCollections (currentUserId ctx)
    match result with
    | Ok clist -> json clist ctx
    | Error error -> text "Error" ctx

let startCollectionHandler ctx =
    async {
        let! model = bindJson<StartCollectionRequest> ctx
        let result = Digitise.startNewCollection model (currentUserId ctx)
        match result with
        | Ok id -> return! json id ctx
        | Error error -> return! text "Error" ctx
    }

let addSlideHandler ctx =
    async {
        let! model = bindJson<SlideRecordRequest> ctx
        let result = Digitise.addSlideRecord model
        match result with
        | Ok id -> return! json id ctx
        | Error error -> return! text "Error" ctx
    }

[<CLIMutable>] type IdQuery = { Id: Guid }

let getCollectionHandler ctx =
    async {
        let! id = bindQueryString<IdQuery> ctx
        let result = Digitise.getCollection (id.Id.ToString())
        match result with
        | Ok rc -> return! json rc ctx
        | Error error -> return! text "Error" ctx
    }

let listGrains ctx =
    let appResult = UnknownGrains.listUnknownGrains
    match appResult with
    | Ok model -> razorHtmlView "" model ctx
    | Error e -> text "Error" ctx

let showGrainDetail id =
    fun ctx -> 
        let appResult = UnknownGrains.getDetail id
        match appResult with
        | Ok model -> razorHtmlView "Grain/Identify" model ctx
        | Error e -> text "error" ctx

let grainUploadHandler ctx =
    async {
        let! req = bindForm<AddUnknownGrainRequest> ctx
        let result = UnknownGrains.submitUnknownGrain req (currentUserId ctx)
        match result with
        | Ok id -> return! json id ctx
        | Error error -> return! text "Error" ctx
    }

let api =
    choose [
        route   "/backbone/search"        >=> backboneSearchHandler
        // route   "/backbone/import"        >=> importFromBackboneHandler
        route   "/taxa"                   >=> taxonListHandler

        route   "/collection"             >=> mustBeUser >=> getCollectionHandler
        route   "/collection/list"        >=> mustBeUser >=> listCollectionsHandler
        route   "/collection/start"       >=> mustBeUser >=> startCollectionHandler
        route   "/collection/slide/add"   >=> mustBeUser >=> addSlideHandler
    ]

let webApp = 
    choose [
        subRoute                "/api/v1"           api
        POST >=>
            choose [
                route               "/Account/Login"    >=> loginHandler
                route               "/Account/Register" >=> registerHandler
                route               "/Identify/Upload"  >=> grainUploadHandler
            ]    
        GET >=> 
            choose [
                route               "/"                 >=> razorHtmlView "Home/Index" None
                route               "/Guide"            >=> razorHtmlView "Home/Guide" None
                route               "/Digitise"         >=> razorHtmlView "Digitise/Index" None
                route               "/Identify"         >=> listGrains
                route               "/Identify/Upload"  >=> razorHtmlView "Grain/Add" None
                //route               "/Identify/%s"      (fun id -> showGrainDetail id)
                subRoute            "/Taxon"    
                    (choose [   
                        route       ""                  >=> pagedTaxonomyHandler
                        routef      "/%s/%s/%s"         (fun (family,genus,species) -> showTaxonHandler family genus species)
                        routef      "/%s/%s"            (fun (family,genus) -> showTaxonHandler family genus None)
                        routef      "/%s"               (fun family -> showTaxonHandler family None None) ])
                route               "/Account/Login"    >=> razorHtmlView "Account/Login" None
                route               "/Account/Register" >=> razorHtmlView "Account/Register" None
                route               "/Account/Logoff"   >=> signOff authScheme >=> text "Logged Out"
                route               "/user"             >=> mustBeUser >=> userHandler
                routef              "/user/%i"          showUserHandler
            ]
        setStatusCode 404 >=> razorHtmlView "NotFound" None ]

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