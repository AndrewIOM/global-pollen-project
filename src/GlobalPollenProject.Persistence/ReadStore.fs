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
    Name:string;
    Description:string;
    SlideCount:int;
}

[<CLIMutable>]
type PublicProfile = {
    [<Key>]
    UserId:Guid
    IsPublic:bool
    FirstName:string
    LastName:string
}

// Read Model Repositories
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

        // Unknown Grains Index View
        [<DefaultValue>] val mutable grainSummaries:DbSet<GrainSummary>
        [<DefaultValue>] val mutable taxonSummaries:DbSet<TaxonSummary>
        [<DefaultValue>] val mutable backboneTaxon:DbSet<BackboneTaxon>
        member x.GrainSummaries
            with get() = x.grainSummaries
            and set v = x.grainSummaries <- v

        member x.TaxonSummaries
            with get() = x.taxonSummaries
            and set v = x.taxonSummaries <- v

        member x.BackboneTaxa
            with get() = x.backboneTaxon
            and set v = x.backboneTaxon <- v

        override this.OnConfiguring optionsBuilder = 
            optionsBuilder.UseSqlite "Filename=./projections.db" |> ignore
            printfn "Starting ReadStore with Entity Framework"

    open System.Linq

    // Repositories
    let isNull x = match box x with null -> true | _ -> false
    let grainRepo (ctx:ReadContext) =
        let getById (guid:Guid) =
            let result = ctx.GrainSummaries.Find guid
            if not (isNull result) then Some result else None
        let list request =
            match request with
            | All -> ctx.GrainSummaries |> Seq.toList
            | Paged pr ->
                ctx.GrainSummaries.Skip(pr.Page - 1 * pr.ItemsPerPage).Take(pr.ItemsPerPage) |> Seq.toList
        {GetById = getById; List = list}
    
    let backboneRepo (ctx:ReadContext) =
        let getById (id:Guid) =
            let result = ctx.BackboneTaxa.Find id
            if not (isNull result) then Some result else None
        let list request =
            match request with
            | All -> ctx.BackboneTaxa |> Seq.toList
            | Paged pr ->
                ctx.BackboneTaxa.Skip(pr.Page - 1 * pr.ItemsPerPage).Take(pr.ItemsPerPage) |> Seq.toList

        let getTaxonByName latinName rank parent =
            let result = match rank with
                        | "Species" -> ctx.BackboneTaxa.FirstOrDefault(fun t -> t.LatinName = latinName && t.Rank = "Species" && t.Genus = parent)
                        | "Genus" -> ctx.BackboneTaxa.FirstOrDefault(fun t -> t.LatinName = latinName && t.Rank = "Genus" && t.Family = parent)
                        | "Family" -> ctx.BackboneTaxa.FirstOrDefault(fun t -> t.LatinName = latinName && t.Rank = "Family")
            if not (isNull result)
                then Some result
                else None

        {GetById = getById; List = list; GetTaxonByName = getTaxonByName}

        //     let taxonomicBackbone (query:BackboneQuery) : TaxonId option =
        //         match query with
        //         | ValidateById id -> 
        //             let t = getTaxon id
        //             match t with
        //             | Some t -> Some (TaxonId t.Id)
        //             | None -> None
        //         | Validate identity ->
        //             None // TODO implement
