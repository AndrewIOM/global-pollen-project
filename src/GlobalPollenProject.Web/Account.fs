module Account

open System
open System.Text
open System.Security.Claims
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.WebUtilities
open Microsoft.Extensions.Logging

open Giraffe.Tasks
open Giraffe.HttpContextExtensions
open Giraffe.HttpHandlers
open Giraffe.Razor.HttpHandlers

open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Shared.Identity.Models
open GlobalPollenProject.App.UseCases
open ReadModels

open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Authentication
open ModelValidation

let renderView name model = warbler (fun _ -> razorHtmlView name model)

let getBaseUrl (ctx:HttpContext) = 
    let port = 
        if ctx.Request.Host.Port.HasValue 
        then sprintf ":%i" ctx.Request.Host.Port.Value
        else ""
    sprintf "%s://%s%s" ctx.Request.Scheme ctx.Request.Host.Host port

let identityErrorsToModelState (identityResult:IdentityResult) =
    let dict = ModelStateDictionary()
    for error in identityResult.Errors do dict.AddModelError("",error.Description)
    dict

let challengeWithProperties (authScheme : string) properties _ (ctx : HttpContext) =
    task {
        do! ctx.ChallengeAsync(authScheme,properties)
        return Some ctx }

let loginHandler redirectUrl : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! loginRequest = ctx.BindForm<LoginRequest>()
            let isValid,errors = validateModel' loginRequest
            match isValid with
            | false -> return! razorHtmlViewWithModelState "Account/Login" errors loginRequest next ctx
            | true ->
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! result = signInManager.PasswordSignInAsync(loginRequest.Email, loginRequest.Password, loginRequest.RememberMe, lockoutOnFailure = false)
                if result.Succeeded then
                   let logger = ctx.GetLogger()
                   logger.LogInformation "User logged in."
                   return! redirectTo false redirectUrl next ctx
                else
                   return! renderView "Account/Login" loginRequest next ctx
        }

let logoutHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            do! signInManager.SignOutAsync()
            return! (redirectTo false "/") next ctx
}

let registerHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let! model = ctx.BindForm<NewAppUserRequest>()
            let userManager = ctx.GetService<UserManager<ApplicationUser>>()
            let user = ApplicationUser(UserName = model.Email, Email = model.Email)
            let! result = userManager.CreateAsync(user, model.Password)
            match result.Succeeded with
            | true ->
                let id() = Guid.Parse user.Id
                let register = User.register model id
                match register with
                | Error _ -> return! renderView "Account/Register" model next ctx
                | Ok _ -> 
                    (ctx.GetLogger()).LogInformation "User created a new account with password."
                    let! code = userManager.GenerateEmailConfirmationTokenAsync(user)
                    let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
                    let callbackUrl = sprintf "%s/Account/ConfirmEmail?userId=%s&code=%s" (getBaseUrl ctx) user.Id codeBase64
                    let html = sprintf "Please confirm your account by following this link: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
                    let! _ = sendEmail model.Email "Confirm your email" html
                    return! renderView "Account/AwaitingEmailConfirmation" None next ctx
            | false -> 
                return! razorHtmlViewWithModelState "Account/Register" (identityErrorsToModelState result) model next ctx
        }

let confirmEmailHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let model = ctx.BindQueryString<ConfirmEmailRequest>()
            if isNull model.Code || isNull model.UserId then return! renderView "Error" None next ctx
            else
                let manager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = manager.FindByIdAsync(model.UserId)
                if isNull user then return! renderView "Error" None next ctx
                else
                    let decodedCode = WebEncoders.Base64UrlDecode(model.Code) |> Encoding.UTF8.GetString
                    let! result = manager.ConfirmEmailAsync(user,decodedCode)
                    if result.Succeeded
                    then return! renderView "Account/ConfirmEmail" None next ctx
                    else return! renderView "Error" None next ctx
        }

let externalLoginHandler : HttpHandler =
    fun next ctx ->
        task {
            let! provider = ctx.BindForm<ExternalLoginRequest>()
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            let returnUrl = "/Account/ExternalLoginCallback"
            let properties = signInManager.ConfigureExternalAuthenticationProperties(provider.Provider,returnUrl)
            return! challengeWithProperties provider.Provider properties next ctx
        }

