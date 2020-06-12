namespace GlobalPollenProject.Identity

module ViewModels =

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

module Handlers =

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
                | Ok m -> return! htmlView (HtmlViews.Manage.index m) next ctx
                | Error _ -> return! htmlView HtmlViews.StatusPages.error next ctx
            }
    
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
                match isValid model with
                | false -> return! htmlView (HtmlViews.Manage.changePassword [] model) next ctx
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
                match isValid model with
                | false -> return! htmlView (HtmlViews.Manage.setPassword [] model) next ctx
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
                | true -> return! htmlView HtmlViews.StatusPages.error next ctx
                | false ->
                    let! userLogins = userManager.GetLoginsAsync user
                    let! otherLogins = signInManager.GetExternalAuthenticationSchemesAsync()
                    ctx.Items.Add ("ShowRemoveButton", (not (isNull user.PasswordHash)) || userLogins.Count > 1)
                    let model = { CurrentLogins = userLogins |> Seq.toList; OtherLogins = otherLogins |> Seq.toList }
                    return! htmlView (HtmlViews.Manage.manageLogins model) next ctx
            }

    let linkLogin : HttpHandler =
        fun next ctx ->
            let userManager = ctx.GetService<UserManager<ApplicationUser>>()
            let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
            let provider = ctx.BindFormAsync<LinkLogin>() |> Async.AwaitTask |> Async.RunSynchronously
            let user = userManager.GetUserAsync ctx.User |> Async.AwaitTask |> Async.RunSynchronously
            let callbackUrl = sprintf "%s/Account/Manage/LinkLoginCallback" (Urls.getBaseUrl ctx)
            let properties = signInManager.ConfigureExternalAuthenticationProperties(provider.Provider,callbackUrl, user.Id)
            Identity.challengeWithProperties provider.Provider properties next ctx

    let linkLoginCallback : HttpHandler =
        fun next ctx ->
            task {
                let userManager = ctx.GetService<UserManager<ApplicationUser>>()
                let signInManager = ctx.GetService<SignInManager<ApplicationUser>>()
                let! user = userManager.GetUserAsync ctx.User
                match isNull user with
                | true -> return! htmlView HtmlViews.StatusPages.error next ctx
                | false ->
                    let! info = signInManager.GetExternalLoginInfoAsync()
                    match isNull info with
                    | true -> return! htmlView HtmlViews.StatusPages.error next ctx
                    | false ->
                        let! res = userManager.AddLoginAsync(user,info)
                        match res.Succeeded with 
                        | false -> return! htmlView HtmlViews.StatusPages.error next ctx
                        | true -> return! redirectTo true "/Account/cool" next ctx
            }
