namespace GlobalPollenProject.Identity

open System
open Giraffe
open Microsoft.AspNetCore.Identity
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Diagnostics.HealthChecks
open IdentityServer4.Services
open IdentityServer4.Models
open IdentityServer4.Stores
open System.ComponentModel.DataAnnotations
open System.Threading.Tasks
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore
open IdentityServer4.EntityFramework.DbContexts
open GlobalPollenProject.Identity.ViewModels
open Microsoft.AspNetCore.WebUtilities
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Hosting

module Login =

    type ILoginService<'T> =
        
        abstract member ValidateCredentials : 'T -> string -> Task<bool>
        abstract member FindByUsername : string -> Task<'T>
        abstract member SignInAsync : 'T -> AuthenticationProperties -> Task

    type EFLoginService(userManager:UserManager<ApplicationUser>, signInManager:SignInManager<ApplicationUser>) =
        interface ILoginService<ApplicationUser> with

            member __.FindByUsername user =
                userManager.FindByEmailAsync(user)

            member __.ValidateCredentials user password =
                userManager.CheckPasswordAsync(user, password)

            member __.SignInAsync user properties =
                signInManager.SignInAsync(user, properties)


module Redirect =

    open System.Text.RegularExpressions

    type IRedirectService =
        abstract member ExtractRedirectUriFromReturnUrl : string -> string

    type RedirectService =
        interface IRedirectService with

            member __.ExtractRedirectUriFromReturnUrl url =
                let results = 
                    url
                    |> System.Net.WebUtility.HtmlDecode
                    |> fun x -> Regex.Split(x, "redirect_uri=")
                match results.Length with
                | r when r < 2 -> ""
                | _ ->
                    let result = results.[1]
                    let splitKey =
                        if result.Contains("signin-oidc")
                        then "signin-oidc"
                        else "scope"
                    let results = Regex.Split(result, splitKey)
                    if results.Length < 2
                    then ""
                    else
                        let result = results.[0]
                        result.Replace("%3A", "").Replace("%2F", "/").Replace("&", "")


module Profile =

    open System.Security.Claims
    open System.IdentityModel.Tokens.Jwt

    let appendClaim name prop claims =
        if String.IsNullOrEmpty prop
        then claims
        else Claim(name, prop) :: claims

    let getClaims (user:ApplicationUser) =
        [ Claim(IdentityModel.JwtClaimTypes.Subject, user.Id)
          Claim(IdentityModel.JwtClaimTypes.GivenName, user.GivenNames)
          Claim(IdentityModel.JwtClaimTypes.FamilyName, user.FamilyName)
          Claim(IdentityModel.JwtClaimTypes.PreferredUserName, user.UserName)
          Claim(JwtRegisteredClaimNames.UniqueName, user.UserName) ]
        |> appendClaim "organisation" user.Organisation
        |> appendClaim "title" user.Title

    type ProfileService(userManager:UserManager<ApplicationUser>) =
        interface IProfileService with

            member __.GetProfileDataAsync(context: ProfileDataRequestContext): Task = 
                task {
                    let subject = context.Subject
                    let subjectId = (subject.Claims |> Seq.where(fun x -> x.Type = "sub") |> Seq.head).Value
                    let! user = userManager.FindByIdAsync subjectId
                    if isNull user then invalidArg "subjectId" "Invalid subject identifier"
                    let claims = getClaims user
                    context.IssuedClaims <- claims |> Collections.Generic.List
                } :> Task

            member __.IsActiveAsync(context: IsActiveContext): Task = 
                task {
                    let subject = context.Subject
                    let subjectId = (subject.Claims |> Seq.where(fun x -> x.Type = "sub") |> Seq.head).Value
                    let! user = userManager.FindByIdAsync subjectId
                    context.IsActive <- true
                } :> Task

