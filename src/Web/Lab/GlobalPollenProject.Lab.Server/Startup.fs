namespace GlobalPollenProject.Lab.Server

open System
open System.IdentityModel.Tokens.Jwt
open System.Net.Http
open System.Net.Http.Headers
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Authentication.OpenIdConnect
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Bolero.Remoting.Server
open Bolero.Templating.Server
open Bolero.Server.RazorHost
open Microsoft.IdentityModel.Protocols.OpenIdConnect

///////////////////////////
/// Custom Authentication
///////////////////////////

type HttpClientAuthorizationDelegatingHandler(httpContextAccessor:IHttpContextAccessor) =
    inherit DelegatingHandler()
        
    member __.GetToken () : Task<string> =
        let accessToken = "access_token"
        httpContextAccessor.HttpContext.GetTokenAsync(accessToken)

    override this.SendAsync (request:HttpRequestMessage, cancellationToken:CancellationToken) = 
        let authorisationHeader = httpContextAccessor.HttpContext.Request.Headers.["Authorization"]
        if authorisationHeader.Count > 0 then request.Headers.Add("Authorization", authorisationHeader)
        let token = this.GetToken().Result // TODO Don't call 'Result' here
        if isNull token |> not then request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
        base.SendAsync(request, cancellationToken)

type Startup(configuration: IConfiguration) =

    /// Adds support for GPP custom Identity service
    member __.AddCustomAuthentication(services:IServiceCollection) =   
        let identityUrl = configuration.GetValue<string>("IdentityUrl")
        let callBackUrl = configuration.GetValue<string>("CallBackUrl")
        let authSecret = configuration.GetValue<string>("AuthSecret")
        printfn "Identity url is %s" identityUrl
        printfn "Callbk url is %s" callBackUrl
        printfn "authsec url is %s" authSecret
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
                opt.ClientId <- "lab-ui"
                opt.ClientSecret <- authSecret
                opt.ResponseType <- OpenIdConnectResponseType.CodeIdToken
                opt.SaveTokens <- true
                opt.GetClaimsFromUserInfoEndpoint <- true
                opt.RequireHttpsMetadata <- false
                opt.Scope.Add("openid")
                opt.Scope.Add("profile")
                opt.Scope.Add("webapigw")
                opt.Scope.Add("core"))
    
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddOptions()
            .Configure<Connections.AppSettings>(configuration) |> ignore
        services.AddMvc().AddRazorRuntimeCompilation() |> ignore
        services.AddServerSideBlazor() |> ignore
        services.AddAuthorization() |> ignore
        this.AddCustomAuthentication(services) |> ignore
        services
            .AddHttpContextAccessor()
            .AddTransient<HttpClientAuthorizationDelegatingHandler>()
            .AddHttpClient<Connections.CoreMicroservice>()
                .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
                .Services
            .AddRemoting<DigitiseService>()
            .AddBoleroHost()
#if DEBUG
            .AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../GlobalPollenProject.Lab.Client")
#endif
        |> ignore

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    member this.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()
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
                endpoints.MapFallbackToPage("/_Host") |> ignore)
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
