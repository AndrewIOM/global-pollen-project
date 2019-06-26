module Startup

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open System.Net.Http
open System.Threading
open Microsoft.AspNetCore.Authentication
open System.Threading.Tasks
open System.Net.Http.Headers
open Polly
open Polly.Extensions.Http
open System.IdentityModel.Tokens.Jwt
open Microsoft.Extensions.Diagnostics.HealthChecks
open Account


///////////////////////////
/// Custom Authentication Infrastructure
///////////////////////////

type HttpClientAuthorizationDelegatingHandler(httpContextAccesor:IHttpContextAccessor) =
    inherit DelegatingHandler()

    member __.GetToken () : Task<string> =
        let accessToken = "access_token"
        httpContextAccesor.HttpContext.GetTokenAsync(accessToken)

    member this.SendAsync (request:HttpRequestMessage) (cancellationToken:CancellationToken) = 
        let authorisationHeader = httpContextAccesor.HttpContext.Request.Headers.["Authorization"]
        if authorisationHeader.Count > 0 then request.Headers.Add("Authorization", authorisationHeader)
        let token = this.GetToken().Result // TODO Don't call 'Result' here
        if token |> isNotNull then request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
        base.SendAsync(request, cancellationToken)

type HttpClientRequestIdDelegatingHandler() =
    inherit DelegatingHandler()

    member __.SendAsync (request:HttpRequestMessage) (cancellationToken:CancellationToken) = 
        if request.Method = HttpMethod.Post || request.Method = HttpMethod.Put
        then 
            if request.Headers.Contains "x-requestid" 
            then request.Headers.Add("x-requestid", Guid.NewGuid().ToString())
        base.SendAsync(request, cancellationToken)

///////////////////////////
/// App Configuration
///////////////////////////


///////////////////////////
/// Startup: Configuration
///////////////////////////

type Startup (configuration: IConfiguration) =

    member __.AddCustomAuthentication(services:IServiceCollection) =   
        let useLoadTest = configuration.GetValue<bool>("UseLoadTest")
        let identityUrl = configuration.GetValue<string>("IdentityUrl")
        let callBackUrl = configuration.GetValue<string>("CallBackUrl")
        printfn "Callback URL for OpenID is %s" callBackUrl
        let sessionCookieLifetime = configuration.GetValue("SessionCookieLifetimeMinutes", 60.)
        services
            .AddAuthentication(fun opt ->
                opt.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                opt.DefaultChallengeScheme <- OpenIdConnectDefaults.AuthenticationScheme)
            .AddCookie(fun setup -> setup.ExpireTimeSpan <- TimeSpan.FromMinutes(sessionCookieLifetime))
            .AddOpenIdConnect(fun opt ->
                opt.SignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                opt.Authority <- identityUrl.ToString()
                opt.SignedOutRedirectUri <- callBackUrl.ToString()
                opt.ClientId <- if useLoadTest then "mvctest" else "mvc"
                opt.ClientSecret <- "secret"
                opt.ResponseType <- if useLoadTest then "code id_token token" else "code id_token"
                opt.SaveTokens <- true
                opt.GetClaimsFromUserInfoEndpoint <- true
                opt.RequireHttpsMetadata <- false
                opt.Scope.Add("openid")
                opt.Scope.Add("profile")
                opt.Scope.Add("webapigw")
                opt.Scope.Add("core"))

    member __.AddHttpClientServices(services:IServiceCollection) =
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>() |> ignore
        services.AddTransient<HttpClientAuthorizationDelegatingHandler>() |> ignore
        services.AddTransient<HttpClientRequestIdDelegatingHandler>() |> ignore
        services.AddHttpClient<Connections.CoreMicroservice>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(2.))
            .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>() |> ignore
            // .AddPolicyHandler(GetRetryPolicy())
            // .AddPolicyHandler(GetCircuitBreakerPolicy()) |> ignore
        services.AddHttpClient<Connections.AuthenticationService>()
            .SetHandlerLifetime(TimeSpan.FromMinutes(2.))
            .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>() |> ignore
            // .AddPolicyHandler(GetRetryPolicy())
            // .AddPolicyHandler(GetCircuitBreakerPolicy()) |> ignore
        services.AddTransient<IdentityParser>()

    member __.AddHealthChecks(services:IServiceCollection) =
        services.AddHealthChecks()
            .AddCheck("self", fun () -> HealthCheckResult.Healthy())
            // .AddUrlGroup(new Uri(configuration["PurchaseUrlHC"]), name = "purchaseapigw-check", tags = [ "purchaseapigw" ])
            // .AddUrlGroup(new Uri(configuration["MarketingUrlHC"]), name: "marketingapigw-check", tags: new string[] { "marketingapigw" })
            // .AddUrlGroup(new Uri(configuration["IdentityUrlHC"]), name: "identityapi-check", tags: new string[] { "identityapi" });                

    member this.ConfigureServices(services: IServiceCollection) =
        services
            .AddOptions()
            .Configure<Connections.AppSettings>(configuration)
            .ConfigureApplicationCookie(fun opt ->
                opt.LoginPath <- PathString "/Account/Login" )
            .AddDataProtection() |> ignore
        // services.AddSession() |> ignore
        services.AddGiraffe() |> ignore
        this.AddHttpClientServices(services) |> ignore
        this.AddCustomAuthentication(services) |> ignore
        //this.AddHealthChecks(services) |> ignore

    member __.Configure(app: IApplicationBuilder, env: IHostingEnvironment) =

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()

        // app.UseHealthChecks(PathString "/health") |> ignore

        if (env.IsDevelopment()) 
        then app.UseDeveloperExceptionPage() |> ignore
        else 
            app.UseGiraffeErrorHandler(Handlers.errorHandler) |> ignore
            app.UseHsts() |> ignore

        app.UseStaticFiles() |> ignore
        // app.UseSession() |> ignore
        app.UseAuthentication() |> ignore
        // app.UseHttpsRedirection() |> ignore
        app.UseGiraffe(GlobalPollenProject.Web.App.webApp)


