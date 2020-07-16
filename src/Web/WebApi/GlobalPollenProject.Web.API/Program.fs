module Program

open System
open System.IdentityModel.Tokens.Jwt
open System.Net.Http
open System.Net.Http.Headers
open System.Threading
open System.Threading.Tasks
open Microsoft.AspNetCore.HttpOverrides
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.OpenApi.Models

type HttpClientAuthorizationDelegatingHandler(httpContextAccessor:IHttpContextAccessor) =
    inherit DelegatingHandler()
        
    member __.GetToken () : Task<string> =
        let accessToken = "access_token"
        httpContextAccessor.HttpContext.GetTokenAsync(accessToken)

    override this.SendAsync (request:HttpRequestMessage, cancellationToken:CancellationToken) = 
        let authorisationHeader = httpContextAccessor.HttpContext.Request.Headers.["Authorization"]
        if authorisationHeader.Count > 0 then request.Headers.Add("Authorization", authorisationHeader)
        let token = this.GetToken().Result
        if String.IsNullOrEmpty token |> not
        then request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", token)
        base.SendAsync(request, cancellationToken)

type Startup (configuration: IConfiguration) =

    member __.AddCustomAuthentication(services:IServiceCollection) =   
        let identityUrl = configuration.GetValue<string>("IdentityUrl")
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddIdentityServerAuthentication(JwtBearerDefaults.AuthenticationScheme, fun opt ->
                opt.Authority <- identityUrl
                opt.ApiName <- "public_api" )

    member __.AddApiDocumentation(services:IServiceCollection) =
        services.AddSwaggerGen(fun options ->
            options.SwaggerDoc("v1", OpenApiInfo
                ( Title = "Global Pollen Project: Data Access API",
                  Version = "v1",
                  Description = "Access the GPP dataset using this REST API",
                  TermsOfService = null ))
            options.AddSecurityDefinition("oauth2", OpenApiSecurityScheme
                ( Type = SecuritySchemeType.OAuth2,
                  Flows = OpenApiOAuthFlows() )) |> ignore )

    member this.ConfigureServices(services: IServiceCollection) =
        services.AddOptions()
            .Configure<Connections.AppSettings>(configuration)
            .AddDataProtection() |> ignore
        services.AddControllers() |> ignore
        services.AddHttpContextAccessor() |> ignore
        services.AddHttpClient<Connections.CoreMicroservice>()
            .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>() |> ignore
        this.AddCustomAuthentication(services) |> ignore
        this.AddApiDocumentation(services) |> ignore
    
    member __.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear()
        if (env.IsDevelopment()) 
        then app.UseDeveloperExceptionPage() |> ignore
        else 
            app.UseHsts() |> ignore
            app.UseHttpsRedirection() |> ignore
        app.UseForwardedHeaders(ForwardedHeadersOptions(ForwardedHeaders = ForwardedHeaders.XForwardedFor)) |> ignore
        app.UseStaticFiles() |> ignore
        app.UseRouting() |> ignore
        app.UseAuthentication() |> ignore
        app.UseAuthorization() |> ignore
        app.UseEndpoints(fun endpoints ->
                        endpoints.MapControllers() |> ignore
                        ) |> ignore
        app.UseSwaggerUI() |> ignore

let BuildWebHost args =
    WebHost
        .CreateDefaultBuilder(args)
        .UseKestrel(fun opt ->
            opt.Limits.MaxRequestBodySize <- Nullable<int64>(int64 (1024 * 1024 * 10)))
        .UseStartup<Startup>()
        .Build()

[<EntryPoint>]
let main args =
    BuildWebHost(args).Run()
    0