module Handlers =

    open System.Text
    open GlobalPollenProject.Shared

    let identityToValidationError' (e:IdentityError) =
        {Property = e.Code; Errors = [e.Description] }

    let identityToValidationError (errs:IdentityError seq) =
        errs
        |> Seq.map(fun e -> {Property = e.Code; Errors = [e.Description] })
        |> Seq.toList
 
    let register (model:NewAppUserRequest) : HttpHandler =
        fun next ctx ->
            // TODO Validate model here
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let user = ApplicationUser(UserName = model.Email, Email = model.Email,
                                           Organisation = model.Organisation, GivenNames = model.FirstName,
                                           FamilyName = model.LastName)
                let! result = userManager.CreateAsync(user, model.Password)
                match result.Succeeded with
                | true ->
                    ctx.GetLogger().LogInformation "User created a new account with password."
                    let baseUrl = sprintf "%s://%s%s" ctx.Request.Scheme ctx.Request.Host.Value ctx.Request.PathBase.Value
                    let! code = userManager.GenerateEmailConfirmationTokenAsync(user)
                    let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
                    let returnUrlBase64 = Encoding.UTF8.GetBytes(model.ReturnUrl) |> WebEncoders.Base64UrlEncode
                    let callbackUrl = sprintf "%s/Account/ConfirmEmail?userId=%s&code=%s&returnUrl=%s" baseUrl user.Id codeBase64 returnUrlBase64
                    let html = sprintf "Please confirm your account by following this link: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
                    let sendEmail = ctx.GetService<Email.SendEmail>()
                    sendEmail { To = model.Email; Subject = "Confirm your email"; MessageHtml = html } |> Async.RunSynchronously |> ignore
                    return! htmlView (Views.Pages.confirmCode model.Email) next ctx
                | false -> return! (htmlView <| Views.Pages.register (result.Errors |> identityToValidationError) model) next ctx
            }


