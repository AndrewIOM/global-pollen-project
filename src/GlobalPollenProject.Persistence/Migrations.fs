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
    NamedBy: OperationBuilder<AddColumnOperation>
    LatinName: OperationBuilder<AddColumnOperation>
    Rank: OperationBuilder<AddColumnOperation>
    ReferenceName: OperationBuilder<AddColumnOperation>
    ReferenceUrl: OperationBuilder<AddColumnOperation> }

type ReferenceCollectionSummaryTable = {
    Id: OperationBuilder<AddColumnOperation>
    User: OperationBuilder<AddColumnOperation>
    Name: OperationBuilder<AddColumnOperation>
    Description: OperationBuilder<AddColumnOperation>
    SlideCount: OperationBuilder<AddColumnOperation>
}

type CalibrationTable = {
    Id: OperationBuilder<AddColumnOperation>
    User: OperationBuilder<AddColumnOperation>
    Device: OperationBuilder<AddColumnOperation>
    Ocular: OperationBuilder<AddColumnOperation>
    Objective: OperationBuilder<AddColumnOperation>
    Image: OperationBuilder<AddColumnOperation>
    PixelWidth: OperationBuilder<AddColumnOperation>
}

type SlideTable = {
    Id: OperationBuilder<AddColumnOperation>
    CollectionId: OperationBuilder<AddColumnOperation>
    CollectionSlideId: OperationBuilder<AddColumnOperation>
    IdentificationMethod: OperationBuilder<AddColumnOperation>
    FamilyOriginal: OperationBuilder<AddColumnOperation>
    GenusOriginal: OperationBuilder<AddColumnOperation>
    SpeciesOriginal: OperationBuilder<AddColumnOperation>
    IsFullyDigitised: OperationBuilder<AddColumnOperation>
    ReferenceCollectionId: OperationBuilder<AddColumnOperation>
    TaxonId: OperationBuilder<AddColumnOperation>
}

type ReferenceCollectionTable = {
    Id: OperationBuilder<AddColumnOperation>
    User: OperationBuilder<AddColumnOperation>
    Name: OperationBuilder<AddColumnOperation>
    Status: OperationBuilder<AddColumnOperation>
    Version: OperationBuilder<AddColumnOperation>
    Description: OperationBuilder<AddColumnOperation>
}

type SlideImageTable = {
    Id: OperationBuilder<AddColumnOperation>
    CalibrationImageUrl: OperationBuilder<AddColumnOperation>
    CalibrationFocusLevel: OperationBuilder<AddColumnOperation>
    PixelWidth: OperationBuilder<AddColumnOperation>
    SlideId: OperationBuilder<AddColumnOperation>
}

type FrameTable = {
    Id: OperationBuilder<AddColumnOperation>
    Url: OperationBuilder<AddColumnOperation>
    SlideImageId: OperationBuilder<AddColumnOperation>
}

type SlideSummary = {
    Id: OperationBuilder<AddColumnOperation>
    ThumbnailUrl: OperationBuilder<AddColumnOperation>
    TaxonId: OperationBuilder<AddColumnOperation>
}



