module Startup

open System
open System.Globalization
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
open System.IdentityModel.Tokens.Jwt
open Microsoft.Extensions.Diagnostics.HealthChecks
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.HttpOverrides
open Newtonsoft.Json

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
        if token |> isNotNull then request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
        base.SendAsync(request, cancellationToken)

type HttpClientRequestIdDelegatingHandler() =
    inherit DelegatingHandler()

    override __.SendAsync (request:HttpRequestMessage, cancellationToken:CancellationToken) = 
        if request.Method = HttpMethod.Post || request.Method = HttpMethod.Put
        then 
            if request.Headers.Contains "x-requestid" |> not
            then request.Headers.Add("x-requestid", Guid.NewGuid().ToString())
        base.SendAsync(request, cancellationToken)


///////////////////////////
/// App Configuration
///////////////////////////

type Startup (env: IWebHostEnvironment, configuration: IConfiguration) =

    member __.AddCustomAuthentication(services:IServiceCollection) =   
        let identityUrl = configuration.GetValue<string>("IdentityUrl")
        let callBackUrl = configuration.GetValue<string>("CallBackUrl")
        let authSecret = configuration.GetValue<string>("AuthSecret")
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
                opt.ClientId <- "mvc"
                opt.ClientSecret <- authSecret
                opt.ResponseType <- "code id_token"
                opt.SaveTokens <- true
                opt.GetClaimsFromUserInfoEndpoint <- true
                opt.RequireHttpsMetadata <- not (env.IsDevelopment())
                opt.Scope.Add("openid")
                opt.Scope.Add("profile")
                opt.Scope.Add("webapigw")
                opt.Scope.Add("core"))
    
    member __.AddHttpClientServices(services:IServiceCollection) =
        services.AddHttpContextAccessor() |> ignore
        services.AddTransient<HttpClientAuthorizationDelegatingHandler>() |> ignore
        services.AddTransient<HttpClientRequestIdDelegatingHandler>() |> ignore
        services.AddHttpClient<Connections.CoreMicroservice>()
            .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>() |> ignore
            //.AddHttpMessageHandler<HttpClientRequestIdDelegatingHandler>() |> ignore

    member __.AddHealthChecks(services:IServiceCollection) =
        services.AddHealthChecks()
            .AddCheck("self", fun () -> HealthCheckResult.Healthy())

    member this.ConfigureServices(services: IServiceCollection) =
        services
            .AddOptions()
            .Configure<Connections.AppSettings>(configuration)
            .ConfigureApplicationCookie(fun opt ->
                opt.LoginPath <- PathString "/Account/Login" )
            .AddDataProtection() |> ignore
        services.AddMvcCore().AddDataAnnotations() |> ignore
        services.AddGiraffe() |> ignore
        this.AddHttpClientServices(services) |> ignore
        this.AddCustomAuthentication(services) |> ignore
        this.AddHealthChecks(services) |> ignore
        let customSettings = JsonSerializerSettings(Culture = CultureInfo("en-GB"),
                                                    ContractResolver = Serialization.CamelCasePropertyNamesContractResolver())
        customSettings.Converters.Add(Microsoft.FSharpLu.Json.CompactUnionJsonConverter(true))
        services.AddSingleton<Json.ISerializer>(NewtonsoftJson.Serializer(customSettings)) |> ignore
    
    member __.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()
        app.UseHealthChecks(PathString "/health") |> ignore
        if (env.IsDevelopment()) 
        then 
            app.UseDeveloperExceptionPage() |> ignore
            app.UseForwardedHeaders(ForwardedHeadersOptions(ForwardedHeaders = ForwardedHeaders.XForwardedFor)) |> ignore
        else 
            app.UseGiraffeErrorHandler(Handlers.errorHandler) |> ignore
            app.UseForwardedHeaders(ForwardedHeadersOptions(ForwardedHeaders = ForwardedHeaders.XForwardedFor)) |> ignore
            app.UseHsts() |> ignore
            app.UseHttpsRedirection() |> ignore
        app.UseStaticFiles() |> ignore
        app.UseAuthentication() |> ignore
        app.UseGiraffe(GlobalPollenProject.Web.App.webApp)
