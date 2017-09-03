module GlobalPollenProject.Web.App

open System
open System.IO
open System.Text
open System.Security.Claims
open System.Collections.Generic
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.Identity.EntityFrameworkCore
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection

open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Giraffe.Middleware
open Giraffe.Razor.HttpHandlers
open Giraffe.Razor.Middleware

open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Shared.Identity
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.Shared.Identity.Services
open GlobalPollenProject.App.UseCases
open ReadModels

open FunctionalModelState

/////////////////////////
/// Helpers
/////////////////////////

let errorHandler (ex : Exception) (logger : ILogger) (ctx : HttpContext) =
    logger.LogError(EventId(0), ex, "An unhandled exception has occurred while executing the request.")
    ctx |> (clearResponse >=> setStatusCode 500 >=> text ex.Message)

let authScheme = "Cookie"
let accessDenied = setStatusCode 401 >=> razorHtmlView "AccessDenied" None
let mustBeLoggedIn = requiresAuthentication accessDenied
let mustBeAdmin ctx = requiresRole "Admin" accessDenied ctx

let getBaseUrl (ctx:HttpContext) = sprintf "%s://%s:%i" ctx.Request.Scheme ctx.Request.Host.Host ctx.Request.Host.Port.Value

let currentUserId (ctx:HttpContext) () =
    async {
        let manager = ctx.GetService<UserManager<ApplicationUser>>()
        let! user = manager.GetUserAsync(ctx.User) |> Async.AwaitTask
        return Guid.Parse user.Id
    } |> Async.RunSynchronously

let renderView name model =
    warbler (fun x -> razorHtmlView name model)

let toViewResult view ctx result =
    match result with
    | Ok model -> ctx |> renderView view model
    | Error e -> ctx |> (clearResponse >=> setStatusCode 500 >=> renderView "Error" None)

let notFoundResult ctx =
    ctx |> (clearResponse >=> setStatusCode 400 >=> renderView "NotFound" None)

let toApiResult ctx result =
    match result with
    | Ok list -> json list ctx
    | Error e -> 
        ctx |>
        (setStatusCode 400 >=>
            match e with
            | Validation valErrors -> json <| { Message = "Invalid request"; Errors = valErrors }
            | InvalidRequestFormat -> json <| { Message = "Your request was not in a valid format"; Errors = [] }
            | _ -> json <| { Message = "Internal error"; Errors = [] } )

let bindJson<'a> (ctx:HttpContext) =
    try ctx.BindJson<'a>() |> Async.RunSynchronously |> Ok
    with
    | _ -> Error InvalidRequestFormat

let jsonRequestToApiResponse<'a> appService ctx =
    bindJson<'a> ctx
    |> bind validateModel
    |> Result.bind appService
    |> toApiResult ctx

let queryRequestToApiResponse<'a,'b> (appService:'a->Result<'b,ServiceError>) (ctx:HttpContext) =
    ctx.BindQueryString<'a>()
    |> validateModel
    |> Result.bind appService
    |> toApiResult ctx

/////////////////////////
/// Custom HTTP Handlers
/////////////////////////

let loginHandler redirectUrl =
    fun (ctx: HttpContext) ->
        async {
            let! loginRequest = ctx.BindForm<LoginRequest>()
            
            let isValid,errors = validateModel' loginRequest
            match isValid with
            | false -> return! ctx |> razorHtmlViewWithModelState "Account/Login" errors loginRequest
            | true ->
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! result = signInManager.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, loginRequest.RememberMe, lockoutOnFailure = false) |> Async.AwaitTask
                if result.Succeeded then
                   let logger = ctx.GetLogger()
                   logger.LogInformation "User logged in."
                   return! redirectTo false redirectUrl ctx
                else
                   return! renderView "Account/Login" loginRequest ctx
        }

open Microsoft.AspNetCore.Mvc.ModelBinding

let identityErrorsToModelState (identityResult:IdentityResult) =
    let dict = ModelStateDictionary()
    for error in identityResult.Errors do dict.AddModelError("",error.Description)
    dict

