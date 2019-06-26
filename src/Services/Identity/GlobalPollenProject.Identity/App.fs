namespace GlobalPollenProject.Identity

open System
open FSharp.Control.Tasks.V2.ContextInsensitive
open Giraffe
open GlobalPollenProject.Identity.Contract
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




// Manage

    // open System.ComponentModel.DataAnnotations

    // [<CLIMutable>]
    // type IndexViewModel = {
    //     HasPassword: bool
    //     Logins: UserLoginInfo list
    //     Profile: PublicProfile }

    // [<CLIMutable>]
    // type ManageLoginsViewModel = {
    //     CurrentLogins: UserLoginInfo list
    //     OtherLogins: AuthenticationScheme list }

    // [<CLIMutable>]
    // type LinkLogin = { Provider: string }

    // [<CLIMutable>]
    // type RemoveLogin = { LoginProvider: string; ProviderKey: string }

    // [<CLIMutable>]
    // type SetPasswordViewModel = {
    //     [<Required>] 
    //     [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
    //     [<DataType(DataType.Password)>]
    //     [<Display(Name="New password")>]
    //     NewPassword: string
    //     [<DataType(DataType.Password)>]
    //     [<Display(Name = "Confirm new password")>]
    //     [<Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")>]
    //     ConfirmPassword: string
    // }

    // [<CLIMutable>]
    // type ChangePasswordViewModel = {
    //     [<Required>] 
    //     [<DataType(DataType.Password)>]
    //     [<Display(Name="Current password")>]
    //     OldPassword: string
    //     [<Required>] 
    //     [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
    //     [<DataType(DataType.Password)>]
    //     [<Display(Name="New password")>]
    //     NewPassword: string
    //     [<DataType(DataType.Password)>]
    //     [<Display(Name = "Confirm new password")>]
    //     [<Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")>]
    //     ConfirmPassword: string
    // }


    // let index : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let! user = userManager.GetUserAsync ctx.User
    //             let! hasPassword = userManager.HasPasswordAsync(user)
    //             let! logins = userManager.GetLoginsAsync(user)
    //             let createVm p = 
    //                 { HasPassword = hasPassword
    //                   Logins = logins |> Seq.toList
    //                   Profile = p }
    //             let model = createVm <!> User.getPublicProfile (user.Id |> Guid)
    //             match model with
    //             | Ok m -> return! htmlView (HtmlViews.Manage.index m) next ctx
    //             | Error _ -> return! htmlView HtmlViews.StatusPages.error next ctx
    //         }
    
    // let removeLoginView : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let! user = userManager.GetUserAsync ctx.User
    //             let! linkedAccounts = userManager.GetLoginsAsync user
    //             let! hasPass = userManager.HasPasswordAsync user
    //             ctx.Items.Add ("ShowRemoveButton", (hasPass || linkedAccounts.Count > 1))
    //             return! htmlView (HtmlViews.Manage.removeLogin linkedAccounts) next ctx
    //         }
    
    // let removeLogin : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //             let! user = userManager.GetUserAsync ctx.User
    //             let! model = ctx.BindFormAsync<RemoveLogin>()
    //             match isNull user with
    //             | true -> return! redirectTo true "/Account/Manage" next ctx
    //             | false ->
    //                 let! result = userManager.RemoveLoginAsync(user,model.LoginProvider,model.ProviderKey)
    //                 match result.Succeeded with
    //                 | false -> ()
    //                 | true -> do! signInManager.SignInAsync(user, isPersistent = false)
    //                 return! redirectTo true "/Account/Manage" next ctx
    //         }

    // let changePassword : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //             let! model = ctx.BindFormAsync<ChangePasswordViewModel>()
    //             match isValid model with
    //             | false -> return! htmlView (HtmlViews.Manage.changePassword [] model) next ctx
    //             | true ->
    //                 let! user = userManager.GetUserAsync ctx.User
    //                 match isNull user with
    //                 | true -> return! redirectTo true "/Account/Manage" next ctx
    //                 | false ->
    //                     let! result = userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword)
    //                     match result.Succeeded with
    //                     | false -> ()
    //                     | true -> do! signInManager.SignInAsync(user, isPersistent = false)
    //                     return! redirectTo true "/Account/Manage" next ctx
    //         }

    // let setPassword : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //             let! model = ctx.BindFormAsync<SetPasswordViewModel>()
    //             match isValid model with
    //             | false -> return! htmlView (HtmlViews.Manage.setPassword [] model) next ctx
    //             | true ->
    //                 let! user = userManager.GetUserAsync ctx.User
    //                 match isNull user with
    //                 | true -> return! redirectTo true "/Account/Manage" next ctx
    //                 | false ->
    //                     let! result = userManager.AddPasswordAsync(user, model.NewPassword)
    //                     match result.Succeeded with
    //                     | false -> ()
    //                     | true -> do! signInManager.SignInAsync(user, isPersistent = false)
    //                     return! redirectTo true "/Account/Manage" next ctx
    //         }

    // let manageLogins : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //             let! user = userManager.GetUserAsync ctx.User
    //             match isNull user with
    //             | true -> return! htmlView HtmlViews.StatusPages.error next ctx
    //             | false ->
    //                 let! userLogins = userManager.GetLoginsAsync user
    //                 let! otherLogins = signInManager.GetExternalAuthenticationSchemesAsync()
    //                 ctx.Items.Add ("ShowRemoveButton", (not (isNull user.PasswordHash)) || userLogins.Count > 1)
    //                 let model = { CurrentLogins = userLogins |> Seq.toList; OtherLogins = otherLogins |> Seq.toList }
    //                 return! htmlView (HtmlViews.Manage.manageLogins model) next ctx
    //         }

    // let linkLogin : HttpHandler =
    //     fun next ctx ->
    //         let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //         let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //         let provider = ctx.BindFormAsync<LinkLogin>() |> Async.AwaitTask |> Async.RunSynchronously
    //         let user = userManager.GetUserAsync ctx.User |> Async.AwaitTask |> Async.RunSynchronously
    //         let callbackUrl = sprintf "%s/Account/Manage/LinkLoginCallback" (Urls.getBaseUrl ctx)
    //         let properties = signInManager.ConfigureExternalAuthenticationProperties(provider.Provider,callbackUrl, user.Id)
    //         Identity.challengeWithProperties provider.Provider properties next ctx

    // let linkLoginCallback : HttpHandler =
    //     fun next ctx ->
    //         task {
    //             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
    //             let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //             let! user = userManager.GetUserAsync ctx.User
    //             match isNull user with
    //             | true -> return! htmlView HtmlViews.StatusPages.error next ctx
    //             | false ->
    //                 let! info = signInManager.GetExternalLoginInfoAsync()
    //                 match isNull info with
    //                 | true -> return! htmlView HtmlViews.StatusPages.error next ctx
    //                 | false ->
    //                     let! res = userManager.AddLoginAsync(user,info)
    //                     match res.Succeeded with 
    //                     | false -> return! htmlView HtmlViews.StatusPages.error next ctx
    //                     | true -> return! redirectTo true "/Account/cool" next ctx
    //         }



