module Startup

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Configuration

open Giraffe.Middleware
open Giraffe.Razor.Middleware

open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.Shared.Identity.Services
open GlobalPollenProject.App.UseCases

open ReadModels

///////////////////////////
/// DI components for views
///////////////////////////

let parseGuid i =
    match System.Guid.TryParse i with
    | (true,g) -> Ok g
    | (false,g) -> Error InvalidRequestFormat

let resultToOption r =
    match r with
    | Ok o -> Some o
    | Error _ -> None

let getPublicProfile userId =
    userId
    |> parseGuid
    |> bind User.getPublicProfile
    |> resultToOption

type IProfileLoader =
  abstract Get : string -> PublicProfile option

type ProfileLoader() =
    interface IProfileLoader with
        member __.Get id = getPublicProfile id

///////////////////////////
/// App Configuration
///////////////////////////

let createRoles (serviceProvider:IServiceProvider) =
    let roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>()
    let userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>()
    
    [ "Admin"; "Curator" ]
    |> List.iter (fun roleName ->
        match roleManager.RoleExistsAsync(roleName) |> Async.AwaitTask |> Async.RunSynchronously with
        | true -> ()
        | false -> roleManager.CreateAsync(IdentityRole(roleName)) 
                   |> Async.AwaitTask 
                   |> Async.RunSynchronously 
                   |> ignore )

    let powerUser = ApplicationUser(UserName = getAppSetting "UserSettings:UserEmail",Email = getAppSetting "UserSettings:UserEmail" )

    let powerPassword = getAppSetting "UserSettings:UserPassword"
    let existing = userManager.FindByEmailAsync(getAppSetting "UserSettings:UserEmail") |> Async.AwaitTask |> Async.RunSynchronously
    if existing |> isNull 
        then
            let create = userManager.CreateAsync(powerUser, powerPassword) |> Async.AwaitTask |> Async.RunSynchronously
            if create.Succeeded 
                then userManager.AddToRoleAsync(powerUser, "Admin") |> Async.AwaitTask |> Async.RunSynchronously |> ignore
                else ()
        else ()


type Startup () =
    new (configuration: IConfiguration) as this =
        Startup() then
        this.Configuration <- configuration

    member __.ConfigureServices(services: IServiceCollection) =
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
                opt.ConsumerSecret <- getAppSetting "Authentication:Twitter:ConsumerSecret"
                opt.RetrieveUserDetails <- true) |> ignore
            // .AddJwtBearer(fun cfg ->
            //     cfg.RequireHttpsMetadata <- false
            //     cfg.SaveToken <- true
            //     cfg.TokenValidationParameters <- new TokenValidationParameters(
            //         ValidIssuer = Configuration["Tokens:Issuer"],
            //         ValidAudience = Configuration["Tokens:Issuer"],
            //         IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Tokens:Key"]))
            //     )) |> ignore

        services.AddSingleton<IEmailSender, AuthEmailMessageSender>() |> ignore
        services.AddSingleton<IProfileLoader, ProfileLoader>() |> ignore
        services.AddDataProtection() |> ignore
        services.AddRazorEngine(viewsFolderPath) |> ignore
        createRoles (services.BuildServiceProvider()) |> ignore

    member __.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =

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
