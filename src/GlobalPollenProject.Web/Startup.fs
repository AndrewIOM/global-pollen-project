module Startup

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
open Microsoft.Extensions.Configuration

open Giraffe.Middleware
open Giraffe.Razor.HttpHandlers
open Giraffe.Razor.Middleware

open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.Shared.Identity.Services
open GlobalPollenProject.App.UseCases
open GlobalPollenProject.Web.App

open ReadModels

type Startup () =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    member this.ConfigureServices(services: IServiceCollection) =
        let sp  = services.BuildServiceProvider()
        let env = sp.GetService<IHostingEnvironment>()
        let viewsFolderPath = Path.Combine(env.ContentRootPath, "Views")

        services.AddSingleton<UserDbContext>() |> ignore
        services.AddIdentity<ApplicationUser, IdentityRole>(fun opt -> 
            opt.SignIn.RequireConfirmedEmail <- true)
            .AddEntityFrameworkStores<UserDbContext>()
            .AddDefaultTokenProviders() |> ignore

        services.ConfigureApplicationCookie(fun opt ->
            opt.LoginPath <- PathString "/Account/Login" ) |> ignore

        services.AddAuthentication(fun opt ->
            opt.DefaultScheme <- "Cookie")
            .AddFacebook(fun opt ->
                opt.AppId <- getAppSetting "Authentication:Facebook:AppId"
                opt.AppSecret <- getAppSetting "Authentication:Facebook:AppSecret")
            .AddTwitter(fun opt ->
                opt.ConsumerKey <- getAppSetting "Authentication:Twitter:ConsumerKey"
                opt.ConsumerSecret <- getAppSetting "Authentication:Twitter:ConsumerSecret")
            |> ignore

        services.AddSingleton<IEmailSender, AuthEmailMessageSender>() |> ignore
        services.AddAuthentication() |> ignore
        services.AddDataProtection() |> ignore
        services.AddRazorEngine(viewsFolderPath) |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =

        if (env.IsDevelopment()) then
            app.UseDeveloperExceptionPage() |> ignore
        else
            app.UseGiraffeErrorHandler(Handlers.errorHandler)

        app.UseAuthentication() |> ignore
        app.UseStaticFiles() |> ignore
        app.UseGiraffe(GlobalPollenProject.Web.App.webApp)

    member val Configuration : IConfiguration = null with get, set

let configureLogging (loggerFactory : ILoggerFactory) =
    loggerFactory.AddConsole(LogLevel.Trace).AddDebug() |> ignore
