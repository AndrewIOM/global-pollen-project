namespace GlobalPollenProject.Identity.Views

open Giraffe.GiraffeViewEngine
open GlobalPollenProject.Identity.ViewModels

module Colours =

    let primary = "#a8699a"
    let secondary = "#594157"
    let blue = "#435058"
    let green = "#bad29f"
    let grey = "#f2f7f2"

module Template =

    let stylesheets = 
        [ "//fonts.googleapis.com/css?family=Roboto:300,300italic,700,700italic"
          "//fonts.googleapis.com/css?family=Hind|Montserrat&display=swap"
          "//cdn.rawgit.com/necolas/normalize.css/master/normalize.css"
          "//cdn.rawgit.com/milligram/milligram/master/dist/milligram.min.css" 
          "//cdnjs.cloudflare.com/ajax/libs/font-awesome/5.8.2/css/all.min.css"
          "/css/main.css"
        ] |> List.map(fun url -> link [ _rel "stylesheet"; _href url ])

    let master pageTitle contents =
        html [] [
            head [] (List.append [
                meta [_charset "utf-8"]
                meta [_name "viewport"; _content "width=device-width, initial-scale=1.0" ]
                title [] [ pageTitle |> sprintf "%s - Global Pollen Project" |> encodedText ]
            ] stylesheets)
            body [ ] contents
        ]

module Errors =
        
        open GlobalPollenProject.Shared
        
        let validationSummary (additionalErrors: ValidationError list) =
            let errorHtml =
                additionalErrors
                |> List.collect (fun e -> e.Errors)
                |> List.map encodedText
            div [] errorHtml