module UserService =

    open System.Text
    open Microsoft.IdentityModel.Tokens
    open System.IdentityModel.Tokens.Jwt
    open System.Security.Claims
    open Microsoft.AspNetCore.WebUtilities
    open GlobalPollenProject.Shared

    let identityToValidationError' (e:IdentityError) =
        {Property = e.Code; Errors = [e.Description] }

    let identityToValidationError (errs:IdentityError seq) =
        errs
        |> Seq.map(fun e -> {Property = e.Code; Errors = [e.Description] })
        |> Seq.toList


    let secret = "3ce1637ed40041cd94d4853d3e766c4d"

    // let login onError loginRequest : HttpHandler = 
    //     fun next ctx ->
    //         task {
    //             let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
    //             let! result = signInManager.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, loginRequest.RememberMe, lockoutOnFailure = false)
                // if result.Succeeded then
                //    let logger = ctx.GetLogger()
                //    logger.LogInformation "User logged in."
                //    return! next ctx
                // else
                //    return! (onError [] loginRequest) finish ctx
    //         }

    let authenticate username password : HttpHandler =
        fun next ctx ->
            task {
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = userManager.FindByNameAsync username
                let! result = signInManager.PasswordSignInAsync(user, password, false, false)
                if result.Succeeded then
                    let logger = ctx.GetLogger()
                    logger.LogInformation "User logged in."

                    let tokenHandler = JwtSecurityTokenHandler()
                    let key = Encoding.ASCII.GetBytes secret

                    let claims = [
                        Claim("name", user.UserName)
                    ]

                    let tokenDescriptor = SecurityTokenDescriptor(Subject = ClaimsIdentity claims, Expires = Nullable<DateTime>(DateTime.UtcNow.AddDays 7.), SigningCredentials = SigningCredentials(SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature))
                    let token = tokenHandler.CreateToken tokenDescriptor
                    let jwtSecurityToken = tokenHandler.WriteToken token
                    return! json { auth_token = jwtSecurityToken } next ctx
                else return! json "Failed!" next ctx
            }

    let register (model:NewAppUserRequest) : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let user = ApplicationUser(UserName = model.Email, Email = model.Email)
                let! result = userManager.CreateAsync(user, model.Password)
                printfn "%A" result
                match result.Succeeded with
                | true ->
                    let id() = Guid.Parse user.Id
                    ctx.GetLogger().LogInformation "User created a new account with password."
                    let! code = userManager.GenerateEmailConfirmationTokenAsync(user)
                    let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
                    let returnUrlBase64 = Encoding.UTF8.GetBytes(model.ReturnUrl) |> WebEncoders.Base64UrlEncode
                    let callbackUrl = sprintf "http://localhost:5000/Account/ConfirmEmail?userId=%s&code=%s&returnUrl=%s" user.Id codeBase64 returnUrlBase64
                    let html = sprintf "Please confirm your account by following this link: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
                    let sendEmail = ctx.GetService<Email.SendEmail>()
                    sendEmail { To = model.Email; Subject = "Confirm your email"; MessageHtml = html } |> Async.RunSynchronously |> ignore
                    return! htmlView (Views.Pages.confirmCode model.Email) next ctx
                | false -> return! (htmlView <| Views.Pages.register (result.Errors |> identityToValidationError) model) next ctx
            }


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


