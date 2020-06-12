module Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Swashbuckle.AspNetCore.Swagger

let configureApp (app : IApplicationBuilder) =
    app.UseAuthentication() |> ignore
    app.UseMvcWithDefaultRoute() |> ignore
    app.UseSwaggerUI() |> ignore

let configureServices (services : IServiceCollection) =
    // services.AddHttpClient<CoreService>()
    //     .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Sample. Default lifetime is 2 minutes
    //     .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
    //     .AddPolicyHandler(GetRetryPolicy())
    //     .AddPolicyHandler(GetCircuitBreakerPolicy())
    services.AddAuthentication(fun opt -> opt.DefaultScheme <- "Cookie") |> ignore
    services.AddMvc() |> ignore
    services.AddSwaggerGen() |> ignore

    services.AddSwaggerGen(fun options ->
        options.DescribeAllEnumsAsStrings()
        options.SwaggerDoc("v1", Swashbuckle.AspNetCore.Swagger.Info
            ( Title = "Global Pollen Project: Data Access API",
              Version = "v1",
              Description = "Access the GPP dataset using this REST API",
              TermsOfService = "Terms Of Service" ))

        options.AddSecurityDefinition("oauth2", OAuth2Scheme
            ( Type = "oauth2",
              Flow = "implicit"
            // AuthorizationUrl = $"{configuration.GetValue<string>("IdentityUrlExternal")}/connect/authorize",
            //TokenUrl = $"{configuration.GetValue<string>("IdentityUrlExternal")}/connect/token",
            // Scopes = Dictionary<string, string>()
            // {
            //     { "webhooks", "Webhooks API" }
            // }
            ))

        // options.OperationFilter<AuthorizeCheckOperationFilter>()
    ) |> ignore


[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0