let externalLoginCallback returnUrl next (ctx:HttpContext) =
    task {
        let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
        let! info = signInManager.GetExternalLoginInfoAsync()
        if isNull info then return! redirectTo false "Account/Login" next ctx
        else
            let! result = signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false)
            if result.Succeeded then return! redirectTo false returnUrl next ctx
            else if result.IsLockedOut then return! renderView "Account/Lockout" None next ctx
            else
                let email = info.Principal.FindFirstValue(ClaimTypes.Email)
                let firstName = 
                    if isNull (info.Principal.FindFirstValue(ClaimTypes.GivenName))
                        then info.Principal.FindFirstValue ClaimTypes.Name
                        else info.Principal.FindFirstValue ClaimTypes.GivenName
                let lastName = info.Principal.FindFirstValue ClaimTypes.Surname
                ctx.Items.Add ("ReturnUrl","returnUrl")
                ctx.Items.Add ("LoginProvider",info.LoginProvider)
                let model : ExternalLoginConfirmationViewModel = {
                    Email = email
                    FirstName = firstName
                    LastName = lastName
                    Title = ""
                    Organisation = ""
                    EmailConfirmation = ""
                }
                return! renderView "Account/ExternalLoginConfirmation" model next ctx
    }

let externalLoginConfirmation next (ctx:HttpContext) =
    task {
        let! model = ctx.BindForm<ExternalLoginConfirmationViewModel>()
        let userManager = ctx.GetService<UserManager<ApplicationUser>>()
        let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
        let isValid,errors = validateModel' model
        match isValid with
        | false -> return! razorHtmlViewWithModelState "Account/ExternalLoginConfirmation" errors model next ctx
        | true ->
            let! info = signInManager.GetExternalLoginInfoAsync()
            if isNull info then return! redirectTo true "Account/ExternalLoginFailure" next ctx
            else
                let user = ApplicationUser(UserName = model.Email, Email = model.Email)
                let! result = userManager.CreateAsync user
                match result.Succeeded with
                | true ->
                    let! addLoginResult = userManager.AddLoginAsync(user, info)
                    match addLoginResult.Succeeded with
                    | true ->
                        let id() = Guid.Parse user.Id
                        let newUserRequest : NewAppUserRequest = 
                            { Title = model.Title
                              FirstName = model.FirstName
                              LastName = model.LastName
                              Organisation = model.Organisation
                              Email = model.Email
                              EmailConfirmation = model.EmailConfirmation
                              Password = ""
                              ConfirmPassword = "" }
                        let register = User.register newUserRequest id
                        match register with
                        | Error _ -> return! renderView "Account/ExternalLoginFailure" model next ctx
                        | Ok _ -> 
                            (ctx.GetLogger()).LogInformation "User created a new account with password."
                            signInManager.SignInAsync(user, isPersistent = false) |> ignore
                            return! redirectTo true "/" next ctx
                    | false -> return! razorHtmlViewWithModelState "Account/ExternalLoginFailure" (identityErrorsToModelState addLoginResult) model next ctx
                | false -> 
                    return! razorHtmlViewWithModelState "Account/ExternalLoginFailure" (identityErrorsToModelState result) model next ctx
    }

let forgotPasswordHandler : HttpHandler =
    fun next ctx ->
        task {
            let! model = ctx.BindForm<ForgotPasswordViewModel>()
            let isValid,errors = validateModel' model
            match isValid with
            | false -> return! razorHtmlViewWithModelState "Account/ForgotPassword" errors model next ctx
            | true ->
                let manager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = manager.FindByNameAsync(model.Email)
                let! confirmed = manager.IsEmailConfirmedAsync(user)
                if isNull user || not confirmed
                then return! renderView "Account/ForgotPasswordConfirmation" None next ctx
                else
                    let! code = manager.GeneratePasswordResetTokenAsync(user)
                    let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
                    let callbackUrl = sprintf "%s/Account/ResetPassword?userId=%s&code=%s" (getBaseUrl ctx) user.Id codeBase64
                    let html = sprintf "Please reset your password by clicking here: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
                    let! _ = sendEmail model.Email "Reset Password" html
                    return! renderView "Account/ForgotPasswordConfirmation" None next ctx
        }