module Login =

    type ILoginService<'T> =
        
        abstract member ValidateCredentials : 'T -> string -> Task<bool>
        abstract member FindByUsername : string -> Task<'T>
        abstract member SignIn : 'T -> Task
        abstract member SignInAsync : 'T -> AuthenticationProperties -> Task

    type EFLoginService(userManager:UserManager<ApplicationUser>, signInManager:SignInManager<ApplicationUser>) =
        interface ILoginService<ApplicationUser> with

            member __.FindByUsername user =
                userManager.FindByEmailAsync(user)

            member __.ValidateCredentials user password =
                userManager.CheckPasswordAsync(user, password)

            member __.SignIn user =
                signInManager.SignInAsync(user, true)

            member __.SignInAsync user properties =
                signInManager.SignInAsync(user, properties)


module Profile =

    open System.Security.Claims
    open System.IdentityModel.Tokens.Jwt

    let appendClaim name prop claims =
        if String.IsNullOrEmpty prop
        then claims
        else Claim(name, prop) :: claims

    let getClaims (user:ApplicationUser) =

        [ Claim(IdentityModel.JwtClaimTypes.Subject, user.Id)
          Claim(IdentityModel.JwtClaimTypes.PreferredUserName, user.UserName)
          Claim(JwtRegisteredClaimNames.UniqueName, user.UserName) ]
        |> appendClaim "name" user.NormalizedEmail //TODO add in all claims here


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
                    context.IsActive <- false
                    if isNotNull user then
                        if userManager.SupportsUserSecurityStamp then
                            let securityStamp = subject.Claims |> Seq.where(fun c -> c.Type = "security_stamp") |> Seq.map (fun c -> c.Value) |> Seq.tryHead
                            match securityStamp with
                            | None -> ()
                            | Some stamp ->
                                let! dbSecurityStamp = userManager.GetSecurityStampAsync(user)
                                if dbSecurityStamp <> stamp
                                then ()
                                else
                                    context.IsActive <-
                                        not user.LockoutEnabled || 
                                        not user.LockoutEnd.HasValue// || 
                                        //user.LockoutEnd <= DateTime.Now
                        else
                            context.IsActive <-
                                not user.LockoutEnabled || 
                                not user.LockoutEnd.HasValue// || 
                                //user.LockoutEnd <= DateTime.Now
                    else ()
                } :> Task


