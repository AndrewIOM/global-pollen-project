module Account

open System
open System.Text
open Microsoft.Extensions.Logging
open Giraffe
open GlobalPollenProject.Web
open ModelValidation
open ReadModels
open FSharp.Control.Tasks.ContextInsensitive
open Microsoft.AspNetCore.Authentication
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Identity
open Microsoft.AspNetCore.WebUtilities
open System.Security.Principal
open System.Security.Claims
open System.Net.Http

open Connections
open Urls
open Handlers

type IdentityParser() =

    member __.Parse(principal:IPrincipal) = 
        match principal with
        | :? ClaimsPrincipal as claims ->
            { Firstname =  claims.Claims |> Seq.tryFind(fun x -> x.Type = "name")  |> Option.bind(fun x -> Some x.Value)
              Lastname = claims.Claims |> Seq.tryFind(fun x -> x.Type = "lastname") |> Option.bind(fun x -> Some x.Value) }
        | _ -> invalidOp "The principal was not a claims principal"

module Identity =

    let identityToValidationError' (e:IdentityError) =
        {Property = e.Code; Errors = [e.Description] }

    let identityToValidationError (errs:IdentityError seq) =
        errs
        |> Seq.map(fun e -> {Property = e.Code; Errors = [e.Description] })
        |> Seq.toList

    let challengeWithProperties (authScheme : string) properties _ (ctx : HttpContext) =
        task {
            do! ctx.ChallengeAsync(authScheme,properties)
            return Some ctx }

    let login onError loginRequest : HttpHandler = 
        fun next ctx ->
            task {
                let authService = ctx.GetService<AuthenticationService>()
                let! result = authService.Login loginRequest
                match result with
                | Ok _ ->
                   let logger = ctx.GetLogger()
                   logger.LogInformation "User logged in."
                   return! next ctx
                | Error e -> return! (onError [] loginRequest) finish ctx
            }

    let register onError (model:NewAppUserRequest) : HttpHandler =
        fun (next : HttpFunc) (ctx : HttpContext) ->
            task {
                let authService = ctx.GetService<AuthenticationService>()
                let! result = authService.Register model
                match result with
                | Ok s -> return! next ctx
                | Error e -> return! (onError e model) next ctx
            }

///////////////////
/// HTTP Handlers
///////////////////

let bindAndValidate<'T> procedure (onError:ValidationError list -> 'T -> HttpHandler) onComplete : HttpHandler =
    fun next ctx ->
        bindForm<'T> None (fun m -> 
            requiresValidModel (onError []) m
            >=> procedure onError m
            >=> onComplete
        ) next ctx

let htmlViewWithModel view errors vm = view errors vm |> htmlView

let loginHandler : HttpHandler = 
    bindAndValidate 
    <| Identity.login 
    <| htmlViewWithModel HtmlViews.Account.login
    <| redirectTo false Urls.home

let registerHandler : HttpHandler =
    bindAndValidate
    <| Identity.register
    <| htmlViewWithModel HtmlViews.Account.register
    <| htmlView (HtmlViews.Account.awaitingEmailConfirmation)


let externalLoginHandler : HttpHandler =
    fun next ctx ->
        task {
            let! provider = ctx.BindFormAsync<ExternalLoginRequest>()
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            let returnUrl = "/Account/ExternalLoginCallback"
            let properties = signInManager.ConfigureExternalAuthenticationProperties(provider.Provider,returnUrl)
            return! Identity.challengeWithProperties provider.Provider properties next ctx
        }

// let confirmEmailHandler : HttpHandler =
//     fun (next : HttpFunc) (ctx : HttpContext) ->
//         task {
//             let model = ctx.BindQueryString<ConfirmEmailRequest>()
//             if isNull model.Code || isNull model.UserId then return! htmlView HtmlViews.StatusPages.error next ctx
//             else
//                 let manager = ctx.GetService<UserManager<ApplicationUser>>()
//                 let! user = manager.FindByIdAsync(model.UserId)
//                 if isNull user then return! htmlView HtmlViews.StatusPages.error next ctx
//                 else
//                     let decodedCode = WebEncoders.Base64UrlDecode(model.Code) |> Encoding.UTF8.GetString
//                     let! result = manager.ConfirmEmailAsync(user,decodedCode)
//                     if result.Succeeded
//                     then return! htmlView HtmlViews.Account.confirmEmail next ctx
//                     else return! htmlView HtmlViews.StatusPages.error next ctx
//         }

let logoutHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : HttpContext) ->
        task {
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            do! signInManager.SignOutAsync()
            return! (redirectTo false "/") next ctx
}

let externalLoginCallback returnUrl next (ctx:HttpContext) =
    task {
        let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
        let! info = signInManager.GetExternalLoginInfoAsync()
        if isNull info then return! redirectTo false "Account/Login" next ctx
        else
            let! result = signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false)
            if result.Succeeded then return! redirectTo false returnUrl next ctx
            else if result.IsLockedOut then return! htmlView HtmlViews.Account.lockout next ctx
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
                return! htmlView (HtmlViews.Account.externalRegistration info.LoginProvider [] model) next ctx
    }

