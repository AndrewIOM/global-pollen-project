module Program

open System
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Routes
open Microsoft.AspNetCore.Http
open System.Net.Http

// Temporary: Connect to core + identity service

type CoreService() =

    member this.Cool = "Cool"


type HttpClientAuthorizationDelegatingHandler(httpContextAccesor: IHttpContextAccessor) =

    inherit DelegatingHandler()
        member this.Cool = "Cool"


let configureApp (app : IApplicationBuilder) =
    app.UseAuthentication() |> ignore
    app.UseGiraffe webApp

let configureServices (services : IServiceCollection) =
    services.AddHttpClient<CoreService>()
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Sample. Default lifetime is 2 minutes
        .AddHttpMessageHandler<HttpClientAuthorizationDelegatingHandler>()
        .AddPolicyHandler(GetRetryPolicy())
        .AddPolicyHandler(GetCircuitBreakerPolicy())
    services.AddAuthentication(fun opt -> opt.DefaultScheme <- "Cookie") |> ignore
    services.AddGiraffe() |> ignore

[<EntryPoint>]
let main _ =
    WebHostBuilder()
        .UseKestrel()
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0