namespace GlobalPollenProject.Auth.Migrations

open Microsoft.EntityFrameworkCore.Infrastructure
open Microsoft.EntityFrameworkCore.Migrations
open System
open GlobalPollenProject.Identity
open Microsoft.EntityFrameworkCore

// Migrations
// --------------------------------------------------------

[<DbContext(typeof<UserDbContext>)>]
[<Migration("20170218140556_initial")>]
type Initial() =
    inherit Migration()

    override __.Up(migrationBuilder:MigrationBuilder) =
        
        migrationBuilder.CreateTable(
            name = "AspNetUsers",
            columns =
                (fun table ->
                   {| Id = table.Column<string>(nullable = false)
                      AccessFailedCount = table.Column<int>(nullable = false)
                      ConcurrencyStamp = table.Column<string>(nullable = true)
                      Email = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      EmailConfirmed = table.Column<bool>(nullable = false)
                      LockoutEnabled = table.Column<bool>(nullable = false)
                      LockoutEnd = table.Column<DateTimeOffset>(nullable = true)
                      NormalizedEmail = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      NormalizedUserName = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      PasswordHash = table.Column<string>(nullable = true)
                      PhoneNumber = table.Column<string>(nullable = true)
                      PhoneNumberConfirmed = table.Column<bool>(nullable = false)
                      SecurityStamp = table.Column<string>(nullable = true)
                      TwoFactorEnabled = table.Column<bool>(nullable = false)
                      Title = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      GivenNames = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      FamilyName = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      Organisation = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      UserName = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                    |}),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_AspNetUsers", (fun x -> x.Id :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "AspNetRoles",
            columns =
                (fun table ->
                   {| Id = table.Column<string>(nullable = false)
                      ConcurrencyStamp = table.Column<string>(nullable = true)
                      Name = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                      NormalizedName = table.Column<string>(maxLength = Nullable<int>(256), nullable = true)
                    |}),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_AspNetRoles", (fun x -> x.Id :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "AspNetUserTokens",
            columns =
                (fun table ->
                   {| UserId = table.Column<string>(nullable = false)
                      LoginProvider = table.Column<string>(nullable = false)
                      Name = table.Column<string>(nullable = false)
                      Value = table.Column<string>(nullable = true)
            |}),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_AspNetUserTokens", (fun x -> (x.UserId, x.LoginProvider, x.Name) :> obj)) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "AspNetUserClaims",
            columns =
                (fun table ->
                   {| Id = table.Column<int>(nullable = false).Annotation("Sqlite:Autoincrement", true)
                      ClaimType = table.Column<string>(nullable =true)
                      ClaimValue = table.Column<string>(nullable = true)
                      UserId = table.Column<string>(nullable = false)
                |}),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_AspNetUserClaims", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column = (fun x -> x.UserId :> obj),
                        principalTable = "AspNetUsers",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "AspNetUserLogins",
            columns =
                (fun table ->
                   {| LoginProvider = table.Column<string>(nullable = false)
                      ProviderKey = table.Column<string>(nullable = false)
                      ProviderDisplayName = table.Column<string>(nullable = true)
                      UserId = table.Column<string>(nullable = false)
                |}),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_AspNetUserLogins", (fun x -> (x.LoginProvider, x.ProviderKey) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column = (fun x -> x.UserId :> obj),
                        principalTable = "AspNetUsers",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "AspNetRoleClaims",
            columns =
                (fun table ->
                   {| Id = table.Column<int>(nullable = false).Annotation("Sqlite:Autoincrement", true)
                      ClaimType = table.Column<string>(nullable = true)
                      ClaimValue = table.Column<string>(nullable = true)
                      RoleId = table.Column<string>(nullable = false)
                |}),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_AspNetRoleClaims", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column = (fun x -> x.RoleId :> obj),
                        principalTable = "AspNetRoles",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

        migrationBuilder.CreateTable(
            name = "AspNetUserRoles",
            columns =
                (fun table ->
                   {| UserId = table.Column<string>(nullable = false)
                      RoleId = table.Column<string>(nullable = false)
                |}),
            constraints =
                fun table ->
                    table.PrimaryKey("PK_AspNetUserRoles", (fun x -> (x.UserId, x.RoleId) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column = (fun x -> x.RoleId :> obj),
                        principalTable = "AspNetRoles",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore
                    table.ForeignKey(
                        name = "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column = (fun x -> x.UserId :> obj),
                        principalTable = "AspNetUsers",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

        migrationBuilder.CreateIndex(
            name = "EmailIndex",
            table = "AspNetUsers",
            column = "NormalizedEmail") |> ignore

        migrationBuilder.CreateIndex(
            name = "UserNameIndex",
            table = "AspNetUsers",
            column = "NormalizedUserName",
            unique = true) |> ignore

        migrationBuilder.CreateIndex(
            name = "RoleNameIndex",
            table = "AspNetRoles",
            column = "NormalizedName",
            unique = true) |> ignore

        migrationBuilder.CreateIndex(
            name = "IX_AspNetRoleClaims_RoleId",
            table = "AspNetRoleClaims",
            column = "RoleId") |> ignore

        migrationBuilder.CreateIndex(
            name = "IX_AspNetUserClaims_UserId",
            table = "AspNetUserClaims",
            column = "UserId") |> ignore

        migrationBuilder.CreateIndex(
            name = "IX_AspNetUserLogins_UserId",
            table = "AspNetUserLogins",
            column = "UserId") |> ignore

        migrationBuilder.CreateIndex(
            name = "IX_AspNetUserRoles_RoleId",
            table = "AspNetUserRoles",
            column = "RoleId") |> ignore

    override __.Down(migrationBuilder:MigrationBuilder) =
        migrationBuilder.DropTable(
            name = "AspNetRoleClaims") |> ignore

        migrationBuilder.DropTable(
            name = "AspNetUserClaims") |> ignore

        migrationBuilder.DropTable(
            name = "AspNetUserLogins") |> ignore

        migrationBuilder.DropTable(
            name = "AspNetUserRoles") |> ignore

        migrationBuilder.DropTable(
            name = "AspNetUserTokens") |> ignore

        migrationBuilder.DropTable(
            name = "AspNetRoles") |> ignore

        migrationBuilder.DropTable(
            name = "AspNetUsers") |> ignore

    override __.BuildTargetModel(modelBuilder: ModelBuilder) =
        modelBuilder.HasAnnotation("ProductVersion", "1.1.0-rtm-22752") |> ignore

        modelBuilder.Entity("GlobalPollenProject.Auth.ApplicationUser",
            fun b ->
                b.Property<string>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<int>("AccessFailedCount") |> ignore
                b.Property<string>("ConcurrencyStamp").IsConcurrencyToken() |> ignore
                b.Property<string>("Email").HasMaxLength(256) |> ignore
                b.Property<bool>("EmailConfirmed") |> ignore
                b.Property<bool>("LockoutEnabled") |> ignore
                b.Property<Nullable<DateTimeOffset>>("LockoutEnd") |> ignore
                b.Property<string>("NormalizedEmail").HasMaxLength(256) |> ignore
                b.Property<string>("NormalizedUserName").HasMaxLength(256) |> ignore
                b.Property<string>("PasswordHash") |> ignore
                b.Property<string>("PhoneNumber") |> ignore
                b.Property<bool>("PhoneNumberConfirmed") |> ignore
                b.Property<string>("SecurityStamp") |> ignore
                b.Property<bool>("TwoFactorEnabled") |> ignore
                b.Property<string>("UserName").HasMaxLength(256) |> ignore
                b.Property<string>("Title") |> ignore
                b.Property<string>("GivenNames") |> ignore
                b.Property<string>("FamilyName") |> ignore
                b.Property<string>("Organisation") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("NormalizedEmail").HasName("EmailIndex") |> ignore
                b.HasIndex("NormalizedUserName").IsUnique().HasName("UserNameIndex") |> ignore
                b.ToTable("AspNetUsers") |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRole",
            fun b ->
                b.Property<string>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("ConcurrencyStamp").IsConcurrencyToken() |> ignore
                b.Property<string>("Name").HasMaxLength(256) |> ignore
                b.Property<string>("NormalizedName").HasMaxLength(256) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("NormalizedName").IsUnique().HasName("RoleNameIndex") |> ignore
                b.ToTable("AspNetRoles") |> ignore ) |> ignore
                
        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRoleClaim<string>",
            fun b ->
                b.Property<int>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("ClaimType") |> ignore
                b.Property<string>("ClaimValue") |> ignore
                b.Property<string>("RoleId").IsRequired() |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("RoleId") |> ignore
                b.ToTable("AspNetRoleClaims") |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserClaim<string>",
            fun b ->
                b.Property<int>("Id").ValueGeneratedOnAdd() |> ignore
                b.Property<string>("ClaimType") |> ignore
                b.Property<string>("ClaimValue") |> ignore
                b.Property<string>("UserId").IsRequired() |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("UserId") |> ignore
                b.ToTable("AspNetUserClaims") |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserLogin<string>",
            fun b ->
                b.Property<string>("LoginProvider") |> ignore
                b.Property<string>("ProviderKey") |> ignore
                b.Property<string>("ProviderDisplayName") |> ignore
                b.Property<string>("UserId").IsRequired() |> ignore
                b.HasKey("LoginProvider", "ProviderKey") |> ignore
                b.HasIndex("UserId") |> ignore
                b.ToTable("AspNetUserLogins") |> ignore
            ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserRole<string>",
            fun b ->
                b.Property<string>("UserId") |> ignore
                b.Property<string>("RoleId") |> ignore
                b.HasKey("UserId", "RoleId") |> ignore
                b.HasIndex("RoleId") |> ignore
                b.ToTable("AspNetUserRoles") |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserToken<string>",
            fun b ->
                b.Property<string>("UserId") |> ignore
                b.Property<string>("LoginProvider") |> ignore
                b.Property<string>("Name") |> ignore
                b.Property<string>("Value") |> ignore
                b.HasKey("UserId", "LoginProvider", "Name") |> ignore
                b.ToTable("AspNetUserTokens") |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRoleClaim<string>",
            fun b ->
                b.HasOne("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRole")
                    .WithMany("Claims")
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserClaim<string>",
            fun b ->
                b.HasOne("GlobalPollenProject.Shared.Identity.Models.ApplicationUser")
                    .WithMany("Claims")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserLogin<string>",
            fun b ->
                b.HasOne("GlobalPollenProject.Shared.Identity.Models.ApplicationUser")
                    .WithMany("Logins")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityUserRole<string>",
            fun b ->
                b.HasOne("Microsoft.AspNetCore.Identity.EntityFrameworkCore.IdentityRole")
                    .WithMany("Users")
                    .HasForeignKey("RoleId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore

                b.HasOne("GlobalPollenProject.Shared.Identity.Models.ApplicationUser")
                    .WithMany("Roles")
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore