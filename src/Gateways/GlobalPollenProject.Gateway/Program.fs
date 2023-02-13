namespace GlobalPollenProject.Gateway

open System.IO
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Ocelot.DependencyInjection
open Ocelot.Middleware
open Microsoft.IdentityModel.Tokens

module Program =

    let authProviderKey = "IdentityApiKey"

    let CreateHostBuilder args =
        WebHost
            .CreateDefaultBuilder(args)
            .UseKestrel(fun opt ->
                opt.Limits.MaxRequestBodySize <- System.Nullable<int64>(int64 (1024 * 1024 * 100)))
            .UseContentRoot(Directory.GetCurrentDirectory())
            .ConfigureAppConfiguration(fun w config ->
                config
                   .SetBasePath(w.HostingEnvironment.ContentRootPath)
                   .AddJsonFile("ocelot.json")
                   .AddEnvironmentVariables() |> ignore )
            .ConfigureServices(fun w s -> 
                let identityUrl = w.Configuration.GetValue "IdentityUrl"
                s.AddAuthentication()
                    .AddJwtBearer(authProviderKey, fun opts ->
                        opts.Authority <- identityUrl
                        opts.RequireHttpsMetadata <- false
                        opts.TokenValidationParameters <- 
                            let t = TokenValidationParameters()
                            t.ValidAudiences <- [ "core" ]
                            t ) |> ignore
                s.AddOcelot w.Configuration |> ignore)
            //.ConfigureLogging(fun hostingContext logging -> () )
            .UseIISIntegration()
            .Configure(fun app -> app.UseOcelot().Wait())

    [<EntryPoint>]
    let main args =
        CreateHostBuilder(args).Build().Run()
        0