module Pages = 

    let login (vm:LoginRequest) =
        [
            div [ _class "container main-panel" ] [
                h1 [ _class "title" ] [ str "Global Pollen Project" ]
                p [] [ str "Sign in" ]
                form [ _method "POST"; _action "/Account/Login" ] [
                    fieldset [] [
                        label [ _for "Email" ] [ str "Username" ]
                        input [ _type "text"; _placeholder "Your email"; _name "Email"; _value vm.Email; _style "text-align:center" ]
                        label [ _for "Password" ] [ str "Password" ]
                        input [ _type "password"; _name "Password"; _style "text-align:center" ]
                        input [ _class "button-primary"; _type "submit"; _value "Login" ]
                        input [ _type "hidden"; _name "ReturnUrl"; _value vm.ReturnUrl ]
                        input [ _type "hidden"; _name "RememberMe"; _value "false" ]
                    ]
                ]
            ]
            div [ _class "container main-panel" ] [
                form [ _method "GET"; _action "/Account/Register" ] [
                    input [ _type "hidden"; _name "ReturnUrl"; _value vm.ReturnUrl ]
                    input [ _class "button button-register"; _type "submit"; _value "Register with email" ] ]
                form [ _method "GET"; _action "/Account/ExternalLogin" ] [
                    input [ _type "hidden"; _name "Provider"; _value "Facebook" ]
                    input [ _class "button button-social button-facebook"; _type "submit"; _value "Continue with Facebook" ] ]
                form [ _method "GET"; _action "/Account/ExternalLogin" ] [
                    input [ _type "hidden"; _name "Provider"; _value "Twitter" ]
                    input [ _class "button button-social button-twitter"; _type "submit"; _value "Log in with Twitter" ] ]
                p [] [ str "You must login to identity pollen, digitise collections, or access our data programatically." ]
            ]
        ] |> Template.master "Login"

    let register errors (vm:NewAppUserRequest) =
        [
            div [ _class "container main-panel" ] [
                h1 [ _class "title" ] [ str "Create an Account" ]
                p [] [ str "An account will enable you to submit your own unknown pollen grains and identify others. You can also request access to our digitisation features." ]
                form [ _method "POST"; _action "/Account/Register" ] [
                    Errors.validationSummary errors
                    fieldset [] [
                        label [ _for "Title" ] [ str "Title (e.g. Mr, Mrs, Dr)"]
                        input [ _type "text"; _placeholder "Miss"; _name "Title" ]
                        label [ _for "FirstName" ] [ str "First Name(s)"]
                        input [ _type "text"; _name "FirstName" ]
                        label [ _for "LastName" ] [ str "Surname"]
                        input [ _type "text"; _name "LastName" ]
                        label [ _for "Email" ] [ str "Email Address" ]
                        input [ _type "email"; _name "Email" ]
                        label [ _for "EmailConfirmation" ] [ str "Confirm your email address" ]
                        input [ _type "email"; _name "EmailConfirmation" ]
                        label [ _for "Password" ] [ str "Password" ]
                        input [ _type "password"; _name "Password" ]
                        label [ _for "ConfirmPassword" ] [ str "Confirm your password" ]
                        input [ _type "password"; _name "ConfirmPassword" ]
                        label [ _for "Organisation" ] [ str "Organisation"]
                        input [ _type "text"; _name "Organisation" ]
                        input [ _class "button-primary"; _type "submit" ]
                        input [ _type "hidden"; _name "ReturnUrl"; _value vm.ReturnUrl ]
                    ]
                ]
            ]
        ] |> Template.master "Create a new account" 

    let externalRegistration loginProvider errors (vm:ExternalLoginConfirmationViewModel) =
        [
            div [ _class "container main-panel" ] [
                h1 [ _class "title" ] [ str "Nearly There..." ]
                p [] [ str <| sprintf "You've signed in with %s. The Global Pollen Project just needs a few more details from you." loginProvider ]
                form [ _method "POST"; _action "/Account/ExternalLoginConfirmation" ] [
                    Errors.validationSummary errors
                    fieldset [] [
                        label [ _for "Title" ] [ str "Title (e.g. Mr, Mrs, Dr)"]
                        input [ _type "text"; _placeholder "Miss"; _name "Title" ]
                        label [ _for "FirstName" ] [ str "First Name(s)"]
                        input [ _type "text"; _name "FirstName" ]
                        label [ _for "LastName" ] [ str "Surname"]
                        input [ _type "text"; _name "LastName" ]
                        label [ _for "Organisation" ] [ str "Organisation"]
                        input [ _type "text"; _name "Organisation" ]
                        input [ _class "button-primary"; _type "submit" ]
                        input [ _type "hidden"; _name "ReturnUrl"; _value vm.ReturnUrl ]
                    ]
                ]
            ]
        ] |> Template.master "Create a new account" 

    let confirmCode emailAddress =
        [
            div [ _class "container main-panel" ] [
                h1 [ _class "title" ] [ str "One Last Step..." ]
                p [] [ str <| sprintf "You should shortly receive an email at %s. Please follow the link in that email. If you do not receive the email within five minutes, you can request another email here." emailAddress ]
                a [ _class "button" ] [ str "Send me a new code" ]
            ]
        ] |> Template.master "Awaiting your email code"

    let error message =
        [
            h1 [] [ str "There was an error" ]
            p [] [ str message ]
        ] |> Template.master "Error"

    let loggedOut model =
        [
            h1 [] [ str "You are now logged out" ]
            (if not <| isNull model.PostLogoutRedirectUri
            then div [] [
                a [ _href model.PostLogoutRedirectUri ] [ str "Click here" ]
                str (sprintf " to return to the %s app." model.ClientName)
            ]
            else span [] [])
        ] |> Template.master "Successfully logged out"
        
        
    module Manage =
                
        open Microsoft.AspNetCore.Identity
        
        let index username (vm:IndexViewModel) =
            [
                div [ _class "container main-panel" ] [
                    h1 [ _class "title" ] [ str "Manage your login" ]
                    p [] [ str "The Global Pollen Project allows you to log in using a password set with us, or using an external account such as Twitter or Facebook." ]
                    label [] [ str "Your user name:" ]
                    p [] [ str username ]
                    label [] [ str "Password" ]
                    match vm.HasPassword with
                    | true -> a [ _href "/Manage/ChangePassword" ] [ str "Change your password" ]
                    | false -> p [] [
                        str "You do not currently have a local password. You can "
                        a [ _href "/Manage/SetPassword" ] [ str "create" ]
                        str " one now"
                    ]
                    label [] [ str "Connected logins (other providers)" ]
                    match vm.Logins.Length with
                    | 0 -> p [] [ str "None" ]
                    | _ -> p [] [ str <| sprintf "You have %i set up." vm.Logins.Length ]
                    a [ _href "/Manage/ManageLogins" ] [ str "Manage external logins" ]
                ]
            ] |> Template.master "Manage your account" 
            
        let changePassword errors (vm:ChangePasswordViewModel) =
            [
                div [ _class "container main-panel" ] [
                    h1 [ _class "title" ] [ str "Change Password" ]
                    form [ _action "/Manage/ChangePassword"; _method "POST" ] [
                        Errors.validationSummary errors
                        label [ _for "OldPassword" ] [ str "Old Password"]
                        input [ _type "password"; _name "OldPassword"; _value vm.OldPassword ]
                        label [ _for "NewPassword" ] [ str "New Password"]
                        input [ _type "password"; _name "NewPassword"; _value vm.NewPassword ]
                        label [ _for "ConfirmPassword" ] [ str "Re-enter your new password"]
                        input [ _type "password"; _name "ConfirmPassword"; _value vm.ConfirmPassword ]
                        button [ _class "button-primary"; _type "submit" ] [ str "Change password" ]
                    ]
                ]
            ] |> Template.master "Change password"

        let setPassword errors (vm:SetPasswordViewModel) =
            [
                div [ _class "container main-panel" ] [
                    h1 [ _class "title" ] [ str "Set a New Password" ]
                    p [] [ str "You do not have a local username/password for this site. Add a local password so you can log in without an external service." ]
                    form [ _action "/Manage/ChangePassword"; _method "POST" ] [
                        Errors.validationSummary errors
                        label [ _for "NewPassword" ] [ str "New Password"]
                        input [ _type "password"; _name "NewPassword"; _value vm.NewPassword ]
                        label [ _for "ConfirmPassword" ] [ str "Re-enter your new password"]
                        input [ _type "password"; _name "ConfirmPassword"; _value vm.ConfirmPassword ]
                        button [ _class "button-primary"; _type "submit" ] [ str "Set password" ]
                    ]
                ]
            ] |> Template.master "Set new password"

        let manageLogins (vm:ManageLoginsViewModel) =
            [
                div [ _class "container main-panel" ] [
                    h1 [ _class "title" ] [ str "Unlink other services" ]
                    p [] [ str "If you unlink from other services, you will not be able to use them to log in to the Pollen Project." ]
                    if vm.CurrentLogins.Length > 0
                    then
                        div [] (vm.CurrentLogins |> List.map(fun l ->
                            form [ _method "POST"; _action "/Manage/RemoveLogin" ] [
                                p [] [ str l.ProviderDisplayName ]
                                input [ _type "hidden"; _name "LoginProvider"; _value l.LoginProvider ]
                                input [ _type "hidden"; _name "ProviderKey"; _value l.ProviderKey ]
                                input [ _class "button button-outline"; _type "submit"; _value "Remove"
                                        _title <| sprintf "Remove this %s login from your account" l.ProviderDisplayName ]
                                ]
                            ))
                    else div [] []
                    if vm.OtherLogins.Length > 0
                    then
                        h4 [] [ str "Add another service to log in." ]
                        form [ _action "/Manage/LinkLogin"; _method "POST" ] (vm.OtherLogins |> List.map(fun o ->
                            button [ _type "submit"; _class "button"; _name "provider"; _value o.Name
                                     _title <| sprintf "Log in using a %s account" o.DisplayName ] [ str o.Name ]
                            ))
                    else div [] []
                ]
            ] |> Template.master "Manage logins"

        let removeLogin (vm:UserLoginInfo list) =
            [
                div [ _class "container main-panel" ] [
                    h1 [ _class "title" ] [ str "Unlink other services" ]
                    p [] [ str "If you unlink from other services, you will not be able to use them to log in to the Pollen Project." ]
                    div [] (vm |> List.map(fun l ->
                        form [ _method "POST"; _action "/Manage/RemoveLogin" ] [
                            p [] [ str l.ProviderDisplayName ]
                            input [ _type "hidden"; _name "LoginProvider"; _value l.LoginProvider ]
                            input [ _type "hidden"; _name "ProviderKey"; _value l.ProviderKey ]
                            input [ _class "button button-outline"; _type "submit"; _value "Remove" ]
                            ]
                        ))
                ]
            ] |> Template.master "Remove linked login"


