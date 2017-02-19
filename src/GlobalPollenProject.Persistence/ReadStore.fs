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

[<CLIMutable>]
type TaxonSummary = {
    [<Key>]
    Id:Guid;
    Family:string
    Genus:string
    Species:string
    LatinName:string
    Rank:string
    SlideCount:int
    GrainCount:int
    ThumbnailUrl:string
}

[<CLIMutable>]
type PublicProfile = {
    [<Key>]
    UserId:Guid
    IsPublic:bool
    FirstName:string
    LastName:string
}

// Entity Framework Context
type ReadContext =
    inherit DbContext
    
    new() = { inherit DbContext() }
    new(options: DbContextOptions<ReadContext>) = { inherit DbContext(options) }

    // Unknown Grains Index View
    [<DefaultValue>]
    val mutable grainSummaries:DbSet<GrainSummary>
    [<DefaultValue>]
    val mutable taxonSummaries:DbSet<TaxonSummary>
    member x.GrainSummaries
        with get() = x.grainSummaries
        and set v = x.grainSummaries <- v

    member x.TaxonSummaries
        with get() = x.taxonSummaries
        and set v = x.taxonSummaries <- v

    override this.OnConfiguring optionsBuilder = 
        optionsBuilder.UseSqlite "Filename=./projections.db" |> ignore
        printfn "Starting ReadStore with Entity Framework"

    // override this.OnModelCreating modelBuilder =
    //     modelBuilder.Entity<GrainSummary>()
    //         .HasPrimaryKey(c => c.Id)
