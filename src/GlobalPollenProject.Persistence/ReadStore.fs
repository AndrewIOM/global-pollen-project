namespace ReadStore

open GlobalPollenProject.Core.Types
open Microsoft.EntityFrameworkCore
open System
open System.ComponentModel.DataAnnotations

// Read Models
[<CLIMutable>]
type GrainSummary = {
    [<Key>]
    Id:Guid;
    Thumbnail:string }

// Entity Framework Context
type ReadContext =
    inherit DbContext
    
    new() = { inherit DbContext() }
    new(options: DbContextOptions<ReadContext>) = { inherit DbContext(options) }

    // Unknown Grains Index View
    [<DefaultValue>]
    val mutable grainSummaries:DbSet<GrainSummary>
    member x.GrainSummaries
        with get() = x.grainSummaries
        and set v = x.grainSummaries <- v

    override this.OnConfiguring optionsBuilder = 
        optionsBuilder.UseSqlite "Filename=./projections.db" |> ignore
        printfn "Starting ReadStore with Entity Framework"

    // override this.OnModelCreating modelBuilder =
    //     modelBuilder.Entity<GrainSummary>()
    //         .HasPrimaryKey(c => c.Id)