module Routes =

    open System.Collections.Generic

    let finish : HttpFunc = Some >> Task.FromResult

    let isValid model =
        let context = ValidationContext(model)
        let validationResults = new List<ValidationResult>() 
        Validator.TryValidateObject(model,context,validationResults,true)

    let requiresValidModel (error:'a->HttpHandler) model : HttpHandler =
        fun next ctx ->
            match isValid model with
            | false -> (error model) finish ctx
            | true  -> next ctx


    let error : ErrorHandler =
        fun x logger ->  text <| sprintf "There was an error!: %A" x.Message

    let authenticate (userRequest:Login) : HttpHandler =
        fun next ctx -> UserService.authenticate userRequest.Username userRequest.Password next ctx

    let register request : HttpHandler =
        fun next ctx ->
            printfn "Registering"
            UserService.register request next ctx

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
                else return! htmlView (Views.Pages.error "Cool") next ctx
            }

    let buildLoginViewModel returnUrl (context:AuthorizationRequest) (clientStore:IClientStore) =
        task {
            // let allowLocal =
            //     if isNotNull context.ClientId
            //     then 
            //         let! client = clientStore.FindClientByIdAsync(context.ClientId)
            //         if isNotNull client
            //         then client.EnableLocalLogin
            //         else true
            //     else true
            return { ReturnUrl = returnUrl; Email = context.LoginHint; Password = ""; RememberMe = false }
        }

    let login (model:ReturnUrlQuery) : HttpHandler =
        printfn "%A" model
        fun next ctx ->
            task {
                let interaction = ctx.GetService<IIdentityServerInteractionService>()
                if not <| interaction.IsValidReturnUrl model.ReturnUrl
                then return! RequestErrors.BAD_REQUEST "ReturnUrl was not valid" next ctx
                else
                    let! context = interaction.GetAuthorizationContextAsync(model.ReturnUrl)
                    printfn "Interaction is %A" interaction
                    printfn "Context is %A" context
                    if not <| String.IsNullOrEmpty context.IdP
                    then return! invalidOp "Not implemented (external login)"
                    else
                        let clientStore = ctx.GetService<IClientStore>()
                        let! vm = buildLoginViewModel model.ReturnUrl context clientStore
                        return! htmlView (Views.Pages.login vm) next ctx
            }

    let loginPost (model:LoginRequest) : HttpHandler =
        printfn "Model is %A" model
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
                then return! json (Error "Invalid Request") next ctx
                else
                    let manager = ctx.GetService<UserManager<ApplicationUser>>()
                    let! user = manager.FindByIdAsync(model.UserId) |> Async.AwaitTask
                    if isNull user then return! json (Error "User doesn't exist") next ctx
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
                            let decodedReturnUrl = WebEncoders.Base64UrlDecode(model.Code) |> System.Text.Encoding.UTF8.GetString
                            if interaction.IsValidReturnUrl decodedReturnUrl then
                                return! redirectTo true decodedReturnUrl next ctx
                            else
                                return! redirectTo true "/Account/Login" next ctx

                        else return! json (Error "Code was not valid") next ctx
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

    let handler =
        choose [
            GET  >=> route "/Account/Login"         >=> tryBindQuery parsingError None login
            POST >=> route "/Account/Login"         >=> tryBindForm parsingError None loginPost
            GET  >=> route "/Account/ExternalLogin" >=> tryBindQuery parsingError None externalLogin
            GET  >=> route "/Account/ExternalLoginCallback" >=> tryBindQuery parsingError None externalLoginCallback
            GET  >=> route "/Account/Register"      >=> tryBindQuery parsingError None (fun (m:ReturnUrlQuery) -> htmlView (Views.Pages.register [] (ViewModels.Empty.newAppUserRequest m.ReturnUrl)))
            POST >=> route "/Account/Register"      >=> tryBindForm parsingError None UserService.register
            GET  >=> route "/Account/ConfirmEmail"  >=> tryBindQuery parsingError None confirm
        ]

