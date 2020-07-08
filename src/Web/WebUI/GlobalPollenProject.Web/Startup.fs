module Startup

open System
open System.Globalization
open Giraffe.Serialization
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
open Microsoft.Extensions.Hosting
open Microsoft.AspNetCore.HttpOverrides

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
                opt.RequireHttpsMetadata <- false
                opt.Scope.Add("openid")
                opt.Scope.Add("profile")
                opt.Scope.Add("webapigw")
                opt.Scope.Add("core"))
    
    member __.AddHttpClientServices(services:IServiceCollection) =
        services.AddHttpContextAccessor() |> ignore
        services.AddTransient<HttpClientAuthorizationDelegatingHandler>() |> ignore
        services.AddTransient<HttpClientRequestIdDelegatingHandler>() |> ignore
        services.AddHttpClient<Connections.CoreMicroservice>()
            //.SetHandlerLifetime(TimeSpan.FromMinutes(2.))
            .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>() |> ignore
            //.AddHttpMessageHandler<HttpClientRequestIdDelegatingHandler>() |> ignore
            //.AddPolicyHandler(GetRetryPolicy())
            //.AddPolicyHandler(GetCircuitBreakerPolicy()) |> ignore

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
        services.AddMvcCore().AddDataAnnotations() |> ignore // Adds IValidationContext
        services.AddGiraffe() |> ignore
        this.AddHttpClientServices(services) |> ignore
        this.AddCustomAuthentication(services) |> ignore
        this.AddHealthChecks(services) |> ignore
        let customSettings = Newtonsoft.Json.JsonSerializerSettings(Culture = CultureInfo("en-GB"))
        customSettings.Converters.Add(Microsoft.FSharpLu.Json.CompactUnionJsonConverter(true))
        services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer(customSettings)) |> ignore
    
    member __.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()
        app.UseHealthChecks(PathString "/health") |> ignore
        if (env.IsDevelopment()) 
        then app.UseDeveloperExceptionPage() |> ignore
        else 
            app.UseGiraffeErrorHandler(Handlers.errorHandler) |> ignore
            app.UseHsts() |> ignore
            app.UseHttpsRedirection() |> ignore
        app.UseForwardedHeaders(ForwardedHeadersOptions(ForwardedHeaders = ForwardedHeaders.XForwardedFor)) |> ignore
        app.UseStaticFiles() |> ignore
        app.UseAuthentication() |> ignore
        app.UseGiraffe(GlobalPollenProject.Web.App.webApp)
