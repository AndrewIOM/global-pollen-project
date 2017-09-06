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
        .UseStartup<Startup.Startup>()
        .Build()

[<EntryPoint>]
let main args =
    BuildWebHost(args).Run()

    exitCode
