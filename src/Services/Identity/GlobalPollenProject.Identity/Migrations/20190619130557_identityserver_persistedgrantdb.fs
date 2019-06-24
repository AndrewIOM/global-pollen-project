namespace GlobalPollenProject.Auth.Migrations

open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Metadata
open Microsoft.EntityFrameworkCore.Migrations
open Microsoft.EntityFrameworkCore.Migrations.Operations.Builders
open Microsoft.EntityFrameworkCore.Migrations.Operations
open System
open GlobalPollenProject.Identity
open Microsoft.EntityFrameworkCore

[<DbContext(typeof<UserDbContext>)>]
[<Migration("20190619130557_identityserver_persistedgrantdb")>]
type IdentityServerPersistedGrantMigration() =
    inherit Migration()

        override __.Up(migrationBuilder:MigrationBuilder) =

            migrationBuilder.CreateTable(
                name = "DeviceCodes",
                columns = (
                    fun table -> 
                    {| DeviceCode = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       UserCode = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       SubjectId = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       ClientId = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       CreationTime = table.Column<DateTime>(nullable = false)
                       Expiration = table.Column<DateTime>(nullable = false)
                       Data = table.Column<string>(maxLength = Nullable(50000), nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_DeviceCodes", (fun x -> (x.UserCode) :> obj)) |> ignore
            ) |> ignore

            migrationBuilder.CreateTable(
                name = "PersistedGrants",
                columns = (
                    fun table -> 
                    {| Key = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       Type = table.Column<string>(maxLength = Nullable(50), nullable = false)
                       SubjectId = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       ClientId = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       CreationTime = table.Column<DateTime>(nullable = false)
                       Expiration = table.Column<DateTime>(nullable = true)
                       Data = table.Column<string>(maxLength = Nullable(50000), nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_PersistedGrants", (fun x -> (x.Key) :> obj)) |> ignore
            ) |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_DeviceCodes_DeviceCode",
                table = "DeviceCodes",
                column = "DeviceCode",
                unique = true ) |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_PersistedGrants_SubjectId_ClientId_Type",
                table = "PersistedGrants",
                columns = [| "SubjectId"; "ClientId"; "Type" |]) |> ignore


    override __.Down(migrationBuilder:MigrationBuilder) =
        migrationBuilder.DropTable(name = "DeviceCodes") |> ignore
        migrationBuilder.DropTable(name = "PersistedGrants") |> ignore

    override __.BuildTargetModel(modelBuilder: ModelBuilder) =
        modelBuilder
            .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
            .HasAnnotation("Relational:MaxIdentifierLength", 128)
            .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.DeviceFlowCodes",
            fun b ->
                b.Property<int>("UserCode").ValueGeneratedOnAdd().HasMaxLength(200) |> ignore
                b.Property<string>("ClientId").IsRequired().HasMaxLength(200) |> ignore
                b.Property<DateTime>("CreationTime") |> ignore
                b.Property<string>("Data").IsRequired().HasMaxLength(50000) |> ignore
                b.Property<string>("DeviceCode").IsRequired().HasMaxLength(200) |> ignore
                b.Property<DateTime>("Expiration").IsRequired() |> ignore
                b.Property<string>("SubjectId").HasMaxLength(200) |> ignore
                b.HasKey("UserCode") |> ignore
                b.HasIndex("DeviceCode").IsUnique() |> ignore
                b.ToTable("DeviceCodes") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.PersistedGrant",
            fun b ->
                b.Property<string>("Key").HasMaxLength(200) |> ignore
                b.Property<string>("ClientId").IsRequired().HasMaxLength(200) |> ignore
                b.Property<DateTime>("CreationTime") |> ignore
                b.Property<string>("Data").IsRequired().HasMaxLength(50000) |> ignore
                b.Property<DateTime>("Expiration") |> ignore
                b.Property<string>("SubjectId").HasMaxLength(200) |> ignore
                b.Property<string>("Type").IsRequired().HasMaxLength(50) |> ignore
                b.HasKey("Key") |> ignore
                b.HasIndex("SubjectId", "ClientId", "Type").IsUnique() |> ignore
                b.ToTable("PersistedGrants") |> ignore ) |> ignore
