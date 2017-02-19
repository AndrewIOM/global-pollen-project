namespace GlobalPollenProject.WebUI

open System
open System.Collections.Generic
open System.Linq
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open System.IO
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open GlobalPollenProject.Shared.Identity.Services

type Startup() =
    
    member this.ConfigureServices(services: IServiceCollection) =
        services.AddSingleton<UserDbContext>() |> ignore
        services.AddIdentity<ApplicationUser, IdentityRole>()
           .AddEntityFrameworkStores<UserDbContext>()
           .AddDefaultTokenProviders() |> ignore
        services.AddSingleton<IEmailSender, AuthEmailMessageSender>() |> ignore
        services.AddMvc() |> ignore

    member this.Configure(app: IApplicationBuilder, env: IHostingEnvironment, loggerFactory: ILoggerFactory) =
        loggerFactory.AddConsole() |> ignore
        loggerFactory.AddDebug() |> ignore

        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore
            else app.UseExceptionHandler("/Home/Error") |> ignore

        app.UseStaticFiles() |> ignore
        app.UseIdentity() |> ignore
        app.UseMvc(fun routes ->
            routes.MapRoute(
                name = "default",
                template = "{controller=Home}/{action=Index}/{id?}") |> ignore
            ) |> ignore
