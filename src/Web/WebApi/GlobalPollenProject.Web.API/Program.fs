module Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.OpenApi.Models
open Swashbuckle.AspNetCore.Swagger

let configureApp (app : IApplicationBuilder) =
    app.UseAuthentication() |> ignore
    app.UseRouting() |> ignore
    app.UseEndpoints(fun endpoints ->
                    endpoints.MapControllers() |> ignore
                    ) |> ignore
    app.UseSwaggerUI() |> ignore

let configureServices (services : IServiceCollection) =
    // services.AddHttpClient<CoreService>()
    //     .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Sample. Default lifetime is 2 minutes
    //     .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
    //     .AddPolicyHandler(GetRetryPolicy())
    //     .AddPolicyHandler(GetCircuitBreakerPolicy())
    services.AddAuthentication(fun opt -> opt.DefaultScheme <- "Cookie") |> ignore
    services.AddControllers() |> ignore
    services.AddSwaggerGen() |> ignore

    services.AddSwaggerGen(fun options ->
        options.SwaggerDoc("v1", OpenApiInfo
            ( Title = "Global Pollen Project: Data Access API",
              Version = "v1",
              Description = "Access the GPP dataset using this REST API",
              TermsOfService = null ))

        options.AddSecurityDefinition("oauth2", OpenApiSecurityScheme
            ( Type = SecuritySchemeType.OAuth2,
              Flows = OpenApiOAuthFlows() )) |> ignore
        ) |> ignore

            // AuthorizationUrl = $"{configuration.GetValue<string>("IdentityUrlExternal")}/connect/authorize",
            //TokenUrl = $"{configuration.GetValue<string>("IdentityUrlExternal")}/connect/token",
            // Scopes = Dictionary<string, string>()
            // {
            //     { "webhooks", "Webhooks API" }
            // }
//        Type = SecuritySchemeType.OAuth2,
//        Flows = new OpenApiOAuthFlows
//        {
//            Implicit = new OpenApiOAuthFlow
//            {
//                AuthorizationUrl = new Uri("/auth-server/connect/authorize", UriKind.Relative),
//                Scopes = new Dictionary<string, string>
//                {
//                    { "readAccess", "Access read operations" },
//                    { "writeAccess", "Access write operations" }
//                }
//            }

        // options.OperationFilter<AuthorizeCheckOperationFilter>()

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0