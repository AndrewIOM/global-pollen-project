namespace GlobalPollenProject.Persistence.Migrations

open System
open Microsoft.EntityFrameworkCore
open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Metadata
open Microsoft.EntityFrameworkCore.Migrations
open Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
open Microsoft.EntityFrameworkCore.Migrations.Operations

open ReadStore

type GrainSummaryTable = 
    {Id:OperationBuilder<AddColumnOperation>;Thumbnail:OperationBuilder<AddColumnOperation>}

// dotnet ef --startup-project ../GlobalPollenProject.WebUI  migrations add Initial -c SqlEventStoreContext
// dotnet ef --startup-project ../GlobalPollenProject.WebUI database update

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
                      Thumbnail = table.Column<string>(nullable = true) }),
            constraints = 
                fun table -> 
                    table.PrimaryKey("PK_GrainSummaries", (fun x -> x.Id :> obj))|> ignore ) |> ignore
        

    override this.Down(migrationBuilder: MigrationBuilder) = 
        migrationBuilder.DropTable(name = "GrainSummaries") |> ignore

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

[<DbContext(typeof<SqlEventStore.SqlEventStoreContext>)>]
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