let resetPasswordView : HttpHandler =
    fun next ctx ->
        match ctx.TryGetQueryStringValue "code" with
        | None -> renderView "Error" None next ctx
        | Some c -> 
            let decodedCode = c |> WebEncoders.Base64UrlDecode |> Encoding.UTF8.GetString
            let model = { Email =""; Code = decodedCode; Password = ""; ConfirmPassword = "" }
            renderView "Account/ResetPassword" model next ctx

let resetPasswordHandler : HttpHandler =
    fun next ctx ->
        task {
            let! model = ctx.BindForm<ResetPasswordViewModel>()
            let isValid,errors = validateModel' model
            match isValid with
            | false -> return! razorHtmlViewWithModelState "Account/ResetPassword" errors model next ctx
            | true ->
                let manager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = manager.FindByNameAsync(model.Email)
                if isNull user then return! redirectTo false "Account/ResetPasswordConfirmation" next ctx
                else
                    let! result = manager.ResetPasswordAsync(user, model.Code, model.Password)
                    match result.Succeeded with
                    | true -> return! redirectTo false "/Account/ResetPasswordConfirmation" next ctx
                    | false -> return! renderView "Account/ResetPassword" None next ctx
        }

let grantCurationHandler (id:string) : HttpHandler =
    fun next ctx ->
        task {
            let userManager = ctx.GetService<UserManager<ApplicationUser>>()
            match User.grantCuration id with
            | Ok _ ->
                let! existing = userManager.FindByIdAsync id
                if existing |> isNull 
                    then return! text "Error" next ctx
                    else
                        let! _ = userManager.AddToRoleAsync(existing, "Curator")
                        return! redirectTo false "/Admin/Users" next ctx
            | Error _ -> return! text "Error" next ctx
        }

