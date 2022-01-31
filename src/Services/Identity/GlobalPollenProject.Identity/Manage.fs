namespace GlobalPollenProject.Identity

open Giraffe
open Microsoft.AspNetCore.Identity
open GlobalPollenProject.Identity.ViewModels
open System.ComponentModel.DataAnnotations
open System.Collections.Generic

module Helpers =

    open Microsoft.AspNetCore.Http
    open Microsoft.AspNetCore.Authentication
    
    let isValid model =
        let context = ValidationContext(model)
        let validationResults = List<ValidationResult>() 
        Validator.TryValidateObject(model,context,validationResults,true)
    
    let challengeWithProperties (authScheme : string) properties _ (ctx : HttpContext) =
        task {
            do! ctx.ChallengeAsync(authScheme,properties)
            return Some ctx }

    let getBaseUrl (ctx:HttpContext) = 
        let port = 
            if ctx.Request.Host.Port.HasValue 
            then sprintf ":%i" ctx.Request.Host.Port.Value
            else ""
        sprintf "%s://%s%s" ctx.Request.Scheme ctx.Request.Host.Host port


module Manage =
    
    let index : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                let! hasPassword = userManager.HasPasswordAsync(user)
                let! logins = userManager.GetLoginsAsync(user)
                let model = { HasPassword = hasPassword; Logins = logins |> Seq.toList }
                let username = userManager.GetUserName(ctx.User)
                return! htmlView (Views.Pages.Manage.index username model) next ctx
            }
    
    let removeLoginView : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                let! linkedAccounts = userManager.GetLoginsAsync user
                let! hasPass = userManager.HasPasswordAsync user
                ctx.Items.Add ("ShowRemoveButton", (hasPass || linkedAccounts.Count > 1))
                return! htmlView (Views.Pages.Manage.removeLogin (linkedAccounts |> Seq.toList)) next ctx
            }
    
    let removeLogin : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                let! model = ctx.BindFormAsync<RemoveLogin>()
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
                let! model = ctx.BindFormAsync<ChangePasswordViewModel>()
                match Helpers.isValid model with
                | false -> return! htmlView (Views.Pages.Manage.changePassword [] model) next ctx
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
                let! model = ctx.BindFormAsync<SetPasswordViewModel>()
                match Helpers.isValid model with
                | false -> return! htmlView (Views.Pages.Manage.setPassword [] model) next ctx
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
                | true -> return! htmlView (Views.Pages.error "") next ctx
                | false ->
                    let! userLogins = userManager.GetLoginsAsync user
                    let! otherLogins = signInManager.GetExternalAuthenticationSchemesAsync()
                    ctx.Items.Add ("ShowRemoveButton", (not (isNull user.PasswordHash)) || userLogins.Count > 1)
                    let model = { CurrentLogins = userLogins |> Seq.toList; OtherLogins = otherLogins |> Seq.toList }
                    return! htmlView (Views.Pages.Manage.manageLogins model) next ctx
            }

    let linkLogin : HttpHandler =
        fun next ctx ->
            let userManager = ctx.GetService<UserManager<ApplicationUser>>()
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            let provider = ctx.BindFormAsync<LinkLogin>() |> Async.AwaitTask |> Async.RunSynchronously
            let user = userManager.GetUserAsync ctx.User |> Async.AwaitTask |> Async.RunSynchronously
            let callbackUrl = sprintf "%s/Account/Manage/LinkLoginCallback" (Helpers.getBaseUrl ctx)
            let properties = signInManager.ConfigureExternalAuthenticationProperties(provider.Provider,callbackUrl, user.Id)
            Helpers.challengeWithProperties provider.Provider properties next ctx

    let linkLoginCallback : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                match isNull user with
                | true -> return! htmlView (Views.Pages.error "") next ctx
                | false ->
                    let! info = signInManager.GetExternalLoginInfoAsync()
                    match isNull info with
                    | true -> return! htmlView (Views.Pages.error "") next ctx
                    | false ->
                        let! res = userManager.AddLoginAsync(user,info)
                        match res.Succeeded with 
                        | false -> return! htmlView (Views.Pages.error "") next ctx
                        | true -> return! redirectTo true "/Account/cool" next ctx
            }