// For external auth, see:
// https://fullstackmark.com/post/13/jwt-authentication-with-aspnet-core-2-web-api-angular-5-net-core-identity-and-facebook-login

    // POST >=> route  Urls.Account.externalLogin        >=> externalLoginHandler
    // // POST >=> route  Urls.Account.externalLoginConf    >=> externalLoginConfirmation
    // POST >=> route  Urls.Account.logout               >=> Authentication.mustBeLoggedIn >=> logoutHandler
    // // POST >=> route  Urls.Account.forgotPassword       >=> mustBeLoggedIn >=> forgotPasswordHandler
    // // POST >=> route  Urls.Account.resetPassword        >=> mustBeLoggedIn >=> resetPasswordHandler
    // // GET  >=> route  Urls.Account.login                >=> htmlView (HtmlViews.Account.login [] Requests.Empty.login)
    // GET  >=> route  Urls.Account.register             >=> htmlView (HtmlViews.Account.register [] Requests.Empty.newAppUserRequest) 
    // GET  >=> route  Urls.Account.resetPassword        >=> resetPasswordView
    // GET  >=> route  Urls.Account.resetPasswordConf    >=> htmlView (HtmlViews.Account.resetPasswordConfirmation)
    // GET  >=> route  Urls.Account.forgotPassword       >=> htmlView (HtmlViews.Account.forgotPassword Requests.Empty.forgotPassword)
    // // GET  >=> route  Urls.Account.confirmEmail         >=> confirmEmailHandler
    // GET  >=> route  Urls.Account.externalLoginCallbk  >=> externalLoginCallback Urls.home

    //         GET  >=> route  "/Manage"                       >=> Manage.index
    //         // POST >=> route  "/Manage/Profile"               >=> Manage.profile
    //         // GET  >=> route  "/Manage/Profile"               >=> htmlView HtmlViews.Manage.changePassword //renderView "Manage/ChangePublicProfile" None
    //         POST >=> route  "/Manage/LinkLogin"             >=> Manage.linkLogin
    //         GET  >=> route  "/Manage/LinkLoginCallback"     >=> Manage.linkLoginCallback
    //         GET  >=> route  "/Manage/ManageLogins"          >=> Manage.manageLogins
    //         POST >=> route  "/Manage/SetPassword"           >=> Manage.setPassword
    //         GET  >=> route  "/Manage/SetPassword"           >=> htmlView (HtmlViews.Manage.setPassword [] 2.)
    //         POST >=> route  "/Manage/ChangePassword"        >=> Manage.changePassword
    //         GET  >=> route  "/Manage/ChangePassword"        >=> htmlView (HtmlViews.Manage.changePassword [] 2.)
    //         POST >=> route  "/Manage/RemoveLogin"           >=> Manage.removeLogin
    //         GET  >=> route  "/Manage/RemoveLogin"           >=> Manage.removeLoginView
    //     ]


// Routes
// module Route =
    // let routes =
    //     choose [
    //         POST >=> route  Urls.Account.externalLogin        >=> externalLoginHandler
    //         POST >=> route  Urls.Account.forgotPassword       >=> mustBeLoggedIn >=> forgotPasswordHandler
    //         POST >=> route  Urls.Account.resetPassword        >=> mustBeLoggedIn >=> resetPasswordHandler
    //         POST >=> route  "/Manage/LinkLogin"             >=> Manage.linkLogin
    //         POST >=> route  "/Manage/SetPassword"           >=> Manage.setPassword
    //         POST >=> route  "/Manage/ChangePassword"        >=> Manage.changePassword
    //         POST >=> route  "/Manage/RemoveLogin"           >=> Manage.removeLogin
    //     ]

