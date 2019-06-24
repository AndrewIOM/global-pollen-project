namespace GlobalPollenProject.Identity.Views

open Giraffe.GiraffeViewEngine
open GlobalPollenProject.Identity.ViewModels

[<CLIMutable>]
type LoggedOutViewModel = {
    PostLogoutRedirectUri: string
    ClientName: string
    SignOutIframeUrl: string
}

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
                p [] [ str "You will need an account to identity pollen, digitise collections, or access our data programatically." ]
            ]
        ] |> Template.master "Login"

    let register errors (vm:NewAppUserRequest) =
        [
            div [ _class "container main-panel" ] [
                h1 [ _class "title" ] [ str "Create an Account" ]
                p [] [ str "An account will enable you to submit your own unknown pollen grains and identify others. You can also request access to our digitisation features." ]
                form [ _method "POST"; _action "/Account/Register" ] [
                    p [] [ str (sprintf "Errors were %A" errors) ]
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
                    p [] [ str (sprintf "Errors were %A" errors) ]
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
                p [] [ str <| sprintf "You should shortly recieve an email at %s. Please follow the link in that email. If you do not recieve the email within five minutes, you can request another email here." emailAddress ]
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