// let externalLoginConfirmation next (ctx:HttpContext) =
//     task {
//         let! model = ctx.BindFormAsync<ExternalLoginConfirmationViewModel>()
//         let userManager = ctx.GetService<UserManager<ApplicationUser>>()
//         let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
//         match isValid model with
//         | false -> return! htmlView (HtmlViews.Account.externalRegistration "External login" [] model) next ctx
//         | true ->
//             let! info = signInManager.GetExternalLoginInfoAsync()
//             if isNull info then return! redirectTo true Urls.Account.externalLoginFailure next ctx
//             else
//                 let user = ApplicationUser(UserName = model.Email, Email = model.Email)
//                 let! result = userManager.CreateAsync user
//                 match result.Succeeded with
//                 | true ->
//                     let! addLoginResult = userManager.AddLoginAsync(user, info)
//                     match addLoginResult.Succeeded with
//                     | true ->
//                         let id() = Guid.Parse user.Id
//                         let newUserRequest : NewAppUserRequest = 
//                             { Title = model.Title
//                               FirstName = model.FirstName
//                               LastName = model.LastName
//                               Organisation = model.Organisation
//                               Email = model.Email
//                               EmailConfirmation = model.EmailConfirmation
//                               Password = ""
//                               ConfirmPassword = "" }
//                         let register = User.register newUserRequest id
//                         match register with
//                         | Error _ -> return! htmlView (HtmlViews.Account.externalLoginFailure) next ctx
//                         | Ok _ -> 
//                             (ctx.GetLogger()).LogInformation "User created a new account with password."
//                             signInManager.SignInAsync(user, isPersistent = false) |> ignore
//                             return! redirectTo true "/" next ctx
//                     | false -> return! htmlView HtmlViews.Account.externalLoginFailure next ctx
//                 | false -> return! htmlView HtmlViews.Account.externalLoginFailure next ctx
//     }

// let forgotPasswordHandler : HttpHandler =
//     fun next ctx ->
//         task {
//             let! model = ctx.BindFormAsync<ForgotPasswordViewModel>()
//             match isValid model with
//             | false -> return! htmlView (HtmlViews.Account.forgotPassword model) next ctx
//             | true ->
//                 let manager = ctx.GetService<UserManager<ApplicationUser>>()
//                 let! user = manager.FindByNameAsync(model.Email)
//                 let! confirmed = manager.IsEmailConfirmedAsync(user)
//                 if isNull user || not confirmed
//                 then return! htmlView HtmlViews.Account.forgotPasswordConfirmation next ctx
//                 else
//                     let! code = manager.GeneratePasswordResetTokenAsync(user)
//                     let codeBase64 = Encoding.UTF8.GetBytes(code) |> WebEncoders.Base64UrlEncode
//                     let callbackUrl = sprintf "%s/Account/ResetPassword?userId=%s&code=%s" (Urls.getBaseUrl ctx) user.Id codeBase64
//                     let html = sprintf "Please reset your password by clicking here: <a href=\"%s\">%s</a>. You can also copy and paste the address into your browser." callbackUrl callbackUrl
//                     sendEmail model.Email "Reset Password" html |> Async.RunSynchronously |> ignore
//                     return! htmlView HtmlViews.Account.forgotPasswordConfirmation next ctx
//         }

let resetPasswordView : HttpHandler =
    fun next ctx ->
        match ctx.TryGetQueryStringValue "code" with
        | None -> htmlView HtmlViews.StatusPages.error next ctx
        | Some c -> 
            let decodedCode = c |> WebEncoders.Base64UrlDecode |> Encoding.UTF8.GetString
            let model = { Email =""; Code = decodedCode; Password = ""; ConfirmPassword = "" }
            htmlView (HtmlViews.Account.resetPassword model) next ctx

// let resetPasswordHandler : HttpHandler =
//     fun next ctx ->
//         task {
//             let! model = ctx.BindFormAsync<ResetPasswordViewModel>()
//             match isValid model with
//             | false -> return! htmlView (HtmlViews.Account.resetPassword model) next ctx
//             | true ->
//                 let manager = ctx.GetService<UserManager<ApplicationUser>>()
//                 let! user = manager.FindByNameAsync(model.Email)
//                 if isNull user then return! redirectTo false "Account/ResetPasswordConfirmation" next ctx
//                 else
//                     let! result = manager.ResetPasswordAsync(user, model.Code, model.Password)
//                     match result.Succeeded with
//                     | true -> return! redirectTo false "/Account/ResetPasswordConfirmation" next ctx
//                     | false -> return! htmlView (HtmlViews.Account.resetPassword model) next ctx
//         }

// let grantCurationHandler (id:string) : HttpHandler =
//     fun next ctx ->
//         task {
//             let userManager = ctx.GetService<UserManager<ApplicationUser>>()
//             match User.grantCuration id with
//             | Ok _ ->
//                 let! existing = userManager.FindByIdAsync id
//                 if existing |> isNull 
//                     then return! htmlView HtmlViews.StatusPages.error next ctx
//                     else
//                         let! _ = userManager.AddToRoleAsync(existing, "Curator")
//                         return! redirectTo false "/Admin/Users" next ctx
//             | Error _ -> return! htmlView HtmlViews.StatusPages.error next ctx
//         }

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
    
    let removeLoginView : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                let! linkedAccounts = userManager.GetLoginsAsync user
                let! hasPass = userManager.HasPasswordAsync user
                ctx.Items.Add ("ShowRemoveButton", (hasPass || linkedAccounts.Count > 1))
                return! htmlView (HtmlViews.Manage.removeLogin linkedAccounts) next ctx
            }
    
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

    let profile : HttpHandler =
        fun next ctx ->
            ctx.BindFormAsync<ChangePublicProfileViewModel>() |> Async.AwaitTask |> Async.RunSynchronously
            |> ignore
            invalidOp "Not implemented"
