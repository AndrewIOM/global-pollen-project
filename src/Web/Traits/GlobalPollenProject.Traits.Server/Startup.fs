namespace GlobalPollenProject.Traits.Server

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Bolero.Remoting.Server
open Bolero.Server
open Bolero.Templating.Server

type Startup (configuration: IConfiguration) =

    member __.AddCustomAuthentication(services:IServiceCollection) =   
        let identityUrl = configuration.GetValue<string>("IdentityUrl")
        let callBackUrl = configuration.GetValue<string>("CallBackUrl")
        let authSecret = configuration.GetValue<string>("AuthSecret")
        let sessionCookieLifetime = configuration.GetValue("SessionCookieLifetimeMinutes", 60.)
        services
            .AddAuthentication(fun opt ->
                opt.DefaultScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                opt.DefaultChallengeScheme <- OpenIdConnectDefaults.AuthenticationScheme)
            .AddCookie(fun setup -> setup.ExpireTimeSpan <- System.TimeSpan.FromMinutes(sessionCookieLifetime))
            .AddOpenIdConnect(fun opt ->
                opt.SignInScheme <- CookieAuthenticationDefaults.AuthenticationScheme
                opt.Authority <- identityUrl.ToString()
                opt.SignedOutRedirectUri <- callBackUrl.ToString()
                opt.ClientId <- "mvc"
                opt.ClientSecret <- authSecret
                opt.ResponseType <- "code id_token"
                opt.SaveTokens <- true
                opt.GetClaimsFromUserInfoEndpoint <- true
                opt.RequireHttpsMetadata <- true // TODO turn off in development
                opt.Scope.Add("openid")
                opt.Scope.Add("profile")
                opt.Scope.Add("webapigw")
                opt.Scope.Add("core"))
    
    member __.AddHttpClientServices(services:IServiceCollection) =
        services.AddHttpContextAccessor() |> ignore
        services.AddTransient<GlobalPollenProject.Shared.Auth.HttpClientAuthorizationDelegatingHandler>() |> ignore
        services.AddTransient<GlobalPollenProject.Shared.Auth.HttpClientRequestIdDelegatingHandler>() |> ignore
        services.AddHttpClient<Connections.CoreMicroservice>()
            .AddHttpMessageHandler<GlobalPollenProject.Shared.Auth.HttpClientAuthorizationDelegatingHandler>() |> ignore

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        services
            .AddOptions()
            .Configure<Connections.AppSettings>(configuration)
            .ConfigureApplicationCookie(fun opt ->
                opt.LoginPath <- PathString "/Account/Login" )
            .AddDataProtection() |> ignore
        services.AddMvc() |> ignore
        services.AddServerSideBlazor() |> ignore
        this.AddHttpClientServices(services) |> ignore
        this.AddCustomAuthentication(services) |> ignore
        services
            .AddBoleroRemoting<TraitService>()
            .AddBoleroHost()
#if DEBUG
            .AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../GlobalPollenProject.Traits.Client")
#endif
        |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        app
            .UseAuthentication()
            .UseRemoting()
            .UseStaticFiles()
            .UseRouting()
            .UseBlazorFrameworkFiles()
            .UseEndpoints(fun endpoints ->
#if DEBUG
                endpoints.UseHotReload()
#endif
                endpoints.MapBlazorHub() |> ignore
                endpoints.MapFallbackToBolero(Index.page) |> ignore)
        |> ignore

module Program =

    [<EntryPoint>]
    let main args =
        WebHost
            .CreateDefaultBuilder(args)
            .UseStaticWebAssets()
            .UseStartup<Startup>()
            .Build()
            .Run()
        0