(*

module Account =

    open Forms

    let login errors (vm:Requests.LoginRequest) =
        [
            Grid.row [
                Grid.column Medium 8 [
                    form [ _action "/Account/Login"; _method "POST"; _class "form-horizontal" ] [
                        validationSummary errors vm
                        formField <@ vm.Email @>
                        formField <@ vm.Password @>
                        formField <@ vm.RememberMe @>
                        div [ _class "row form-group" ] [
                            div [ _class "offset-sm-2 col-sm-10" ] [
                                button [ _type "submit"; _class "btn btn-primary" ] [ encodedText "Sign in" ]
                                a [ _class "btn btn-secondary"; _href "/Account/ForgotPassword" ] [ encodedText "Forgotten Password" ] 
                            ]
                        ]
                    ]
                ]
                Grid.column Medium 4 [
                    section [] [
                        form [ _action "/Account/ExternalLogin"; _method "POST"; _class "form-horizontal" ] [
                            button [ _name "provider"; _class "btn btn-block btn-social btn-facebook"; _type "submit"; _value "Facebook" ] [ 
                                Icons.fontawesome "facebook"
                                encodedText "Sign in with Facebook" ]
                            button [ _name "provider"; _class "btn btn-block btn-social btn-twitter"; _type "submit"; _value "Twitter" ] [ 
                                Icons.fontawesome "twitter"
                                encodedText "Sign in with Twitter" ]
                        ]
                        br []
                        div [ _class "panel panel-primary" ] [
                            div [ _class "panel-heading" ] [
                                Icons.fontawesome "pencil"
                                encodedText "Sign up today"
                            ]
                            div [ _class "panel-body" ] [
                                p [] [ encodedText "Register to submit your pollen and exchange identifications." ]
                                a [ _class "btn btn-secondary"; _href "/Account/Register" ] [ encodedText "Register" ]
                            ]
                        ]
                    ]
                ]
            ]
        ] |> Layout.standard [] "Log in" "Use your existing Global Pollen Project account, Facebook or Twitter"

    let register errors (vm:NewAppUserRequest) =
        [
            form [ _action "/Account/Register"; _method "POST"; _class "form-horizontal" ] [
                p [] [ encodedText "An account will enable you to submit your own unknown pollen grains and identify others. You can also request access to our digitisation features." ]
                p [] [ encodedText "You can also alternatively"; a [ _href "/Account/Login" ] [ encodedText "sign in with your Facebook or Twitter account." ] ]
                hr []
                validationSummary errors vm
                h4 [] [ encodedText "About You" ]
                formField <@ vm.Title @>
                formField <@ vm.FirstName @>
                formField <@ vm.LastName @>
                formField <@ vm.Email @>
                formField <@ vm.EmailConfirmation @>
                formField <@ vm.Password @>
                formField <@ vm.ConfirmPassword @>
                hr []
                h4 [] [ encodedText "Your Organisation" ]
                p [] [ encodedText "Are you a member of a lab group, company or other organisation? Each grain you identify gives you a bounty score. By using a common group name, you can build up your score together. Can your organisation become top identifiers?" ]
                formField <@ vm.Organisation @>
                p [] [ encodedText "By registering, you agree to the Global Pollen Project"; a [ _href "/Guide/Terms" ] [ encodedText "Terms and Conditions." ] ]
                button [ _type "submit"; _class "btn btn-primary" ] [ encodedText "Register" ]
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Register" "Create a new account"

    let externalRegistration provider errors (vm:ExternalLoginConfirmationViewModel) =
        [
            form [ _action "/Account/ExternalLoginConfirmation"; _method "POST"; _class "form-horizontal" ] [
                p [] [ encodedText ("You've successfully authenticated with " + provider + ". We just need a few more personal details from you before you can log in.") ]
                validationSummary errors vm
                h4 [] [ encodedText "About You" ]
                formField <@ vm.Title @>
                formField <@ vm.FirstName @>
                formField <@ vm.LastName @>
                formField <@ vm.Email @>
                formField <@ vm.EmailConfirmation @>
                hr []
                h4 [] [ encodedText "Your Organisation" ]
                p [] [ encodedText "Are you a member of a lab group, company or other organisation? Each grain you identify gives you a bounty score. By using a common group name, you can build up your score together. Can your organisation become top identifiers?" ]
                formField <@ vm.Organisation @>
                p [] [ encodedText "By registering, you agree to the Global Pollen Project"; a [ _href "/Guide/Terms" ] [ encodedText "Terms and Conditions." ] ]
                button [ _type "submit"; _class "btn btn-primary" ] [ encodedText "Register" ]
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Nearly logged in..." ("Associate your" + provider + "account")

    let awaitingEmailConfirmation =
        [
            p [] [ encodedText "Please check your email for an activation link. You must do this before you can log in." ]
        ] |> Layout.standard [] "Confirm Email" ""

    let confirmEmail =
        [
            p [] [ 
                encodedText "Thank you for confirming your email. Please"
                a [ _href Urls.Account.login ] [ encodedText "Click here to Log in" ]
                encodedText "." ]
        ] |> Layout.standard [] "Confirm Email" ""

    let forgotPasswordConfirmation =
        [ p [] [ encodedText "Please check your email to reset your password" ]
        ] |> Layout.standard [] "Confirm Email" ""

    let externalLoginFailure =
        [ p [] [ encodedText "Unsuccessful login with service" ] ]
        |> Layout.standard [] "Login failure" ""

    let resetPassword (vm:ResetPasswordViewModel) =
        [
            form [ _action "/Account/ResetPassowrd"; _method "POST"; _class "form-horizontal" ] [
                // Validation summary
                input [ _hidden; _value vm.Code ]
                Forms.formField <@ vm.Email @>
                Forms.formField <@ vm.Password @>
                Forms.formField <@ vm.ConfirmPassword @>
                Forms.submit
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Reset Password" ""

    let resetPasswordConfirmation =
        [ p [] [ 
            encodedText "Your password has been reset."
            a [ _href "/Account/Login" ] [ encodedText "Click here to login." ] ]
        ] |> Layout.standard [] "Confirm Email" ""

    let lockout =
        [

        ] |> Layout.standard [] "" ""

    let forgotPassword (vm:ForgotPasswordViewModel) =
        [
            form [ _href "/Account/ForgotPassword"; _method "POST"; _class "form-horizontal" ] [
                h4 [] [ encodedText "Enter your email." ]
                // Validation summary here
                formField <@ vm.Email @>
                Forms.submit
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Forgot your password?" ""
*)