module Routes =

    open System.Collections.Generic

    let finish : HttpFunc = Some >> Task.FromResult

    let isValid model =
        let context = ValidationContext(model)
        let validationResults = List<ValidationResult>() 
        Validator.TryValidateObject(model,context,validationResults,true)

    let requiresValidModel (error:'a->HttpHandler) model : HttpHandler =
        fun next ctx ->
            match isValid model with
            | false -> (error model) finish ctx
            | true  -> next ctx


    let error : ErrorHandler =
        fun x logger ->
            logger.LogError(sprintf "There was an error!: %A" x.Message)    
            text <| sprintf "There was an error!: %A" x.Message

    let register request : HttpHandler =
        fun next ctx -> Handlers.register request next ctx

    let returnToOriginalApp (model:ReturnUrlQuery) : HttpHandler =
        fun next ctx ->
            let redirectSvc = ctx.GetService<Redirect.IRedirectService>()
            let uri = redirectSvc.ExtractRedirectUriFromReturnUrl model.ReturnUrl
            redirectTo true uri next ctx

    let identityError errorId : HttpHandler =
        fun next ctx ->  
            task {
                let interaction = ctx.GetService<IIdentityServerInteractionService>()
                let! error = interaction.GetErrorContextAsync(errorId)
                if isNotNull error
                then return! htmlView (Views.Pages.error error.ErrorDescription) next ctx
                else return! htmlView (Views.Pages.error "Unknown error") next ctx
            }

    let buildLoginViewModel returnUrl (context:AuthorizationRequest) (clientStore:IClientStore) =
        task {
//            let allowLocal =
//                if isNotNull context.ClientId
//                then 
//                    let! client = clientStore.FindClientByIdAsync(context.ClientId)
//                    if isNotNull client
//                    then client.EnableLocalLogin
//                    else true
//                else true
            return { ReturnUrl = returnUrl; Email = context.LoginHint; Password = ""; RememberMe = false }
        }

    let login (model:ReturnUrlQuery) : HttpHandler =
        fun next ctx ->
            task {
                let interaction = ctx.GetService<IIdentityServerInteractionService>()
                if not <| interaction.IsValidReturnUrl model.ReturnUrl
                then return! RequestErrors.BAD_REQUEST "ReturnUrl was not valid" next ctx
                else
                    let! context = interaction.GetAuthorizationContextAsync(model.ReturnUrl)
                    if not <| String.IsNullOrEmpty context.IdP
                    then return! invalidOp "Not implemented (external login)"
                    else
                        let clientStore = ctx.GetService<IClientStore>()
                        let! vm = buildLoginViewModel model.ReturnUrl context clientStore
                        return! htmlView (Views.Pages.login vm) next ctx
            }

    let loginPost (model:LoginRequest) : HttpHandler =
        fun next ctx ->
            task {
                let loginService = ctx.GetService<Login.ILoginService<ApplicationUser>>()
                let! user = loginService.FindByUsername model.Email
                let! isValid = loginService.ValidateCredentials user model.Password
                if isValid then
                    let tokenLifetime = 120.
                    let props = AuthenticationProperties()
                    props.ExpiresUtc <- DateTimeOffset.UtcNow.AddMinutes tokenLifetime |> Nullable
                    props.AllowRefresh <- true |> Nullable
                    props.RedirectUri <- model.ReturnUrl
                    if model.RememberMe then
                        let permanentTokenLifetime = 365.
                        props.ExpiresUtc <- DateTimeOffset.UtcNow.AddDays permanentTokenLifetime |> Nullable
                        props.IsPersistent <- true
                    do! loginService.SignInAsync user props
                    let interaction = ctx.GetService<IIdentityServerInteractionService>()
                    if interaction.IsValidReturnUrl model.ReturnUrl then
                        return! redirectTo true model.ReturnUrl next ctx
                    else
                        return! redirectTo true "~/" next ctx
                else return! htmlView (Views.Pages.login model) next ctx
            }

    let confirm (model:ConfirmEmailRequest) : HttpHandler =
        fun next ctx -> 
            task {
                if String.IsNullOrEmpty(model.UserId) || String.IsNullOrEmpty(model.Code) 
                then return! htmlView (Views.Pages.error "There was an unexpected error with your confirmation email") next ctx
                else
                    let manager = ctx.GetService<UserManager<ApplicationUser>>()
                    let! user = manager.FindByIdAsync(model.UserId) |> Async.AwaitTask
                    if isNull user then return! htmlView (Views.Pages.error "There was an unexpected error with your confirmation email") next ctx
                    else
                        let decodedCode = WebEncoders.Base64UrlDecode(model.Code) |> System.Text.Encoding.UTF8.GetString
                        let! result = manager.ConfirmEmailAsync(user,decodedCode)
                        if result.Succeeded
                        then 
                            // Login automatically
                            let loginService = ctx.GetService<Login.ILoginService<ApplicationUser>>()
                            let tokenLifetime = 120.
                            let props = AuthenticationProperties()
                            props.ExpiresUtc <- DateTimeOffset.UtcNow.AddMinutes tokenLifetime |> Nullable
                            props.AllowRefresh <- true |> Nullable
                            props.RedirectUri <- model.ReturnUrl
                            do! loginService.SignInAsync user props
                            let interaction = ctx.GetService<IIdentityServerInteractionService>()
                            let decodedReturnUrl = WebEncoders.Base64UrlDecode(model.ReturnUrl) |> System.Text.Encoding.UTF8.GetString
                            if interaction.IsValidReturnUrl decodedReturnUrl then
                                return! redirectTo true decodedReturnUrl next ctx
                            else
                                return! htmlView (Views.Pages.error "Your account is now active, but an error occurred returning you to the application.") next ctx
                        else return! htmlView (Views.Pages.error "The code was not valid. Please resend your email verification request.") next ctx
            }

    let challengeWithProperties (authScheme : string) properties _ (ctx : HttpContext) =
        task {
            do! ctx.ChallengeAsync(authScheme,properties)
            return Some ctx }


    let externalLogin (model:ExternalLoginRequest) : HttpHandler = 
        fun next ctx ->
            task {
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let returnUrl = "/Account/ExternalLoginCallback"
                let properties = signInManager.ConfigureExternalAuthenticationProperties(model.Provider,returnUrl)
                return! challengeWithProperties model.Provider properties next ctx
            }

    open System.Security.Claims

    let externalLoginCallback returnUrl next (ctx:HttpContext) =
        task {
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            let! info = signInManager.GetExternalLoginInfoAsync()
            if isNull info then return! redirectTo false "/Account/Login" next ctx
            else
                let! result = signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false)
                if result.Succeeded then return! redirectTo false returnUrl next ctx
                else if result.IsLockedOut then return! htmlView (Views.Pages.error "You are locked out of your account. Please contact us for more information.") next ctx
                else
                    let email = info.Principal.FindFirst(ClaimTypes.Email)
                    let firstName = 
                        if isNull (info.Principal.FindFirst(ClaimTypes.GivenName))
                            then info.Principal.FindFirst ClaimTypes.Name
                            else info.Principal.FindFirst ClaimTypes.GivenName
                    let lastName = info.Principal.FindFirst ClaimTypes.Surname
                    ctx.Items.Add ("ReturnUrl","returnUrl")
                    ctx.Items.Add ("LoginProvider",info.LoginProvider)
                    let model : ExternalLoginConfirmationViewModel = {
                        Email = email.Value
                        FirstName = firstName.Value
                        LastName = lastName.Value
                        Title = ""
                        Organisation = ""
                        EmailConfirmation = ""
                        ReturnUrl = ""
                    }
                    return! htmlView (Views.Pages.externalRegistration info.LoginProvider [] model) next ctx
        }


    let parsingError (err : string) = RequestErrors.BAD_REQUEST err

    let manageRoutes =
        choose [
            GET  >=> route "/"                       >=> Manage.index
            POST >=> route "/LinkLogin"              >=> Manage.linkLogin
            GET  >=> route "/LinkLoginCallback"      >=> Manage.linkLoginCallback
            GET  >=> route "/ManageLogins"           >=> Manage.manageLogins
            POST >=> route "/SetPassword"            >=> Manage.setPassword
            GET  >=> route "/SetPassword"            >=> htmlView (Views.Pages.Manage.setPassword [] Empty.setPass)
            POST >=> route "/ChangePassword"         >=> Manage.changePassword
            GET  >=> route "/ChangePassword"         >=> htmlView (Views.Pages.Manage.changePassword [] Empty.changePass)
            POST >=> route "/RemoveLogin"            >=> Manage.removeLogin
            GET  >=> route "/RemoveLogin"            >=> Manage.removeLoginView
        ]
    
    let handler =
        choose [
            GET  >=> route "/Account/Login"                 >=> tryBindQuery parsingError None login
            POST >=> route "/Account/Login"                 >=> tryBindForm parsingError None loginPost
            GET  >=> route "/Account/ExternalLogin"         >=> tryBindQuery parsingError None externalLogin
            GET  >=> route "/Account/ExternalLoginCallback" >=> tryBindQuery parsingError None externalLoginCallback
            GET  >=> route "/Account/Register"              >=> tryBindQuery parsingError None (fun (m:ReturnUrlQuery) -> htmlView (Views.Pages.register [] (ViewModels.Empty.newAppUserRequest m.ReturnUrl)))
            POST >=> route "/Account/Register"              >=> tryBindForm parsingError None Handlers.register
            GET  >=> route "/Account/ConfirmEmail"          >=> tryBindQuery parsingError None confirm
            GET  >=> route "/Account/ForgotPassword"        >=> tryBindQuery parsingError None confirm
            subRoute "/Manage" manageRoutes
        ]


