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
[<Migration("20190619130556_identityserver_configurationdb")>]
type IdentityServerMigration() =
    inherit Migration()

        override __.Up(migrationBuilder:MigrationBuilder) =

            migrationBuilder.CreateTable(
                name = "ApiResources",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Enabled = table.Column<bool>(nullable = true)
                       Name = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       DisplayName = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       Description = table.Column<string>(maxLength = Nullable(1000), nullable = true)
                       Created = table.Column<DateTime>(nullable = false)
                       Updated = table.Column<DateTime>(nullable = true)
                       LastAccessed = table.Column<DateTime>(nullable = true)
                       NonEditable = table.Column<bool>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ApiResources", (fun x -> (x.Id) :> obj)) |> ignore
            ) |> ignore

            migrationBuilder.CreateTable(
                name = "Clients",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Enabled = table.Column<bool>(nullable = false)
                       ClientId = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       ProtocolType = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       RequireClientSecret = table.Column<bool>(nullable = false)
                       ClientName = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       Description = table.Column<string>(maxLength = Nullable(1000), nullable = true)
                       ClientUri = table.Column<string>(maxLength = Nullable(2000), nullable = true)
                       LogoUri = table.Column<string>(maxLength = Nullable(2000), nullable = true)
                       RequireConsent = table.Column<bool>(nullable = false)
                       AllowRememberConsent = table.Column<bool>(nullable = false)
                       AlwaysIncludeUserClaimsInIdToken = table.Column<bool>(nullable = false)
                       RequirePkce = table.Column<bool>(nullable = false)
                       AllowPlainTextPkce = table.Column<bool>(nullable = false)
                       AllowAccessTokensViaBrowser = table.Column<bool>(nullable = false)
                       FrontChannelLogoutUri = table.Column<string>(maxLength = Nullable(2000), nullable = true)
                       FrontChannelLogoutSessionRequired = table.Column<bool>(nullable = false)
                       BackChannelLogoutUri = table.Column<string>(maxLength = Nullable(2000), nullable = true)
                       BackChannelLogoutSessionRequired = table.Column<bool>(nullable = false)
                       AllowOfflineAccess = table.Column<bool>(nullable = false)
                       IdentityTokenLifetime = table.Column<int>(nullable = false)
                       AccessTokenLifetime = table.Column<int>(nullable = false)
                       AuthorizationCodeLifetime = table.Column<int>(nullable = false)
                       ConsentLifetime = table.Column<int>(nullable = true)
                       AbsoluteRefreshTokenLifetime = table.Column<int>(nullable = false)
                       SlidingRefreshTokenLifetime = table.Column<int>(nullable = false)
                       RefreshTokenUsage = table.Column<int>(nullable = false)
                       UpdateAccessTokenClaimsOnRefresh = table.Column<bool>(nullable = false)
                       RefreshTokenExpiration = table.Column<int>(nullable = false)
                       AccessTokenType = table.Column<int>(nullable = false)
                       EnableLocalLogin = table.Column<bool>(nullable = false)
                       IncludeJwtId = table.Column<bool>(nullable = false)
                       AlwaysSendClientClaims = table.Column<bool>(nullable = false)
                       ClientClaimsPrefix = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       PairWiseSubjectSalt = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       Created = table.Column<DateTime>(nullable = false)
                       Updated = table.Column<DateTime>(nullable = true)
                       LastAccessed = table.Column<DateTime>(nullable = true)
                       UserSsoLifetime = table.Column<int>(nullable = true)
                       UserCodeType = table.Column<string>(maxLength = Nullable(100), nullable = true)
                       DeviceCodeLifetime = table.Column<int>(nullable = false)
                       NonEditable = table.Column<bool>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_Clients", (fun x -> (x.Id) :> obj)) |> ignore
            ) |> ignore

            migrationBuilder.CreateTable(
                name = "IdentityResources",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Enabled = table.Column<bool>(nullable = false)
                       Name = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       DisplayName = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       Description = table.Column<string>(maxLength = Nullable(1000), nullable = true)
                       Required = table.Column<bool>(nullable = false)
                       Emphasize = table.Column<bool>(nullable = false)
                       ShowInDiscoveryDocument = table.Column<bool>(nullable = false)
                       Created = table.Column<DateTime>(nullable = false)
                       Updated = table.Column<DateTime>(nullable = true)
                       NonEditable = table.Column<bool>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_IdentityResources", (fun x -> (x.Id) :> obj)) |> ignore
            ) |> ignore

            migrationBuilder.CreateTable(
                name = "ApiClaims",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Type = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       ApiResourceId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ApiClaims", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ApiClaims_ApiResources_ApiResourceId",
                        column = (fun x -> x.ApiResourceId :> obj),
                        principalTable = "ApiResources",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ApiProperties",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Key = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       Value = table.Column<string>(maxLength = Nullable(2000), nullable = false)
                       ApiResourceId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ApiProperties", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ApiProperties_ApiResources_ApiResourceId",
                        column = (fun x -> x.ApiResourceId :> obj),
                        principalTable = "ApiResources",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ApiScopes",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Name = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       DisplayName = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       Description = table.Column<string>(maxLength = Nullable(1000), nullable = true)
                       Required = table.Column<bool>(nullable = false)
                       Emphasize = table.Column<bool>(nullable = false)
                       ShowInDiscoveryDocument = table.Column<bool>(nullable = false)
                       ApiResourceId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ApiScopes", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ApiScopes_ApiResources_ApiResourceId",
                        column = (fun x -> x.ApiResourceId :> obj),
                        principalTable = "ApiResources",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ApiSecrets",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Name = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       Description = table.Column<string>(maxLength = Nullable(1000), nullable = true)
                       Value = table.Column<string>(maxLength = Nullable(4000), nullable = false)
                       Expiration = table.Column<DateTime>(nullable = true)
                       Type = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       Created = table.Column<DateTime>(nullable = false)
                       ApiResourceId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ApiSecrets", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ApiSecrets_ApiResources_ApiResourceId",
                        column = (fun x -> x.ApiResourceId :> obj),
                        principalTable = "ApiResources",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientClaims",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Type = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       Value = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientClaims", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientClaims_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientCorsOrigins",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Origin = table.Column<string>(maxLength = Nullable(150), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientCorsOrigins", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientCorsOrigins_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientGrantTypes",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       GrantType = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientGrantTypes", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientGrantTypes_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientIdPRestrictions",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Provider = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientIdPRestrictions", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientIdPRestrictions_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientPostLogoutRedirectUris",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       PostLogoutRedirectUri = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientPostLogoutRedirectUris", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientPostLogoutRedirectUris_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientProperties",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Key = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       Value = table.Column<string>(maxLength = Nullable(2000), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientProperties", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientProperties_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientRedirectUris",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       RedirectUri = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientRedirectUris", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientRedirectUris_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientScopes",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Scope = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientScopes", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientScopes_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ClientSecrets",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Description = table.Column<string>(maxLength = Nullable(2000), nullable = true)
                       Value = table.Column<string>(maxLength = Nullable(4000), nullable = false)
                       Expiration = table.Column<DateTime>(nullable = true)
                       Type = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       Created = table.Column<DateTime>(nullable = false)
                       ClientId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ClientSecrets", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ClientSecrets_Clients_ClientId",
                        column = (fun x -> x.ClientId :> obj),
                        principalTable = "Clients",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "IdentityClaims",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Type = table.Column<string>(maxLength = Nullable(200), nullable = true)
                       IdentityResourceId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_IdentityClaims", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_IdentityClaims_IdentityResources_IdentityResourceId",
                        column = (fun x -> x.IdentityResourceId :> obj),
                        principalTable = "IdentityResources",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "IdentityProperties",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Key = table.Column<string>(maxLength = Nullable(250), nullable = false)
                       Value = table.Column<string>(maxLength = Nullable(2000), nullable = false)
                       IdentityResourceId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_IdentityProperties", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_IdentityProperties_IdentityResources_IdentityResourceId",
                        column = (fun x -> x.IdentityResourceId :> obj),
                        principalTable = "IdentityResources",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateTable(
                name = "ApiScopeClaims",
                columns = (
                    fun table -> 
                    {| Id = table.Column<int>(nullable = false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn)
                       Type = table.Column<string>(maxLength = Nullable(200), nullable = false)
                       ApiScopeId = table.Column<int>(nullable = false) |}),
                constraints = fun table ->
                    table.PrimaryKey("PK_ApiScopeClaims", (fun x -> (x.Id) :> obj)) |> ignore
                    table.ForeignKey(
                        name = "FK_ApiScopeClaims_ApiScopes_ApiScopeId",
                        column = (fun x -> x.ApiScopeId :> obj),
                        principalTable = "IdentityResources",
                        principalColumn = "Id",
                        onDelete = ReferentialAction.Cascade ) |> ignore ) |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ApiClaims_ApiResourceId",
                table = "ApiClaims",
                column = "ApiResourceId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ApiProperties_ApiResourceId",
                table = "ApiProperties",
                column = "ApiResourceId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ApiResources_Name",
                table = "ApiResources",
                column = "Name",
                unique = true ) |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ApiScopeClaims_ApiScopeId",
                table = "ApiScopeClaims",
                column = "ApiScopeId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ApiScopes_ApiResourceId",
                table = "ApiScopes",
                column = "ApiResourceId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ApiScopes_Name",
                table = "ApiScopes",
                column = "Name",
                unique = true ) |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ApiSecrets_ApiResourceId",
                table = "ApiSecrets",
                column = "ApiResourceId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientClaims_ClientId",
                table = "ClientClaims",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientCorsOrigins_ClientId",
                table = "ClientCorsOrigins",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientGrantTypes_ClientId",
                table = "ClientGrantTypes",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientIdPRestrictions_ClientId",
                table = "ClientIdPRestrictions",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientPostLogoutRedirectUris_ClientId",
                table = "ClientPostLogoutRedirectUris",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientProperties_ClientId",
                table = "ClientProperties",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientRedirectUris_ClientId",
                table = "ClientRedirectUris",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_Clients_ClientId",
                table = "Clients",
                column = "ClientId",
                unique = true) |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientScopes_ClientId",
                table = "ClientScopes",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_ClientSecrets_ClientId",
                table = "ClientSecrets",
                column = "ClientId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_IdentityClaims_IdentityResourceId",
                table = "IdentityClaims",
                column = "IdentityResourceId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_IdentityProperties_IdentityResourceId",
                table = "IdentityProperties",
                column = "IdentityResourceId") |> ignore

            migrationBuilder.CreateIndex(
                name = "IX_IdentityResources_Name",
                table = "IdentityResources",
                column = "Name",
                unique = true) |> ignore

    override __.Down(migrationBuilder:MigrationBuilder) =
        migrationBuilder.DropTable(name = "ApiClaims") |> ignore
        migrationBuilder.DropTable(name = "ApiProperties") |> ignore
        migrationBuilder.DropTable(name = "ApiScopeClaims") |> ignore
        migrationBuilder.DropTable(name = "ApiSecrets") |> ignore
        migrationBuilder.DropTable(name = "ClientClaims") |> ignore
        migrationBuilder.DropTable(name = "ClientCorsOrigins") |> ignore
        migrationBuilder.DropTable(name = "ClientGrantTypes") |> ignore
        migrationBuilder.DropTable(name = "ClientIdPRestrictions") |> ignore
        migrationBuilder.DropTable(name = "ClientPostLogoutRedirectUris") |> ignore
        migrationBuilder.DropTable(name = "ClientProperties") |> ignore
        migrationBuilder.DropTable(name = "ClientRedirectUris") |> ignore
        migrationBuilder.DropTable(name = "ClientScopes") |> ignore
        migrationBuilder.DropTable(name = "ClientSecrets") |> ignore
        migrationBuilder.DropTable(name = "IdentityClaims") |> ignore
        migrationBuilder.DropTable(name = "IdentityProperties") |> ignore
        migrationBuilder.DropTable(name = "ApiScopes") |> ignore
        migrationBuilder.DropTable(name = "Clients") |> ignore
        migrationBuilder.DropTable(name = "IdentityResources") |> ignore
        migrationBuilder.DropTable(name = "ApiResources") |> ignore

    override __.BuildTargetModel(modelBuilder: ModelBuilder) =
        modelBuilder
            .HasAnnotation("ProductVersion", "2.1.4-rtm-31024")
            .HasAnnotation("Relational:MaxIdentifierLength", 128)
            .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiResource",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<DateTime>("Created") |> ignore
                b.Property<string>("Description").HasMaxLength(1000) |> ignore
                b.Property<string>("DisplayName").HasMaxLength(200) |> ignore
                b.Property<bool>("Enabled") |> ignore
                b.Property<Nullable<DateTime>>("LastAccessed") |> ignore
                b.Property<string>("Name").IsRequired().HasMaxLength(200) |> ignore
                b.Property<bool>("NonEditable") |> ignore
                b.Property<Nullable<DateTime>>("Updated") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("Name").IsUnique() |> ignore
                b.ToTable("ApiResources") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiResourceClaim",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ApiResourceId") |> ignore
                b.Property<string>("Type").IsRequired().HasMaxLength(200) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ApiResourceId").IsUnique() |> ignore
                b.ToTable("ApiClaims") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiResourceProperty",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ApiResourceId") |> ignore
                b.Property<string>("Key").IsRequired().HasMaxLength(250) |> ignore
                b.Property<string>("Value").IsRequired().HasMaxLength(2000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ApiResourceId").IsUnique() |> ignore
                b.ToTable("ApiProperties") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiScope",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ApiResourceId") |> ignore
                b.Property<string>("Description").HasMaxLength(1000) |> ignore
                b.Property<string>("DisplayName").HasMaxLength(200) |> ignore
                b.Property<bool>("Emphasize") |> ignore
                b.Property<string>("Name").IsRequired().HasMaxLength(200) |> ignore
                b.Property<bool>("Required") |> ignore
                b.Property<bool>("ShowInDiscoveryDocument") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ApiResourceId") |> ignore
                b.HasIndex("Name").IsUnique() |> ignore
                b.ToTable("ApiScopes") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiScopeClaim",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ApiScopeId") |> ignore
                b.Property<string>("Type").IsRequired().HasMaxLength(200) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ApiScopeId") |> ignore
                b.ToTable("ApiScopeClaims") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiSecret",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ApiResourceId") |> ignore
                b.Property<DateTime>("Created") |> ignore
                b.Property<string>("Description").HasMaxLength(1000) |> ignore
                b.Property<Nullable<DateTime>>("Expiration") |> ignore
                b.Property<string>("Type").IsRequired().HasMaxLength(250) |> ignore
                b.Property<string>("Value").IsRequired().HasMaxLength(4000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ApiResourceId") |> ignore
                b.ToTable("ApiSecrets") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.Client",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("AbsoluteRefreshTokenLifetime") |> ignore
                b.Property<int>("AccessTokenLifetime") |> ignore
                b.Property<int>("AccessTokenType") |> ignore
                b.Property<bool>("AllowAccessTokensViaBrowser") |> ignore
                b.Property<bool>("AllowOfflineAccess") |> ignore
                b.Property<bool>("AllowPlainTextPkce") |> ignore
                b.Property<bool>("AllowRememberConsent") |> ignore
                b.Property<bool>("AlwaysIncludeUserClaimsInIdToken") |> ignore
                b.Property<bool>("AlwaysSendClientClaims") |> ignore
                b.Property<int>("AuthorizationCodeLifetime") |> ignore
                b.Property<bool>("BackChannelLogoutSessionRequired") |> ignore
                b.Property<string>("BackChannelLogoutUri").HasMaxLength(2000) |> ignore
                b.Property<string>("ClientClaimsPrefix").HasMaxLength(200) |> ignore
                b.Property<string>("ClientId").IsRequired().HasMaxLength(200) |> ignore
                b.Property<string>("ClientName").HasMaxLength(200) |> ignore
                b.Property<string>("ClientUri").HasMaxLength(2000) |> ignore
                b.Property<Nullable<int>>("ConsentLifetime") |> ignore
                b.Property<DateTime>("Created") |> ignore
                b.Property<string>("Description").HasMaxLength(1000) |> ignore
                b.Property<int>("DeviceCodeLifetime") |> ignore
                b.Property<bool>("EnableLocalLogin") |> ignore
                b.Property<bool>("Enabled") |> ignore
                b.Property<bool>("FrontChannelLogoutSessionRequired") |> ignore
                b.Property<string>("FrontChannelLogoutUri").HasMaxLength(2000) |> ignore
                b.Property<int>("IdentityTokenLifetime") |> ignore
                b.Property<bool>("IncludeJwtId") |> ignore
                b.Property<Nullable<DateTime>>("LastAccessed") |> ignore
                b.Property<string>("LogoUri").HasMaxLength(2000) |> ignore
                b.Property<bool>("NonEditable") |> ignore
                b.Property<string>("PairWiseSubjectSalt").HasMaxLength(200) |> ignore
                b.Property<string>("ProtocolType").IsRequired().HasMaxLength(200) |> ignore
                b.Property<int>("RefreshTokenExpiration") |> ignore
                b.Property<int>("RefreshTokenUsage") |> ignore
                b.Property<bool>("RequireClientSecret") |> ignore
                b.Property<bool>("RequireConsent") |> ignore
                b.Property<bool>("RequirePkce") |> ignore
                b.Property<int>("SlidingRefreshTokenLifetime") |> ignore
                b.Property<bool>("UpdateAccessTokenClaimsOnRefresh") |> ignore
                b.Property<Nullable<DateTime>>("Updated") |> ignore
                b.Property<string>("UserCodeType").HasMaxLength(100) |> ignore
                b.Property<Nullable<int>>("UserSsoLifetime") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId").IsUnique() |> ignore
                b.ToTable("Clients") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientClaim",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("Type").IsRequired().HasMaxLength(250) |> ignore
                b.Property<string>("Value").IsRequired().HasMaxLength(2000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientClaims") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientCorsOrigin",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("Origin").IsRequired().HasMaxLength(250) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientCorsOrigins") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientGrantType",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("GrantType").IsRequired().HasMaxLength(250) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientGrantTypes") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientIdPRestriction",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("Provider").IsRequired().HasMaxLength(200) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientIdPRestrictions") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientPostLogoutRedirectUri",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("PostLogoutRedirectUri").IsRequired().HasMaxLength(2000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientPostLogoutRedirectUris") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientProperty",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("Key").IsRequired().HasMaxLength(250) |> ignore
                b.Property<string>("Value").IsRequired().HasMaxLength(2000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientProperties") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientRedirectUri",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("RedirectUri").IsRequired().HasMaxLength(2000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientRedirectUris") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientScope",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<string>("Scope").IsRequired().HasMaxLength(200) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientScopes") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientSecret",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("ClientId") |> ignore
                b.Property<DateTime>("Created") |> ignore
                b.Property<string>("Description").HasMaxLength(2000) |> ignore
                b.Property<Nullable<DateTime>>("Expiration") |> ignore
                b.Property<string>("Type").IsRequired().HasMaxLength(250) |> ignore
                b.Property<string>("Value").IsRequired().HasMaxLength(4000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("ClientId") |> ignore
                b.ToTable("ClientSecrets") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.IdentityClaim",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("IdentityResourceId") |> ignore
                b.Property<string>("Type").IsRequired().HasMaxLength(200) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("IdentityResourceId") |> ignore
                b.ToTable("IdentityClaims") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.IdentityResource",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<DateTime>("Created") |> ignore
                b.Property<string>("Description").HasMaxLength(1000) |> ignore
                b.Property<string>("DisplayName").HasMaxLength(200) |> ignore
                b.Property<bool>("Emphasize") |> ignore
                b.Property<bool>("Enabled") |> ignore
                b.Property<string>("Name").IsRequired().HasMaxLength(200) |> ignore
                b.Property<bool>("NonEditable") |> ignore
                b.Property<bool>("Required") |> ignore
                b.Property<bool>("ShowInDiscoveryDocument") |> ignore
                b.Property<Nullable<DateTime>>("Updated") |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("Name").IsUnique() |> ignore
                b.ToTable("IdentityResources") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.IdentityResourceProperty",
            fun b ->
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn) |> ignore
                b.Property<int>("IdentityResourceId") |> ignore
                b.Property<string>("Key").IsRequired().HasMaxLength(250) |> ignore
                b.Property<string>("Value").IsRequired().HasMaxLength(2000) |> ignore
                b.HasKey("Id") |> ignore
                b.HasIndex("IdentityResourceId") |> ignore
                b.ToTable("IdentityProperties") |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiResourceClaim",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.ApiResource", "ApiResource")
                    .WithMany("UserClaims")
                    .HasForeignKey("ApiResourceId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiResourceProperty",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.ApiResource", "ApiResource")
                    .WithMany("Properties")
                    .HasForeignKey("ApiResourceId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiScope",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.ApiResource", "ApiResource")
                    .WithMany("Scopes")
                    .HasForeignKey("ApiResourceId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiScopeClaim",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.ApiScope", "ApiScope")
                    .WithMany("UserClaims")
                    .HasForeignKey("ApiScopeId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ApiSecret",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.ApiResource", "ApiResource")
                    .WithMany("Secrets")
                    .HasForeignKey("ApiResourceId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientClaim",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("Claims")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientCorsOrigin",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("AllowedCorsOrigins")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientGrantType",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("AllowedGrantTypes")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientIdPRestriction",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("IdentityProviderRestrictions")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientPostLogoutRedirectUri",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("PostLogoutRedirectUris")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientProperty",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("Properties")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientRedirectUri",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("RedirectUris")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientScope",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("AllowedScopes")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.ClientSecret",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.Client", "Client")
                    .WithMany("ClientSecrets")
                    .HasForeignKey("ClientId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.IdentityClaim",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.IdentityResource", "IdentityResource")
                    .WithMany("UserClaims")
                    .HasForeignKey("IdentityResourceId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore

        modelBuilder.Entity("IdentityServer4.EntityFramework.Entities.IdentityResourceProperty",
             fun b ->
                b.HasOne("IdentityServer4.EntityFramework.Entities.IdentityResource", "IdentityResource")
                    .WithMany("Properties")
                    .HasForeignKey("IdentityResourceId")
                    .OnDelete(DeleteBehavior.Cascade) |> ignore ) |> ignore