module Manage =

    open System.ComponentModel.DataAnnotations

    [<CLIMutable>]
    type IndexViewModel = {
        HasPassword: bool
        Logins: UserLoginInfo list
        Profile: PublicProfile }

    [<CLIMutable>]
    type ManageLoginsViewModel = {
        CurrentLogins: UserLoginInfo list
        OtherLogins: AuthenticationScheme list }

    [<CLIMutable>]
    type LinkLogin = { Provider: string }

    [<CLIMutable>]
    type RemoveLogin = { LoginProvider: string; ProviderKey: string }

    [<CLIMutable>]
    type SetPasswordViewModel = {
        [<Required>] 
        [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
        [<DataType(DataType.Password)>]
        [<Display(Name="New password")>]
        NewPassword: string
        [<DataType(DataType.Password)>]
        [<Display(Name = "Confirm new password")>]
        [<Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")>]
        ConfirmPassword: string
    }

    [<CLIMutable>]
    type ChangePasswordViewModel = {
        [<Required>] 
        [<DataType(DataType.Password)>]
        [<Display(Name="Current password")>]
        OldPassword: string
        [<Required>] 
        [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
        [<DataType(DataType.Password)>]
        [<Display(Name="New password")>]
        NewPassword: string
        [<DataType(DataType.Password)>]
        [<Display(Name = "Confirm new password")>]
        [<Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")>]
        ConfirmPassword: string
    }

    [<CLIMutable>]
    type ChangePublicProfileViewModel = {
        [<Required>] [<Display(Name="Title")>] Title: string
        [<Required>] [<Display(Name="Forename(s)")>] FirstName: string
        [<Required>] [<Display(Name="Surname")>] LastName: string
        [<Required>] [<Display(Name="Organisation")>] Organisation: string
    }

    let index : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                let! hasPassword = userManager.HasPasswordAsync(user)
                let! logins = userManager.GetLoginsAsync(user)
                let createVm p = 
                    { HasPassword = hasPassword
                      Logins = logins |> Seq.toList
                      Profile = p }
                let model = createVm <!> User.getPublicProfile (user.Id |> Guid)
                match model with
                | Ok m -> return! renderView "Manage/Index" m next ctx
                | Error _ -> return! renderView "Error" None next ctx
            }
    
    let removeLoginView : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                let! linkedAccounts = userManager.GetLoginsAsync user
                let! hasPass = userManager.HasPasswordAsync user
                ctx.Items.Add ("ShowRemoveButton", (hasPass || linkedAccounts.Count > 1))
                return! renderView "Manage/RemoveLogin" linkedAccounts next ctx
            }
    
    let removeLogin : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                let! model = ctx.BindForm<RemoveLogin>()
                match isNull user with
                | true -> return! redirectTo true "/Account/Manage" next ctx
                | false ->
                    let! result = userManager.RemoveLoginAsync(user,model.LoginProvider,model.ProviderKey)
                    match result.Succeeded with
                    | false -> ()
                    | true -> do! signInManager.SignInAsync(user, isPersistent = false)
                    return! redirectTo true "/Account/Manage" next ctx
            }

    let changePassword : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! model = ctx.BindForm<ChangePasswordViewModel>()
                let isValid,errors = validateModel' model
                match isValid with
                | false -> return! razorHtmlViewWithModelState "Manage/ChangePassword" errors model next ctx
                | true ->
                    let! user = userManager.GetUserAsync ctx.User
                    match isNull user with
                    | true -> return! redirectTo true "/Account/Manage" next ctx
                    | false ->
                        let! result = userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword)
                        match result.Succeeded with
                        | false -> ()
                        | true -> do! signInManager.SignInAsync(user, isPersistent = false)
                        return! redirectTo true "/Account/Manage" next ctx
            }

    let setPassword : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! model = ctx.BindForm<SetPasswordViewModel>()
                let isValid,errors = validateModel' model
                match isValid with
                | false -> return! razorHtmlViewWithModelState "Manage/SetPassword" errors model next ctx
                | true ->
                    let! user = userManager.GetUserAsync ctx.User
                    match isNull user with
                    | true -> return! redirectTo true "/Account/Manage" next ctx
                    | false ->
                        let! result = userManager.AddPasswordAsync(user, model.NewPassword)
                        match result.Succeeded with
                        | false -> ()
                        | true -> do! signInManager.SignInAsync(user, isPersistent = false)
                        return! redirectTo true "/Account/Manage" next ctx
            }

    let manageLogins : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                match isNull user with
                | true -> return! renderView "Error" None next ctx
                | false ->
                    let! userLogins = userManager.GetLoginsAsync user
                    let! otherLogins = signInManager.GetExternalAuthenticationSchemesAsync()
                    ctx.Items.Add ("ShowRemoveButton", (not (isNull user.PasswordHash)) || userLogins.Count > 1)
                    let model = { CurrentLogins = userLogins |> Seq.toList; OtherLogins = otherLogins |> Seq.toList }
                    return! renderView "Manage/ManageLogins" model next ctx
            }

    let linkLogin : HttpHandler =
        fun next ctx ->
            let userManager = ctx.GetService<UserManager<ApplicationUser>>()
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            let provider = ctx.BindForm<LinkLogin>() |> Async.AwaitTask |> Async.RunSynchronously
            let user = userManager.GetUserAsync ctx.User |> Async.AwaitTask |> Async.RunSynchronously
            let callbackUrl = sprintf "%s/Account/Manage/LinkLoginCallback" (getBaseUrl ctx)
            let properties = signInManager.ConfigureExternalAuthenticationProperties(provider.Provider,callbackUrl, user.Id)
            challengeWithProperties provider.Provider properties next ctx

    let linkLoginCallback : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                match isNull user with
                | true -> return! renderView "Error" None next ctx
                | false ->
                    let! info = signInManager.GetExternalLoginInfoAsync()
                    match isNull info with
                    | true -> return! renderView "Error" None next ctx
                    | false ->
                        let! res = userManager.AddLoginAsync(user,info)
                        match res.Succeeded with 
                        | false -> return! renderView "Error" None next ctx
                        | true -> return! redirectTo true "/Account/cool" next ctx
            }

    let profile : HttpHandler =
        fun next ctx ->
            ctx.BindForm<ChangePublicProfileViewModel>() |> Async.AwaitTask |> Async.RunSynchronously
            |> ignore
            invalidOp "Not implemented"
