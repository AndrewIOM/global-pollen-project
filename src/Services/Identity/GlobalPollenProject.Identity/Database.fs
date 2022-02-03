namespace GlobalPollenProject.Identity

open System.IO
open Microsoft.EntityFrameworkCore
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Identity.EntityFrameworkCore

type UserDbContext () =
    inherit IdentityDbContext<ApplicationUser>()
    with

        override __.OnConfiguring(optionsBuilder:DbContextOptionsBuilder) =
            let config = 
                ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json")
                    .AddEnvironmentVariables()
                    .Build()
            optionsBuilder.UseSqlServer(config.GetConnectionString("UserConnection")) |> ignore

        override __.OnModelCreating(builder:ModelBuilder) =
            base.OnModelCreating(builder)