module Config =

    open IdentityServer4
    open IdentityServer4.Models
    
    let apis = [
        ApiResource("core", "Core Pollen Services")
        ApiResource("webapigw", "Website Aggregator")
    ]

    let resources : IdentityResource list = [
        IdentityResources.OpenId()
        IdentityResources.Profile()
    ]

    let clients websiteUrl = [
        Client(
            ClientId = "mvc",
            ClientName = "MVC Client",
            ClientSecrets = [| Secret("secret".Sha256()) |],
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
    ]


// ---------------------------------
// Config and Main
// ---------------------------------
module Program = 

    open System.IO
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Cors.Infrastructure
    open Microsoft.AspNetCore.Hosting
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.Extensions.Configuration
    open Microsoft.EntityFrameworkCore

    let appSettings = 
        ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build()

    let getAppSetting name =
        match String.IsNullOrEmpty appSettings.[name] with
        | true -> invalidOp "Appsetting is missing: " + name
        | false -> appSettings.[name]

    open IdentityServer4.EntityFramework.Mappers

    // Seed configuration data
    let seedConfigurationDbAsync (context:ConfigurationDbContext) =
        task {
            let! hasClients = context.Clients.AnyAsync()
            let! hasIdentityResources = context.IdentityResources.AnyAsync()
            let! hasApiResources = context.ApiResources.AnyAsync()
            if not hasClients then
                let webUrl = getAppSetting "WebsiteUrl"
                let clients = Config.clients webUrl
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

    let ensureRoles (serviceProvider:IServiceProvider) =
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

        let powerUser = ApplicationUser(UserName = getAppSetting "UserSettings:UserEmail",Email = getAppSetting "UserSettings:UserEmail" )

        let powerPassword = getAppSetting "UserSettings:UserPassword"
        let existing = userManager.FindByEmailAsync(getAppSetting "UserSettings:UserEmail") |> Async.AwaitTask |> Async.RunSynchronously
        if existing |> isNull 
            then
                let create = userManager.CreateAsync(powerUser, powerPassword) |> Async.AwaitTask |> Async.RunSynchronously
                if create.Succeeded 
                    then userManager.AddToRoleAsync(powerUser, "Admin") |> Async.AwaitTask |> Async.RunSynchronously |> ignore
                    else ()
            else ()

    let configureCors (builder : CorsPolicyBuilder) =
        builder//.WithOrigins("http://localhost:8080")
               .AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               |> ignore

    let sendEmail message = 
        message |> Email.Cloud.sendAsync (getAppSetting "SendGridKey") (getAppSetting "EmailFromName") (getAppSetting "EmailFromAddress")

    let configureApp (app : IApplicationBuilder) =
        let env = app.ApplicationServices.GetService<IHostingEnvironment>()
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

        services.AddCors()  |> ignore
        services.AddSingleton<UserDbContext>() |> ignore
        services.AddSingleton<Email.SendEmail>(sendEmail) |> ignore
        services.AddIdentity<ApplicationUser, IdentityRole>(fun opt -> 
            opt.SignIn.RequireConfirmedEmail <- true)
            .AddEntityFrameworkStores<UserDbContext>()
            .AddDefaultTokenProviders() |> ignore
        services.AddAuthentication(fun opt ->
            opt.DefaultScheme <- "Cookie")
            .AddFacebook(fun opt ->
                opt.AppId <- getAppSetting "Authentication:Facebook:AppId"
                opt.AppSecret <- getAppSetting "Authentication:Facebook:AppSecret")
            .AddTwitter(fun opt ->
                opt.ConsumerKey <- getAppSetting "Authentication:Twitter:ConsumerKey"
                opt.ConsumerSecret <- getAppSetting "Authentication:Twitter:ConsumerSecret"
                opt.RetrieveUserDetails <- true) |> ignore
        services.AddTransient<Login.ILoginService<ApplicationUser>, Login.EFLoginService>() |> ignore
        services.AddTransient<Redirect.IRedirectService, Redirect.RedirectService>() |> ignore

        services.AddGiraffe() |> ignore

        services.AddHealthChecks()
            .AddCheck("self", Func<HealthCheckResult> (fun () -> HealthCheckResult.Healthy())) |> ignore

        let connectionString = getAppSetting "ConnectionStrings:UserConnection"

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
            ) |> ignore
            // .Services.AddTransient<IProfileService, ProfileService>() |> ignore

        let sp = services.BuildServiceProvider()
        let context = sp.GetService<UserDbContext>()
        context.Database.Migrate()
        sp.GetService<ConfigurationDbContext>() |> seedConfigurationDbAsync |> ignore

        services.BuildServiceProvider() |> ensureRoles

    let configureLogging (builder : ILoggingBuilder) =
        let filter (l: LogLevel) = l.Equals LogLevel.Error
        builder.AddFilter(filter)
               .AddConsole()
               .AddDebug() |> ignore

    [<EntryPoint>]
    let main args =
        let contentRoot = Directory.GetCurrentDirectory()
        WebHost
            .CreateDefaultBuilder(args)
            .UseKestrel()
            .UseContentRoot(contentRoot)
            .UseIISIntegration()
            .Configure(Action<IApplicationBuilder> configureApp)
            .ConfigureServices(configureServices)
            .ConfigureLogging(configureLogging)
            .Build()
            .Run()
        0