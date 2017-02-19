namespace GlobalPollenProject.WebUI

open System
open System.IO
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Hosting
open GlobalPollenProject.WebUI

module Program =

    [<EntryPoint>]
    let main argv =
        let host =
                WebHostBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseIISIntegration()
                    .UseStartup<Startup>()
                    .Build();

        host.Run()
        0