module Account

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

open Giraffe.Tasks
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

open Microsoft.AspNetCore.Mvc.ModelBinding
open Microsoft.AspNetCore.Authentication
open ModelValidation

let renderView name model = warbler (fun x -> razorHtmlView name model)

let getBaseUrl (ctx:HttpContext) = sprintf "%s://%s:%i" ctx.Request.Scheme ctx.Request.Host.Host ctx.Request.Host.Port.Value

let identityErrorsToModelState (identityResult:IdentityResult) =
    let dict = ModelStateDictionary()
    for error in identityResult.Errors do dict.AddModelError("",error.Description)
    dict

let challengeWithProperties (authScheme : string) properties next (ctx : HttpContext) =
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
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
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
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            let user = ApplicationUser(UserName = model.Email, Email = model.Email)
            let! result = userManager.CreateAsync(user, model.Password)
            match result.Succeeded with
            | true ->
                let id() = Guid.Parse user.Id
                let register = User.register model id
                match register with
                | Error msg -> return! renderView "Account/Register" model next ctx
                | Ok r -> 
                    (ctx.GetLogger()).LogInformation "User created a new account with password."
                    let! code = userManager.GenerateEmailConfirmationTokenAsync(user)
                    let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
                    let callbackUrl = sprintf "%s/Account/ConfirmEmail?userId=%s&code=%s" (getBaseUrl ctx) user.Id codeBase64
                    let html = sprintf "Please confirm your account by following this link: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
                    let! response = sendEmail model.Email "Confirm your email" html
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
                        | Error msg -> return! renderView "Account/ExternalLoginFailure" model next ctx
                        | Ok r -> 
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
                    let! response = sendEmail model.Email "Reset Password" html
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
            let roleManager = ctx.GetService<RoleManager<IdentityRole>>()
            let userManager = ctx.GetService<UserManager<ApplicationUser>>()
            match User.grantCuration id with
            | Ok _ ->
                let! existing = userManager.FindByIdAsync id
                if existing |> isNull 
                    then return! text "Error" next ctx
                    else
                        let! r = userManager.AddToRoleAsync(existing, "Curator")
                        return! redirectTo false "/Admin/Users" next ctx
            | Error e -> return! text "Error" next ctx
        }
