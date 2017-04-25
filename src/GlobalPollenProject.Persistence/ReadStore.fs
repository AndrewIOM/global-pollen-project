namespace ReadStore

open GlobalPollenProject.Core.Types
open Microsoft.EntityFrameworkCore
open System
open System.ComponentModel.DataAnnotations
open System.Collections.Generic

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
type BackboneTaxon = {
    [<Key>]
    Id:Guid;
    Family:string
    Genus:string
    Species:string
    NamedBy:string
    LatinName:string
    Rank:string
    ReferenceName:string
    ReferenceUrl:string
}

[<CLIMutable>]
type ReferenceCollectionSummary = {
    [<Key>]
    Id:Guid;
    User:Guid;
    Name:string;
    Description:string;
    SlideCount:int;
}

[<CLIMutable>]
type Calibration = {
    [<Key>]
    Id: Guid
    User: Guid
    Device: string
    Ocular: int
    Objective: int
    Image: string
    PixelWidth: float
}

[<CLIMutable>]
type Frame = {
    [<Key>]
    Id: Guid
    Url: string
}

[<CLIMutable>]
type SlideImage = {
    [<Key>]
    Id: int
    Frames: List<Frame>
    CalibrationImageUrl: string
    CalibrationFocusLevel: int
    PixelWidth: float
}

[<CLIMutable>]
type Slide = {
    [<Key>]
    Id:Guid
    CollectionId: Guid
    CollectionSlideId: string
    Taxon: TaxonSummary
    IdentificationMethod: string
    FamilyOriginal: string
    GenusOriginal: string
    SpeciesOriginal: string
    IsFullyDigitised: bool
    Images: List<SlideImage>
}

[<CLIMutable>]
type ReferenceCollection = {
    [<Key>]
    Id:Guid;
    User:Guid;
    Name:string;
    Status:string; // Draft etc.
    Version: int;
    Description:string;
    Slides: List<Slide>
}

[<CLIMutable>]
type SlideSummary = {
    [<Key>]
    Id: Guid
    ThumbnailUrl: string
    TaxonId: Guid
}

[<CLIMutable>]
type PublicProfile = {
    [<Key>]
    UserId:Guid
    IsPublic:bool
    FirstName:string
    LastName:string
}

type ListRequest =
| All 
| Paged of PagedRequest

and PagedRequest = {
    ItemsPerPage: int
    Page: int
}

type ProjectionRepository<'TProjection> = {
    GetById: Guid -> 'TProjection option
    List: ListRequest -> 'TProjection list
    Replace: 'TProjection -> unit
}

type BackboneRepository = {
    GetById: Guid -> BackboneTaxon option
    List: ListRequest -> BackboneTaxon list
    GetTaxonByName: string -> string -> string -> BackboneTaxon option
}