let registerHandler (ctx:HttpContext) =
    async {
        let! model = ctx.BindForm<NewAppUserRequest>()
        let userManager = ctx.GetService<UserManager<ApplicationUser>>()
        let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
        let user = ApplicationUser(UserName = model.Email, Email = model.Email)
        let! result = userManager.CreateAsync(user, model.Password) |> Async.AwaitTask
        match result.Succeeded with
        | true ->
            let id() = Guid.Parse user.Id
            let register = User.register model id
            match register with
            | Error msg -> return! renderView "Account/Register" model ctx
            | Ok r -> 
                (ctx.GetLogger()).LogInformation "User created a new account with password."
                let! code = userManager.GenerateEmailConfirmationTokenAsync(user) |> Async.AwaitTask
                let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
                let callbackUrl = sprintf "%s/Account/ConfirmEmail?userId=%s&code=%s" (getBaseUrl ctx) user.Id codeBase64
                let html = sprintf "Please confirm your account by following this link: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
                let! response = sendEmail model.Email "Reset Password" html
                return! renderView "Account/AwaitingEmailConfirmation" None ctx
        | false -> 
            return! razorHtmlViewWithModelState "Account/Register" (identityErrorsToModelState result) model ctx
    }

let confirmEmailHandler (ctx:HttpContext) =
    async {
        let model = ctx.BindQueryString<ConfirmEmailRequest>()
        if isNull model.Code || isNull model.UserId then return! renderView "Error" None ctx
        else
            let manager = ctx.GetService<UserManager<ApplicationUser>>()
            let! user = manager.FindByIdAsync(model.UserId) |> Async.AwaitTask
            if isNull user then return! renderView "Error" None ctx
            else
                let decodedCode = WebEncoders.Base64UrlDecode(model.Code) |> Encoding.UTF8.GetString
                let! result = manager.ConfirmEmailAsync(user,decodedCode) |> Async.AwaitTask
                if result.Succeeded
                then return! renderView "Account/ConfirmEmail" None ctx
                else return! renderView "Error" None ctx
    }

let challengeWithProperties (authScheme : string) (properties:Authentication.AuthenticationProperties) (ctx : HttpContext) =
    async {
        let auth = ctx.Authentication
        do! auth.ChallengeAsync(authScheme,properties) |> Async.AwaitTask
        return Some ctx }

let externalLoginHandler (ctx:HttpContext) =
    let provider = ctx.BindForm<ExternalLoginRequest>() |> Async.RunSynchronously
    let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    let returnUrl = "/"
    let properties = signInManager.ConfigureExternalAuthenticationProperties(provider.Provider,returnUrl)
    challengeWithProperties provider.Provider properties ctx

let externalLoginCallback returnUrl (ctx:HttpContext) =
    let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    async {
        let! info = signInManager.GetExternalLoginInfoAsync() |> Async.AwaitTask
        if isNull info then return! redirectTo false "Account/Login" ctx
        else
            let! result = signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false) |> Async.AwaitTask
            if result.Succeeded then return! redirectTo false returnUrl ctx
            else if result.IsLockedOut then return! renderView "Account/Lockout" None ctx
            else
                let email = info.Principal.FindFirstValue(ClaimTypes.Email)
                let model : ExternalLoginConfirmationViewModel = {
                    Email = email
                    FirstName = ""
                    LastName = ""
                    Title = ""
                    Organisation = ""
                    EmailConfirmation = ""
                }
                return! renderView "Account/ExternalLoginConfirmation" model ctx
    }

let forgotPasswordHandler (ctx:HttpContext) =
    async {
        let! model = ctx.BindForm<ForgotPasswordViewModel>()
        let isValid,errors = validateModel' model
        match isValid with
        | false -> return! ctx |> razorHtmlViewWithModelState "Account/ForgotPassword" errors model
        | true ->
            let manager = ctx.GetService<UserManager<ApplicationUser>>()
            let! user = manager.FindByNameAsync(model.Email) |> Async.AwaitTask
            let! confirmed = manager.IsEmailConfirmedAsync(user) |> Async.AwaitTask 
            if isNull user || not confirmed
            then return! renderView "Account/ForgotPasswordConfirmation" None ctx
            else
                let! code = manager.GeneratePasswordResetTokenAsync(user) |> Async.AwaitTask
                let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
                let callbackUrl = sprintf "%s/Account/ResetPassword?userId=%s&code=%s" (getBaseUrl ctx) user.Id codeBase64
                let html = sprintf "Please reset your password by clicking here: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
                let! response = sendEmail model.Email "Reset Password" html
                return! renderView "Account/ForgotPasswordConfirmation" None ctx
    }

