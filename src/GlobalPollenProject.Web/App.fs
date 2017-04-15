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

open GlobalPollenProject.App
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.Shared.Identity.Services
open GlobalPollenProject.Web.Models

(* Error Handling *)

let errorHandler (ex : Exception) (ctx : HttpHandlerContext) =
    ctx.Logger.LogError(EventId(0), ex, "An unhandled exception has occurred while executing the request.")
    ctx |> (clearResponse >=> setStatusCode 500 >=> text ex.Message)

(* App *)

let authScheme = "Identity.Application"

let accessDenied = setStatusCode 401 >=> text "Access Denied"

let mustBeUser = requiresAuthentication accessDenied

let mustBeAdmin = 
    requiresAuthentication accessDenied 
    >=> requiresRole "Admin" accessDenied

let loginHandler =
    fun ctx ->
        async {
            // let! model = bindForm<LoginRequest> ctx
            // let userManager = ctx.Services.GetRequiredService<UserManager<ApplicationUser>>()
            // let signInManager = ctx.Services.GetRequiredService<SignInManager<ApplicationUser>>()
            
            // let! result = Async.AwaitTask(signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false))            

            let issuer = "http://localhost:5000"
            let claims =
                [
                    Claim(ClaimTypes.Name,      "John",  ClaimValueTypes.String, issuer)
                    Claim(ClaimTypes.Surname,   "Doe",   ClaimValueTypes.String, issuer)
                    Claim(ClaimTypes.Role,      "Admin", ClaimValueTypes.String, issuer)
                ]
            let identity = ClaimsIdentity(claims, authScheme)
            let user     = ClaimsPrincipal(identity)
            do! ctx.HttpContext.Authentication.SignInAsync(authScheme, user) |> Async.AwaitTask
            
            return! text "Successfully logged in" ctx
        }

let userHandler =
    fun ctx ->
        text ctx.HttpContext.User.Identity.Name ctx

let importFromBackboneHandler =
    fun ctx ->
        let app = ctx.Services.GetRequiredService<AppServices>()
        app.Backbone.Import "/Users/andrewmartin/Documents/Global Pollen Project/Plant List Offline/taxa.txt"
        text "Import successful" ctx

let showUserHandler id =
    fun ctx ->
        mustBeAdmin >=>
        text (sprintf "User ID: %i" id)
        <| ctx

let backboneSearchHandler =
    fun ctx ->
        let name = ctx.HttpContext.Request.Query.["name"].ToString()
        let app = ctx.Services.GetRequiredService<AppServices>()
        let appResult = app.Backbone.Search name
        json appResult ctx

let taxonListHandler =
    fun ctx ->
        let name = ctx.HttpContext.Request.Query.["name"].ToString()
        let app = ctx.Services.GetRequiredService<AppServices>()
        let appResult = app.Taxonomy.List {Page = 1; PageSize = 20}
        json appResult ctx

let pagedTaxonomyHandler =
    fun ctx ->
        let app = ctx.Services.GetRequiredService<AppServices>()
        let appResult = app.Taxonomy.List {Page = 1; PageSize = 20}
        razorHtmlView "Taxon/Index" appResult ctx

let api =
    choose [
        route   "/backbone/search"    >=> backboneSearchHandler
      //route   "/backbone/import"    >=> importFromBackboneHandler
        route   "/taxa"               >=> taxonListHandler
    ]

let webApp = 
    choose [
        subRoute                "/api/v1"           api
        POST >=>    
            route               "/Account/Login"    >=> loginHandler
        GET >=> 
            route               "/"                 >=> razorHtmlView "Home/Index" None
            route               "/Guide"            >=> razorHtmlView "Home/Guide" None
            route               "/Digitise"         >=> razorHtmlView "Digitise/Index" None
            subRoute            "/Taxon"    
                (choose [   
                    route       ""                  >=> pagedTaxonomyHandler
                    routef      "/%s"               (fun (family) -> text family)
                    routef      "/%s/%s"            (fun (family,genus) -> text genus)
                    routef      "/%s/%s/%s"         (fun (family,genus,species) -> text species) ])
            route               "/Account/Login"    >=> razorHtmlView "Account/Login" None
            route               "/logout"           >=> signOff authScheme >=> text "Successfully logged out."
            route               "/user"             >=> mustBeUser >=> userHandler
            routef              "/user/%i"          showUserHandler
            
        setStatusCode 404 >=> razorHtmlView "NotFound" None ]


let configureApp (app : IApplicationBuilder) = 
    app.UseGiraffeErrorHandler(errorHandler)
    app.UseIdentity() |> ignore
    app.UseStaticFiles() |> ignore
    app.UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    let viewsFolderPath = Path.Combine(env.ContentRootPath, "views")

    services.AddSingleton<UserDbContext>() |> ignore
    services.AddIdentity<ApplicationUser, IdentityRole>()
        .AddEntityFrameworkStores<UserDbContext>()
        .AddDefaultTokenProviders() |> ignore

    services.AddSingleton<AppServices>(composeApp()) |> ignore
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