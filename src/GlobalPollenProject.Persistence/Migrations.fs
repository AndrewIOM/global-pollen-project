namespace GlobalPollenProject.Persistence.Migrations

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Metadata
open Microsoft.EntityFrameworkCore.Migrations
open Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
open Microsoft.EntityFrameworkCore.Migrations.Operations

open ReadStore

// dotnet ef --startup-project ../GlobalPollenProject.WebUI  migrations add Initial -c SqlEventStoreContext
// dotnet ef --startup-project ../GlobalPollenProject.WebUI database update

type GrainSummaryTable = {
    Id:OperationBuilder<AddColumnOperation>;
    Id2:OperationBuilder<AddColumnOperation>; // Temporary fix for conflicting DUs
    Thumbnail:OperationBuilder<AddColumnOperation> }

type TaxonSummaryTable = {
    Id: OperationBuilder<AddColumnOperation>
    Family: OperationBuilder<AddColumnOperation>
    Genus: OperationBuilder<AddColumnOperation>
    GrainCount: OperationBuilder<AddColumnOperation>
    LatinName: OperationBuilder<AddColumnOperation>
    Rank: OperationBuilder<AddColumnOperation>
    SlideCount: OperationBuilder<AddColumnOperation>
    Species: OperationBuilder<AddColumnOperation>
    ThumbnailUrl: OperationBuilder<AddColumnOperation> }

type BackboneTaxonTable = {
    Id: OperationBuilder<AddColumnOperation>
    Family: OperationBuilder<AddColumnOperation>
    Genus: OperationBuilder<AddColumnOperation>
    Species: OperationBuilder<AddColumnOperation>
    LatinName: OperationBuilder<AddColumnOperation>
    Rank: OperationBuilder<AddColumnOperation>
    ReferenceName: OperationBuilder<AddColumnOperation>
    ReferenceUrl: OperationBuilder<AddColumnOperation> }

// Migrations
[<DbContext(typeof<ReadContext>)>]
[<Migration("20170105185538_Initial")>]
type Init() =
    inherit Migration()
    
    override this.Up(migrationBuilder: MigrationBuilder) =
        migrationBuilder.CreateTable(
            name = "GrainSummaries",
            columns = 
                (fun table -> 
                    { Id = table.Column<Guid>(nullable = false)
                      Id2 = table.Column<Guid>(nullable = true)
                      Thumbnail = table.Column<string>(nullable = true) }),
            constraints = 
                fun table -> 
                    table.PrimaryKey("PK_GrainSummaries", (fun x -> x.Id2 :> obj))|> ignore ) |> ignore
        
        migrationBuilder.CreateTable(
            name = "TaxonSummaries",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      Family = table.Column<string>(nullable = true)
                      Genus = table.Column<string>(nullable = true)
                      GrainCount = table.Column<int>(nullable = false)
                      LatinName = table.Column<string>(nullable = true)
                      Rank = table.Column<string>(nullable = true)
                      SlideCount = table.Column<int>(nullable = false)
                      Species = table.Column<string>(nullable = true)
                      ThumbnailUrl = table.Column<string>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_TaxonSummaries", (fun x -> x.Id :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "BackboneTaxa",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      Family = table.Column<string>(nullable = true)
                      Genus = table.Column<string>(nullable = true)
                      Species = table.Column<string>(nullable = true)
                      LatinName = table.Column<string>(nullable = true)
                      Rank = table.Column<string>(nullable = true)
                      ReferenceName = table.Column<int>(nullable = false)
                      ReferenceUrl = table.Column<string>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_BackboneTaxa", (fun x -> x.Id :> obj)) |> ignore ) |> ignore


    override this.Down(migrationBuilder: MigrationBuilder) = 
        migrationBuilder.DropTable(name = "GrainSummaries") |> ignore
        migrationBuilder.DropTable(name = "TaxonSummaries") |> ignore
        migrationBuilder.DropTable(name = "BackboneTaxa") |> ignore

    override this.BuildTargetModel(modelBuilder: ModelBuilder) =
        modelBuilder
            .HasAnnotation("ProductVersion", "1.0.1")
            |> ignore

        modelBuilder.Entity("ReadStore.GrainSummary", 
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("Thumbnail") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("GrainSummaries") |> ignore)|> ignore

        modelBuilder.Entity("ReadStore.TaxonSummary",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("Family") |> ignore
                b.Property<string>("Genus") |> ignore
                b.Property<int>("GrainCount") |> ignore
                b.Property<string>("LatinName") |> ignore
                b.Property<string>("Rank") |> ignore
                b.Property<int>("SlideCount") |> ignore
                b.Property<string>("Species") |> ignore
                b.Property<string>("ThumbnailUrl") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("TaxonSummaries") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.BackboneTaxon",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("Family") |> ignore
                b.Property<string>("Genus") |> ignore
                b.Property<string>("LatinName") |> ignore
                b.Property<string>("Rank") |> ignore
                b.Property<string>("Species") |> ignore
                b.Property<string>("Reference") |> ignore
                b.Property<string>("ReferenceUrl") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("BackboneTaxa") |> ignore) |> ignore

open EventStore

type EventInfoTable =
    {
        Id: OperationBuilder<AddColumnOperation>
        StreamId: OperationBuilder<AddColumnOperation>
        StreamVersion: OperationBuilder<AddColumnOperation>
        OccurredAt: OperationBuilder<AddColumnOperation>
        EventType: OperationBuilder<AddColumnOperation>
        EventPayload: OperationBuilder<AddColumnOperation>
    }

[<DbContext(typeof<SqlEventStoreContext>)>]
[<Migration("20170106144439_Initial")>]
type InitEventStore() =
    inherit Migration()

    override this.Up(migrationBuilder: MigrationBuilder) =
        migrationBuilder.CreateTable(
            name = "Events",
            columns = 
                (fun table -> 
                    { 
                        Id = table.Column<Guid>(nullable = false)
                        StreamId = table.Column<string>(nullable = true)
                        StreamVersion = table.Column<int>(nullable = false)
                        EventPayload = table.Column<byte[]>(nullable = true)
                        EventType = table.Column<string>(nullable = true)
                        OccurredAt = table.Column<DateTime>(nullable = false)
                    }
                ),
            constraints = 
                fun table -> 
                    table.PrimaryKey("PK_Events", (fun x -> x.Id :> obj))|> ignore ) |> ignore

    override this.Down(migrationBuilder: MigrationBuilder) = 
        migrationBuilder.DropTable(name = "Events") |> ignore

    override this.BuildTargetModel(modelBuilder: ModelBuilder) =
        modelBuilder
            .HasAnnotation("ProductVersion", "1.0.1")
            |> ignore

        modelBuilder.Entity("EventStore+SqlEventStore+EventInfo", 
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<byte[]>("EventPayload") |> ignore
                b.Property<string>("EventType") |> ignore
                b.Property<DateTime>("OccurredAt") |> ignore
                b.Property<string>("StreamId") |> ignore
                b.Property<int>("StreamVersion") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("Events") |> ignore) |> ignore