let resetPasswordView (ctx:HttpContext) =
    match ctx.TryGetQueryStringValue "code" with
    | None -> renderView "Error" None ctx
    | Some c -> 
        let decodedCode = c |> WebEncoders.Base64UrlDecode |> Encoding.UTF8.GetString
        let model = { Email =""; Code = decodedCode; Password = ""; ConfirmPassword = "" }
        renderView "Account/ResetPassword" model ctx

let resetPasswordHandler (ctx:HttpContext) =
    async {
        let! model = ctx.BindForm<ResetPasswordViewModel>()
        let isValid,errors = validateModel' model
        match isValid with
        | false -> return! ctx |> razorHtmlViewWithModelState "Account/ResetPassword" errors model
        | true ->
            let manager = ctx.GetService<UserManager<ApplicationUser>>()
            let! user = manager.FindByNameAsync(model.Email) |> Async.AwaitTask
            if isNull user then return! redirectTo false "Account/ResetPasswordConfirmation" ctx
            else
                let! result = manager.ResetPasswordAsync(user, model.Code, model.Password) |> Async.AwaitTask
                match result.Succeeded with
                | true -> return! redirectTo false "/Account/ResetPasswordConfirmation" ctx
                | false -> return! renderView "Account/ResetPassword" None ctx
    }

let slideViewHandler (id:string) ctx =
    let split = id.Split '/'
    match split.Length with
    | 2 -> 
        let col,slide = split.[0],split.[1]
        Taxonomy.getSlide col slide
        |> toViewResult "MRC/Slide" ctx
    | _ -> notFoundResult ctx

let taxonDetail (taxon:string) ctx =
    let (f,g,s) =
        let split = taxon.Split '/'
        match split.Length with
        | 1 -> split.[0],None,None
        | 2 -> split.[0],Some split.[1],None
        | 3 -> split.[0],Some split.[1],Some split.[2]
        | _ -> "",None,None
    Taxonomy.getByName f g s
    |> toViewResult "MRC/Taxon" ctx

let individualCollectionIndex ctx =
    IndividualReference.list {Page = 1; PageSize = 20}
    |> toViewResult "Reference/Index" ctx

let individualCollection (colId:string) version ctx =
    IndividualReference.getDetail colId version
    |> toViewResult "Reference/View" ctx

let defaultIfNull (req:TaxonPageRequest) =
    match String.IsNullOrEmpty req.Rank with
    | true -> { Page = 1; PageSize = 40; Rank = "Genus"; Lex = "" }
    | false -> req

let pagedTaxonomyHandler (ctx:HttpContext) =
    ctx.BindQueryString<TaxonPageRequest>()
    |> defaultIfNull
    |> Taxonomy.list
    |> toViewResult "MRC/Index" ctx

let listCollectionsHandler ctx =
    Digitise.myCollections (currentUserId ctx)
    |> toApiResult ctx

let startCollectionHandler (ctx:HttpContext) =
    bindJson<StartCollectionRequest> ctx
    |> bind validateModel
    |> Result.bind (Digitise.startNewCollection (currentUserId ctx))
    |> toApiResult ctx

let publishCollectionHandler (ctx:HttpContext) =
    let id = ctx.BindQueryString<IdQuery>().Id
    Digitise.publish (currentUserId ctx) id
    text "Ok" ctx

let addSlideHandler (ctx:HttpContext) =
    bindJson<SlideRecordRequest> ctx
    |> bind validateModel
    |> bind Digitise.addSlideRecord
    |> toApiResult ctx

let addImageHandler (ctx:HttpContext) =
    bindJson<SlideImageRequest> ctx
    |> bind validateModel
    |> Result.bind Digitise.uploadSlideImage
    |> toApiResult ctx

let getCollectionHandler (ctx:HttpContext) =
    ctx.BindQueryString<IdQuery>().Id.ToString()
    |> Digitise.getCollection
    |> toApiResult ctx
    
