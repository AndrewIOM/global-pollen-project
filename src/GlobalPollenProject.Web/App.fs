module GlobalPollenProject.Web.App

open System
open System.IO
open System.Security.Claims
open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe.HttpHandlers
open Giraffe.Middleware

open GlobalPollenProject.Web.Models
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open GlobalPollenProject.Shared.Identity.Services

open GlobalPollenProject.App

let errorHandler (ex : Exception) (ctx : HttpHandlerContext) =
    ctx.Logger.LogError(EventId(0), ex, "An unhandled exception has occurred while executing the request.")
    ctx |> (clearResponse >=> setStatusCode 500 >=> text ex.Message)

let authScheme = "Cookie"

let accessDenied = setStatusCode 401 >=> text "Access Denied"

let mustBeUser = requiresAuthentication accessDenied

let mustBeAdmin = 
    requiresAuthentication accessDenied 
    >=> requiresRole "Admin" accessDenied

let loginHandler =
    fun ctx ->
        async {
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
        BackboneAppService.CsvImport.importAll "/Users/andrewmartin/Documents/Global Pollen Project/Plant List Offline/taxa.txt"
        text ctx.HttpContext.User.Identity.Name ctx

let showUserHandler id =
    fun ctx ->
        mustBeAdmin >=>
        text (sprintf "User ID: %i" id)
        <| ctx

let webApp = 
    choose [
        GET >=>
            choose [
                route  "/"           >=> text "index"
                route  "/ping"       >=> importFromBackboneHandler >=> text "pong"
                route  "/error"      >=> (fun _ -> failwith "Something went wrong!")
                route  "/login"      >=> loginHandler
                route  "/logout"     >=> signOff authScheme >=> text "Successfully logged out."
                route  "/user"       >=> mustBeUser >=> userHandler
                routef "/user/%i"    showUserHandler
                route  "/razor"      >=> razorHtmlView "Home/Index" { Name = "Razor" }
            ]
        setStatusCode 404 >=> text "Not Found" ]


let configureApp (app : IApplicationBuilder) = 
    app.UseGiraffeErrorHandler(errorHandler)
    app.UseIdentity() |> ignore
    app.UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    let viewsFolderPath = Path.Combine(env.ContentRootPath, "views")

    services.AddSingleton<UserDbContext>() |> ignore
    services.AddIdentity<ApplicationUser, IdentityRole>()
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