// ---------------------------------
// Config and Main
// ---------------------------------
module Program = 

    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Cors.Infrastructure
    open Microsoft.AspNetCore.Hosting
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Configuration
    open Microsoft.EntityFrameworkCore
    open IdentityServer4.EntityFramework.Mappers

    let getAppSetting (appSettings:IConfiguration) name =
        match String.IsNullOrEmpty appSettings.[name] with
        | true -> invalidOp "Appsetting is missing: " + name
        | false -> appSettings.[name]

    module Config =

        open IdentityServer4
        
        let apis = [
            ApiResource("core", "Core Pollen Services")
            ApiResource("webapigw", "Website Aggregator")
        ]

        let resources : IdentityResource list = [
            IdentityResources.OpenId()
            IdentityResources.Profile()
        ]
        
        let clients (mvcSecret:string) websiteUrl (labAppSecret:string) labAppUrl = [
            Client(
                ClientId = "mvc",
                ClientName = "MVC Client",
                ClientSecrets = [| Secret(mvcSecret.Sha256()) |],
                ClientUri = websiteUrl,
                AllowedGrantTypes = [| GrantType.Hybrid |],
                AllowAccessTokensViaBrowser = false,
                RequireConsent = false,
                AllowOfflineAccess = true,
                AlwaysIncludeUserClaimsInIdToken = true,
                RedirectUris = [| sprintf "%s/signin-oidc" websiteUrl  |],
                PostLogoutRedirectUris = [| sprintf "%s/signout-callback-oidc" websiteUrl |],
                AllowedScopes = [|
                    IdentityServerConstants.StandardScopes.OpenId
                    IdentityServerConstants.StandardScopes.Profile
                    IdentityServerConstants.StandardScopes.OfflineAccess
                    "core"
                    "webapigw"
                |])
            Client(
                ClientId = "lab-ui",
                ClientName = "Lab Bolero Client",
                ClientSecrets = [| Secret(labAppSecret.Sha256()) |],
                ClientUri = labAppUrl,
                AllowedGrantTypes = [| GrantType.Hybrid |],
                AllowAccessTokensViaBrowser = false,
                RequireConsent = false,
                AllowOfflineAccess = true,
                AlwaysIncludeUserClaimsInIdToken = true,
                RedirectUris = [| sprintf "%s/signin-oidc" labAppUrl  |],
                PostLogoutRedirectUris = [| sprintf "%s/signout-callback-oidc" labAppUrl |],
                AllowedScopes = [|
                    IdentityServerConstants.StandardScopes.OpenId
                    IdentityServerConstants.StandardScopes.Profile
                    IdentityServerConstants.StandardScopes.OfflineAccess
                    "core"
                    "webapigw"
                |])
        ]
    
    // Seed configuration data
    let seedConfigurationDbAsync (appSettings:IConfiguration) (context:ConfigurationDbContext) =
        task {
            let! hasClients = context.Clients.AnyAsync()
            let! hasIdentityResources = context.IdentityResources.AnyAsync()
            let! hasApiResources = context.ApiResources.AnyAsync()
            if not hasClients then
                let webUrl = getAppSetting appSettings "WebsiteUrl"
                let mvcSecret = getAppSetting appSettings "ClientSecretMvc"
                let labUrl = getAppSetting appSettings "LabWebsiteUrl"
                let labSecret = getAppSetting appSettings "ClientSecretLabBolero"
                let clients = Config.clients mvcSecret webUrl labSecret labUrl
                clients |> List.map(fun c -> context.Clients.Add(c.ToEntity())) |> ignore
                let! _ = context.SaveChangesAsync()
                ()
            if not hasIdentityResources then
                Config.resources |> List.map(fun r -> context.IdentityResources.Add(r.ToEntity())) |> ignore
                let! _ = context.SaveChangesAsync()
                ()
            if not hasApiResources then
                Config.apis |> List.map(fun r -> context.ApiResources.Add(r.ToEntity())) |> ignore
                let! _ = context.SaveChangesAsync()
                ()
        } :> Task

    let ensureRoles appSettings (serviceProvider:IServiceProvider) =
        let roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>()
        let userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>()
        [ "Admin"; "Curator" ]
        |> List.iter (fun roleName ->
            match roleManager.RoleExistsAsync(roleName) |> Async.AwaitTask |> Async.RunSynchronously with
            | true -> ()
            | false -> roleManager.CreateAsync(IdentityRole(roleName)) 
                       |> Async.AwaitTask 
                       |> Async.RunSynchronously 
                       |> ignore )
        let powerUser = ApplicationUser(UserName = getAppSetting appSettings "UserSettings:UserEmail",Email = getAppSetting appSettings "UserSettings:UserEmail",
                                        GivenNames = "Pollen", FamilyName = "Admin", Organisation = "Global Pollen Project")
        let powerPassword = getAppSetting appSettings "UserSettings:UserPassword"
        let existing = userManager.FindByEmailAsync(getAppSetting appSettings "UserSettings:UserEmail") |> Async.AwaitTask |> Async.RunSynchronously
        if existing |> isNull 
            then
                let create = userManager.CreateAsync(powerUser, powerPassword) |> Async.AwaitTask |> Async.RunSynchronously
                if create.Succeeded 
                    then 
                        userManager.AddToRoleAsync(powerUser, "Admin") |> Async.AwaitTask |> Async.RunSynchronously |> ignore
                        let token = userManager.GenerateEmailConfirmationTokenAsync(powerUser) |> Async.AwaitTask |> Async.RunSynchronously
                        userManager.ConfirmEmailAsync(powerUser, token) |> Async.AwaitTask |> Async.RunSynchronously |> ignore
                    else failwithf "Could not create default user account: %A" create.Errors
            else ()

    let configureCors (builder : CorsPolicyBuilder) =
        builder//.WithOrigins("http://localhost:8080")
               .AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               |> ignore

    let sendEmail appSettings message = 
        message |> Email.Cloud.sendAsync (getAppSetting appSettings "SendGridKey") (getAppSetting appSettings "EmailFromName") (getAppSetting appSettings "EmailFromAddress")

    let configureApp (app : IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
        // use serviceScope = app.ApplicationServices.CreateScope()
        // let context = serviceScope.ServiceProvider.GetService<UserDbContext>()
        // context.Database.Migrate()
        if env.IsDevelopment() 
            then app.UseDeveloperExceptionPage() |> ignore
            else app.UseGiraffeErrorHandler Routes.error |> ignore
        app.UseCors(configureCors) |> ignore
        app.UseIdentityServer() |> ignore
        app.UseStaticFiles() |> ignore
        app.UseGiraffe Routes.handler |> ignore

    let configureServices (services : IServiceCollection) =

        let appSettings = services.BuildServiceProvider().GetService<IConfiguration>()

        services.AddCors()  |> ignore
        services.AddSingleton<UserDbContext>() |> ignore
        services.AddSingleton<Email.SendEmail>(sendEmail appSettings) |> ignore
        services.AddIdentity<ApplicationUser, IdentityRole>(fun opt -> 
            opt.SignIn.RequireConfirmedEmail <- true)
            .AddEntityFrameworkStores<UserDbContext>()
            .AddDefaultTokenProviders() |> ignore
        services.AddAuthentication(fun opt ->
            opt.DefaultScheme <- "Cookie")
            .AddFacebook(fun opt ->
                opt.AppId <- getAppSetting appSettings "Authentication:Facebook:AppId"
                opt.AppSecret <- getAppSetting appSettings "Authentication:Facebook:AppSecret")
            .AddTwitter(fun opt ->
                opt.ConsumerKey <- getAppSetting appSettings "Authentication:Twitter:ConsumerKey"
                opt.ConsumerSecret <- getAppSetting appSettings "Authentication:Twitter:ConsumerSecret"
                opt.RetrieveUserDetails <- true) |> ignore
        services.AddTransient<Login.ILoginService<ApplicationUser>, Login.EFLoginService>() |> ignore
        services.AddTransient<Redirect.IRedirectService, Redirect.RedirectService>() |> ignore

        services.AddGiraffe() |> ignore

        services.AddHealthChecks()
            .AddCheck("self", Func<HealthCheckResult> (fun () -> HealthCheckResult.Healthy())) |> ignore

        let connectionString = getAppSetting appSettings "ConnectionStrings:UserConnection"

        services.AddTransient<IProfileService, Profile.ProfileService>() |> ignore
        services.AddIdentityServer(fun config ->
            config.IssuerUri <- "null"
            config.Authentication.CookieLifetime <- TimeSpan.FromHours 2.)
            //.AddSigningCredential
            .AddDeveloperSigningCredential()
            .AddAspNetIdentity<ApplicationUser>()
            .AddConfigurationStore(fun opt ->
                opt.ConfigureDbContext <- (fun builder ->
                    builder.UseSqlServer(connectionString, fun sqlOpt ->
                        sqlOpt.EnableRetryOnFailure(15, TimeSpan.FromSeconds(30.), null) |> ignore
                    ) |> ignore
                )
            )
            .AddOperationalStore(fun opt ->
                opt.ConfigureDbContext <- (fun builder ->
                    builder.UseSqlServer(connectionString, fun sqlOpt ->
                        sqlOpt.EnableRetryOnFailure(15, TimeSpan.FromSeconds(30.), null) |> ignore
                    ) |> ignore
                )
            )
            .AddProfileService<Profile.ProfileService>() |> ignore
        
        let sp = services.BuildServiceProvider()
        let context = sp.GetService<UserDbContext>()
        context.Database.Migrate()
        sp.GetService<ConfigurationDbContext>() |> seedConfigurationDbAsync appSettings |> ignore

        services.BuildServiceProvider() |> ensureRoles appSettings

    let configureLogging (builder : ILoggingBuilder) =
        let filter (l: LogLevel) = l.Equals LogLevel.Error
        builder.AddFilter(filter)
               .AddConsole()
               .AddDebug() |> ignore

    [<EntryPoint>]
    let main args =
        WebHost.CreateDefaultBuilder(args)
            .Configure(Action<IApplicationBuilder> configureApp)
            .ConfigureServices(configureServices)
            .ConfigureLogging(configureLogging)
            .Build()
            .Run()
        0