let getCalibrationsHandler (ctx:HttpContext) =
    Calibrations.getMyCalibrations (currentUserId ctx)
    |> toApiResult ctx

let setupMicroscopeHandler (ctx:HttpContext) =
    bindJson<AddMicroscopeRequest> ctx
    |> bind (Calibrations.setupMicroscope (currentUserId ctx))
    |> toApiResult ctx

let calibrateHandler (ctx:HttpContext) =
    bindJson<CalibrateRequest> ctx
    |> bind Calibrations.calibrateMagnification
    |> toApiResult ctx

let listGrains ctx =
    UnknownGrains.listUnknownGrains()
    |> toViewResult "Identify/Index" ctx

let showGrainDetail id ctx =
    UnknownGrains.getDetail id
    |> toViewResult "Identify/View" ctx

let submitGrainHandler (ctx:HttpContext) =
    bindJson<AddUnknownGrainRequest> ctx
    >>= UnknownGrains.submitUnknownGrain (currentUserId ctx)
    |> toApiResult ctx

let submitIdentificationHandler (ctx:HttpContext) =
    bindJson<IdentifyGrainRequest> ctx
    |> bind (UnknownGrains.identifyUnknownGrain (currentUserId ctx))
    |> toApiResult ctx

/////////////////////////
/// Routes
/////////////////////////

let webApp =
    let publicApi =
        GET >=>
        choose [
            // route   "/backbone/match"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryMatch
            route   "/backbone/trace"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryTrace
            route   "/backbone/search"          >=> queryRequestToApiResponse<BackboneSearchRequest,string list> Backbone.searchNames
            route   "/taxon/search"             >=> queryRequestToApiResponse<TaxonAutocompleteRequest,TaxonAutocompleteItem list> Taxonomy.autocomplete
        ]

    let digitiseApi =
        mustBeLoggedIn >=>
        choose [
            route   "/collection"               >=> getCollectionHandler
            route   "/collection/list"          >=> listCollectionsHandler
            route   "/collection/start"         >=> startCollectionHandler
            route   "/collection/publish"       >=> publishCollectionHandler
            route   "/collection/slide/add"     >=> addSlideHandler
            route   "/collection/slide/addimage">=> addImageHandler
            route   "/calibration/list"         >=> getCalibrationsHandler
            route   "/calibration/use"          >=> setupMicroscopeHandler
            route   "/calibration/use/mag"      >=> calibrateHandler
        ]

    let accountManagement =
        choose [
            POST >=> route  "/Login"                        >=> loginHandler "/"
            POST >=> route  "/ExternalLogin"                >=> externalLoginHandler
            POST >=> route  "/Register"                     >=> registerHandler
            POST >=> route  "/Logout"                       >=> signOff authScheme >=> redirectTo true "/"
            POST >=> route  "/ForgotPassword"               >=> forgotPasswordHandler
            POST >=> route  "/ResetPassword"                >=> resetPasswordHandler

            GET  >=> route  "/Login"                        >=> renderView "Account/Login" None
            GET  >=> route  "/Register"                     >=> renderView "Account/Register" None
            GET  >=> route  "/ResetPassword"                >=> resetPasswordView
            GET  >=> route  "/ResetPasswordConfirmation"    >=> renderView "Account/ResetPasswordConfirmation" None
            GET  >=> route  "/ForgotPassword"               >=> renderView "Account/ForgotPassword" None
            GET  >=> route  "/ConfirmEmail"                 >=> confirmEmailHandler
            GET  >=> route  "/ExternalLoginCallback"        >=> (fun x -> invalidOp "Not implemented")
            GET  >=> route  "/ExternalLoginConfirmation"    >=> (fun x -> invalidOp "Not implemented")
            GET  >=> route  "/LinkLogin"                    >=> (fun x -> invalidOp "Not implemented")
            GET  >=> route  "/LinkLoginCallback"            >=> (fun x -> invalidOp "Not implemented")
            GET  >=> route  "/ManageLogins"                 >=> (fun x -> invalidOp "Not implemented")
            GET  >=> route  "/SetPassword"                  >=> (fun x -> invalidOp "Not implemented")
            GET  >=> route  "/ChangePassword"               >=> (fun x -> invalidOp "Not implemented")
            GET  >=> route  "/RemoveLogin"                  >=> (fun x -> invalidOp "Not implemented")
        ]

    let masterReferenceCollection =
        GET >=> 
        choose [   
            route   ""                          >=> pagedTaxonomyHandler
            routef  "/Slide/%s"                 slideViewHandler
            routef  "/%s"                       taxonDetail
        ]

    let individualRefCollections =
        GET >=>
        choose [
            route ""                            >=> individualCollectionIndex
            routef "/%s/%i"                     (fun (id,v) -> individualCollection id v)
        ]

    let identify =
        choose [
            POST >=> route  "/Upload"           >=> submitGrainHandler
            POST >=> route  "/Identify"         >=> submitIdentificationHandler
            GET  >=> route  ""                  >=> listGrains
            GET  >=> route  "/Upload"           >=> renderView "Identify/Add" None
            GET  >=> routef "/%s"               (fun id -> showGrainDetail id)
        ]

    // Main router
    choose [
        subRoute    "/api/v1"                   publicApi
        subRoute    "/api/v1/digitise"          digitiseApi
        subRoute    "/Account"                  accountManagement
        subRoute    "/Taxon"                    masterReferenceCollection
        subRoute    "/Reference"                individualRefCollections
        subRoute    "/Identify"                 identify
        GET >=> 
        choose [
            route   "/"                         >=> renderView "Home/Index" None
            route   "/Guide"                    >=> renderView "Home/Guide" None
            route   "/Statistics"               >=> renderView "Statistics/Index" None
            route   "/Digitise"                 >=> mustBeLoggedIn >=> renderView "Digitise/Index" None
            route   "/Api"                      >=> renderView "Home/Api" None
            route   "/Tools"                    >=> renderView "Tools/Index" None
        ]
        setStatusCode 404 >=> renderView "NotFound" None 
    ]