module EntityFramework =

    // EF Context
    type ReadContext =
        inherit DbContext
        
        new() = { inherit DbContext() }
        new(options: DbContextOptions<ReadContext>) = { inherit DbContext(options) }

        [<DefaultValue>] val mutable grainSummary:DbSet<GrainSummary>
        [<DefaultValue>] val mutable taxonSummary:DbSet<TaxonSummary>
        [<DefaultValue>] val mutable backboneTaxon:DbSet<BackboneTaxon>
        [<DefaultValue>] val mutable referenceCollectionSummary:DbSet<ReferenceCollectionSummary>
        [<DefaultValue>] val mutable referenceCollection:DbSet<ReferenceCollection>
        [<DefaultValue>] val mutable calibration:DbSet<Calibration>
        member x.GrainSummary
            with get() = x.grainSummary
            and set v = x.grainSummary <- v

        member x.TaxonSummary
            with get() = x.taxonSummary
            and set v = x.taxonSummary <- v

        member x.BackboneTaxon
            with get() = x.backboneTaxon
            and set v = x.backboneTaxon <- v

        member x.ReferenceCollectionSummary
            with get() = x.referenceCollectionSummary
            and set v = x.referenceCollectionSummary <- v

        member x.ReferenceCollection
            with get() = x.referenceCollection
            and set v = x.referenceCollection <- v

        member x.Calibration
            with get() = x.calibration
            and set v = x.calibration <- v
        override this.OnConfiguring optionsBuilder = 
            optionsBuilder.UseSqlite "Filename=./projections.db" |> ignore
            //optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) |> ignore
            printfn "Starting ReadStore with Entity Framework"

        // override this.OnModelCreating modelBuilder =
        //     modelBuilder.Entity<ReferenceCollection>().HasMany<Slide>(fun c -> c.Slides).WithOne() |> ignore
        //     modelBuilder.Entity<Slide>().HasMany<SlideImage>(fun s -> s.Images) |> ignore
        //     modelBuilder.Entity<SlideImage>().HasMany<Frame>(fun i -> i.Frames) |> ignore

    open System.Linq

    // Repositories
    let isNull x = match box x with null -> true | _ -> false
    let grainRepo (ctx:ReadContext) =
        let getById (guid:Guid) =
            let result = ctx.GrainSummary.Find guid
            if not (isNull result) then Some result else None
        let list request =
            match request with
            | All -> ctx.GrainSummary |> Seq.toList
            | Paged pr ->
                ctx.GrainSummary.Skip(pr.Page - 1 * pr.ItemsPerPage).Take(pr.ItemsPerPage) |> Seq.toList
        {GetById = getById; List = list; Replace = fun x -> invalidOp "Not implemented yet" }
    
    let backboneRepo (ctx:ReadContext) =
        let getById (id:Guid) =
            let result = ctx.BackboneTaxon.Find id
            if not (isNull result) then Some result else None
        let list request =
            match request with
            | All -> ctx.BackboneTaxon |> Seq.toList
            | Paged pr ->
                ctx.BackboneTaxon.Skip(pr.Page - 1 * pr.ItemsPerPage).Take(pr.ItemsPerPage) |> Seq.toList

        let getTaxonByName latinName rank parent =
            let result = match rank with
                         | "Species" -> ctx.BackboneTaxon.FirstOrDefault(fun t -> t.LatinName = latinName && t.Rank = "Species" && t.Genus = parent)
                         | "Genus" -> ctx.BackboneTaxon.FirstOrDefault(fun t -> t.LatinName = latinName && t.Rank = "Genus" && t.Family = parent)
                         | "Family" -> ctx.BackboneTaxon.FirstOrDefault(fun t -> t.LatinName = latinName && t.Rank = "Family")
                         | _ -> invalidOp "Not a valid taxonomic rank"
            if not (isNull result)
                then Some result
                else None

        {GetById = getById; List = list; GetTaxonByName = getTaxonByName}

        // let referenceRepo (ctx:ReadContext) =
            
        //     let getById (id:Guid) =
        //         ctx .ReferenceCollections
        //             .Include(fun x -> x.Slides)
        //             .ThenInclude(fun y -> y.Images)
        //             .ThenInclude(fun z -> z.Frames)
        //         |> Seq.tryFind (fun col -> col.Id = id)

        //     let list (req:ListRequest) = 
        //         let all = ctx   .ReferenceCollections
        //                         .Include(fun x -> x.Slides)
        //                         .ThenInclude(fun y -> y.Images)
        //                         .ThenInclude(fun z -> z.Frames)
        //         match req with
        //         | All -> all |> Seq.toList
        //         | Paged req ->

                

        //     let taxonomicBackbone (query:BackboneQuery) : TaxonId option =
        //         match query with
        //         | ValidateById id -> 
        //             let t = getTaxon id
        //             match t with
        //             | Some t -> Some (TaxonId t.Id)
        //             | None -> None
        //         | Validate identity ->
        //             None // TODO implement