// Migrations
[<DbContext(typeof<EntityFramework.ReadContext>)>]
[<Migration("20170105185538_Initial")>]
type Init() =
    inherit Migration()
    
    override this.Up(migrationBuilder: MigrationBuilder) =
        migrationBuilder.CreateTable(
            name = "GrainSummary",
            columns = 
                (fun table -> 
                    { Id = table.Column<Guid>(nullable = false)
                      Id2 = table.Column<Guid>(nullable = true)
                      Thumbnail = table.Column<string>(nullable = true) }),
            constraints = 
                fun table -> 
                    table.PrimaryKey("PK_GrainSummary", (fun x -> x.Id2 :> obj))|> ignore ) |> ignore
        
        migrationBuilder.CreateTable(
            name = "TaxonSummary",
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
                    table.PrimaryKey("PK_TaxonSummary", (fun x -> x.Id :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "BackboneTaxon",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      Family = table.Column<string>(nullable = true)
                      Genus = table.Column<string>(nullable = true)
                      Species = table.Column<string>(nullable = true)
                      NamedBy = table.Column<string>(nullable = true)
                      LatinName = table.Column<string>(nullable = true)
                      Rank = table.Column<string>(nullable = true)
                      ReferenceName = table.Column<int>(nullable = false)
                      ReferenceUrl = table.Column<string>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_BackboneTaxon", (fun x -> x.Id :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "ReferenceCollectionSummary",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      User = table.Column<Guid>(nullable = false)
                      Name = table.Column<string>(nullable = true)
                      Description = table.Column<string>(nullable = true)
                      SlideCount = table.Column<int>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_ReferenceCollectionSummary", (fun x -> x.Id :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "Calibration",
            columns =
                (fun table ->
                    {   Id = table.Column<Guid>(nullable=false)
                        User = table.Column<Guid>(nullable=false)
                        Device = table.Column<string>(nullable=false)
                        Ocular = table.Column<int>(nullable=false)
                        Objective = table.Column<int>(nullable=false)
                        Image = table.Column<string>(nullable=false)
                        PixelWidth = table.Column<float>(nullable=false) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_Calibration", (fun x -> x.Id :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "ReferenceCollection",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      User = table.Column<Guid>(nullable = false)
                      Name = table.Column<string>(nullable = true)
                      Status = table.Column<string>(nullable = true)
                      Description = table.Column<string>(nullable = true)
                      Version = table.Column<int>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_ReferenceCollection", (fun x -> x.Id :> obj)) |> ignore ) |> ignore
        
        migrationBuilder.CreateTable(
            name = "Slide",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      CollectionId = table.Column<Guid>(nullable = true)
                      CollectionSlideId = table.Column<string>(nullable = true)
                      IdentificationMethod = table.Column<string>(nullable = true)
                      FamilyOriginal = table.Column<string>(nullable = true)
                      GenusOriginal = table.Column<string>(nullable = true)
                      SpeciesOriginal = table.Column<string>(nullable = true)
                      ReferenceCollectionId = table.Column<Guid>(nullable = true)
                      TaxonId = table.Column<Guid>(nullable = true)
                      IsFullyDigitised = table.Column<int>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_Slide", (fun x -> x.Id :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_Slide_ReferenceCollection_ReferenceCollectionId",
                        column = (fun x -> x.ReferenceCollectionId :> obj),
                        principalTable = "ReferenceCollection",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Restrict) |> ignore
                    table.ForeignKey(
                        name = "FK_Slide_TaxonSummary_TaxonId",
                        column = (fun x -> x.TaxonId :> obj),
                        principalTable = "TaxonSummary",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Restrict) |> ignore ) |> ignore


        migrationBuilder.CreateTable(
            name = "SlideImage",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      CalibrationImageUrl = table.Column<string>(nullable = true)
                      CalibrationFocusLevel = table.Column<string>(nullable = true)
                      PixelWidth = table.Column<float>(nullable = true)
                      SlideId = table.Column<Guid>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_SlideImage", (fun x -> x.Id :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_SlideImage_Slide_SlideId",
                        column = (fun x -> x.SlideId :> obj),
                        principalTable = "Slide",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Restrict) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "Frame",
            columns =
                (fun table ->
                    { Id = table.Column<Guid>(nullable = false)
                      Url = table.Column<string>(nullable = true)
                      SlideImageId = table.Column<Guid>(nullable = true) }),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_Frame", (fun x -> x.Id :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_Frame_SlideImage_SlideImageId",
                        column = (fun x -> x.SlideImageId :> obj),
                        principalTable = "SlideImage",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Restrict) |> ignore ) |> ignore

        migrationBuilder.CreateIndex("IX_Frame_SlideImageId", "Frame", "SlideImageId") |> ignore
        migrationBuilder.CreateIndex("IX_Slide_ReferenceCollectionId", "Slide", "ReferenceCollectionId") |> ignore
        migrationBuilder.CreateIndex("IX_Slide_TaxonId","Slide","TaxonId") |> ignore
        migrationBuilder.CreateIndex("IX_SlideImage_SlideId","SlideImage","SlideId") |> ignore


    override this.Down(migrationBuilder: MigrationBuilder) = 
        migrationBuilder.DropTable(name = "GrainSummary") |> ignore
        migrationBuilder.DropTable(name = "TaxonSummary") |> ignore
        migrationBuilder.DropTable(name = "BackboneTaxon") |> ignore
        migrationBuilder.DropTable(name = "ReferenceCollectionSummary") |> ignore
        migrationBuilder.DropTable(name = "ReferenceCollection") |> ignore
        migrationBuilder.DropTable(name = "Slide") |> ignore
        migrationBuilder.DropTable(name = "SlideImage") |> ignore
        migrationBuilder.DropTable(name = "Frame") |> ignore
        migrationBuilder.DropTable(name = "Calibration") |> ignore

    override this.BuildTargetModel(modelBuilder: ModelBuilder) =
        modelBuilder
            .HasAnnotation("ProductVersion", "1.1.1")
            |> ignore

        modelBuilder.Entity("ReadStore.GrainSummary", 
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("Thumbnail") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("GrainSummary") |> ignore)|> ignore

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
                b.ToTable("TaxonSummary") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.BackboneTaxon",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("Family") |> ignore
                b.Property<string>("Genus") |> ignore
                b.Property<string>("LatinName") |> ignore
                b.Property<string>("NamedBy") |> ignore
                b.Property<string>("Rank") |> ignore
                b.Property<string>("Species") |> ignore
                b.Property<string>("Reference") |> ignore
                b.Property<string>("ReferenceUrl") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("BackboneTaxon") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.ReferenceCollectionSummary",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<Guid>("User") |> ignore
                b.Property<string>("Name") |> ignore
                b.Property<string>("Description") |> ignore
                b.Property<int>("SlideCount") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("ReferenceCollectionSummary") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.ReferenceCollection",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<Guid>("User") |> ignore
                b.Property<string>("Name") |> ignore
                b.Property<string>("Description") |> ignore
                b.Property<string>("Status") |> ignore
                b.Property<int>("Version") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("ReferenceCollection") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.Slide",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<Guid>("CollectionId") |> ignore
                b.Property<string>("CollectionSlideId") |> ignore
                b.Property<string>("FamilyOriginal") |> ignore
                b.Property<string>("GenusOriginal") |> ignore
                b.Property<string>("IdentificationMethod") |> ignore
                b.Property<bool>("IsFullyDigitised") |> ignore
                b.Property<System.Nullable<Guid>>("ReferenceCollectionId") |> ignore
                b.Property<string>("SpeciesOriginal") |> ignore
                b.Property<System.Nullable<Guid>>("TaxonId") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ReferenceCollectionId") |> ignore
                b.HasIndex("TaxonId") |> ignore
                b.ToTable("Slide") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.SlideImage",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("CalibrationImageUrl") |> ignore
                b.Property<int>("CalibrationFocusLevel") |> ignore
                b.Property<System.Nullable<Guid>>("SlideId") |> ignore
                b.Property<float>("PixelWidth") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("SlideId") |> ignore
                b.ToTable("SlideImage") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.Frame",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<System.Nullable<int>>("SlideImageId") |> ignore
                b.Property<string>("Url") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("SlideImageId") |> ignore
                b.ToTable("SlideImage") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.Calibration",
            fun b ->
                b.Property<Guid>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<Guid>("User") |> ignore
                b.Property<string>("Device") |> ignore
                b.Property<int>("Ocular") |> ignore
                b.Property<int>("Objective") |> ignore
                b.Property<string>("Image") |> ignore
                b.Property<float>("PixelWidth") |> ignore
                b.HasKey("Id") |> ignore
                b.ToTable("Calibration") |> ignore) |> ignore

        modelBuilder.Entity("ReadStore.Frame",
            fun b ->
                b.HasOne("ReadStore.SlideImage")
                    .WithMany("Frames")
                    .HasForeignKey("SlideImageId") |> ignore ) |> ignore

        modelBuilder.Entity("ReadStore.Slide",
            fun b ->
                b.HasOne("ReadStore.ReferenceCollection")
                    .WithMany("Slides")
                    .HasForeignKey("ReferenceCollectionId") |> ignore

                b.HasOne("ReadStore.TaxonSummary", "Taxon")
                    .WithMany()
                    .HasForeignKey("TaxonId") |> ignore ) |> ignore

        modelBuilder.Entity("ReadStore.SlideImage",
            fun b ->
                b.HasOne("ReadStore.Slide")
                    .WithMany("Images")
                    .HasForeignKey("SlideId") |> ignore ) |> ignore


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