/////////////////////////
/// Configuration
/////////////////////////

let fbOpt = FacebookOptions()
fbOpt.AppId <- getAppSetting "Authentication:Facebook:AppId"
fbOpt.AppSecret <- getAppSetting "Authentication:Facebook:AppSecret"

let twitOpt = TwitterOptions()
twitOpt.ConsumerKey <- getAppSetting "Authentication:Twitter:ConsumerKey"
twitOpt.ConsumerSecret <- getAppSetting "Authentication:Twitter:ConsumerSecret"

let configureApp (app : IApplicationBuilder) = 
    app.UseGiraffeErrorHandler(errorHandler)
    app.UseIdentity() |> ignore
    app.UseFacebookAuthentication fbOpt |> ignore
    app.UseTwitterAuthentication twitOpt |> ignore
    app.UseStaticFiles() |> ignore
    app.UseGiraffe(webApp)

let configureServices (services : IServiceCollection) =
    let sp  = services.BuildServiceProvider()
    let env = sp.GetService<IHostingEnvironment>()
    let viewsFolderPath = Path.Combine(env.ContentRootPath, "Views")

    services.AddSingleton<UserDbContext>() |> ignore
    services.AddIdentity<ApplicationUser, IdentityRole>(fun opt -> 
        opt.SignIn.RequireConfirmedEmail <- true
        opt.Cookies.ApplicationCookie.LoginPath <- PathString "/Account/Login"
        opt.Cookies.ApplicationCookie.AuthenticationScheme <- "Cookie"
        opt.Cookies.ApplicationCookie.AutomaticAuthenticate <- true)
        .AddEntityFrameworkStores<UserDbContext>()
        .AddDefaultTokenProviders() |> ignore

    services.AddSingleton<IEmailSender, AuthEmailMessageSender>() |> ignore

    services.AddAuthentication() |> ignore
    services.AddDataProtection() |> ignore
    services.AddRazorEngine(viewsFolderPath) |> ignore

let configureLogging (loggerFactory : ILoggerFactory) =
    loggerFactory.AddConsole(LogLevel.Trace).AddDebug() |> ignore


[<EntryPoint>]
let main argv =
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(Action<IServiceCollection> configureServices)
        .ConfigureLogging(Action<ILoggerFactory> configureLogging)
        .Build()
        .Run()
    0