// let createRoles (serviceProvider:IServiceProvider) =
//     let roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>()
//     let userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>()

//     [ "Admin"; "Curator" ]
//     |> List.iter (fun roleName ->
//         match roleManager.RoleExistsAsync(roleName) |> Async.AwaitTask |> Async.RunSynchronously with
//         | true -> ()
//         | false -> roleManager.CreateAsync(IdentityRole(roleName)) 
//                    |> Async.AwaitTask 
//                    |> Async.RunSynchronously 
//                    |> ignore )

//     let powerUser = ApplicationUser(UserName = getAppSetting "UserSettings:UserEmail",Email = getAppSetting "UserSettings:UserEmail" )

//     let powerPassword = getAppSetting "UserSettings:UserPassword"
//     let existing = userManager.FindByEmailAsync(getAppSetting "UserSettings:UserEmail") |> Async.AwaitTask |> Async.RunSynchronously
//     if existing |> isNull 
//         then
//             let create = userManager.CreateAsync(powerUser, powerPassword) |> Async.AwaitTask |> Async.RunSynchronously
//             if create.Succeeded 
//                 then userManager.AddToRoleAsync(powerUser, "Admin") |> Async.AwaitTask |> Async.RunSynchronously |> ignore
//                 else ()
//         else ()


        // createRoles (services.BuildServiceProvider()) |> ignore

        // services.AddSingleton<UserDbContext>() |> ignore
        // services.AddIdentity<ApplicationUser, IdentityRole>(fun opt -> 
        //     opt.SignIn.RequireConfirmedEmail <- true)
        //     .AddEntityFrameworkStores<UserDbContext>()
        //     .AddDefaultTokenProviders() |> ignore

        // services.AddAuthentication(fun opt ->
        //     opt.DefaultScheme <- "Cookie")
        //     .AddFacebook(fun opt ->
        //         opt.AppId <- getAppSetting "Authentication:Facebook:AppId"
        //         opt.AppSecret <- getAppSetting "Authentication:Facebook:AppSecret")
        //     .AddTwitter(fun opt ->
        //         opt.ConsumerKey <- getAppSetting "Authentication:Twitter:ConsumerKey"
        //         opt.ConsumerSecret <- getAppSetting "Authentication:Twitter:ConsumerSecret"
        //         opt.RetrieveUserDetails <- true) |> ignore
