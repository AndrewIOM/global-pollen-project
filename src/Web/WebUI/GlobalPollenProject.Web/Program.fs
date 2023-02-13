module Program

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging

let exitCode = 0

let BuildWebHost args =
    WebHost
        .CreateDefaultBuilder(args)
        .UseKestrel(fun opt ->
            opt.Limits.MaxRequestBodySize <- System.Nullable<int64>(int64 (1024 * 1024 * 100)))
        .UseStartup<Startup.Startup>()
        .Build()

[<EntryPoint>]
let main args =
    BuildWebHost(args).Run()

    exitCode
