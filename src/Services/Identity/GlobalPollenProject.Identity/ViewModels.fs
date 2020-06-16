namespace GlobalPollenProject.Identity.ViewModels

open System.ComponentModel.DataAnnotations

[<CLIMutable>]
type LoggedOutViewModel = {
    PostLogoutRedirectUri: string
    ClientName: string
    SignOutIframeUrl: string
}

[<CLIMutable>]
type ReturnUrlQuery = {
    ReturnUrl: string
}

[<CLIMutable>]
type NewAppUserRequest = {
    [<Required>] [<Display(Name ="Title")>] Title: string
    [<Required>] [<Display(Name = "Forename(s)")>] FirstName: string
    [<Required>] [<Display(Name = "Surname")>] LastName: string
    [<Required>] [<Display(Name = "Organisation")>] Organisation: string
    [<Required>] [<EmailAddress>] [<Display(Name = "Email")>] Email: string

    [<Required>] 
    [<EmailAddress>] 
    [<Compare("Email", ErrorMessage = "The email and confirmation email do not match.")>] 
    [<Display(Name = "Confirm Email")>] 
    EmailConfirmation: string
    
    [<Required>]
    [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
    [<DataType(DataType.Password)>]
    [<Display(Name = "Password")>]
    Password: string

    [<DataType(DataType.Password)>]
    [<Display(Name = "Confirm password")>]
    [<Compare("Password", ErrorMessage = "The password and confirmation password do not match.")>]
    ConfirmPassword: string

    ReturnUrl: string
}

[<CLIMutable>]
type ExternalLoginConfirmationViewModel = {
    [<Required>] [<Display(Name ="Title")>] Title: string
    [<Required>] [<Display(Name = "Forename(s)")>] FirstName: string
    [<Required>] [<Display(Name = "Surname")>] LastName: string
    [<Required>] [<Display(Name = "Organisation")>] Organisation: string
    [<Required>] [<EmailAddress>] [<Display(Name = "Email")>] Email: string

    [<Required>] 
    [<EmailAddress>] 
    [<Compare("Email", ErrorMessage = "The email and confirmation email do not match.")>] 
    [<Display(Name = "Confirm Email")>] 
    EmailConfirmation: string
    ReturnUrl: string
}

[<CLIMutable>]
type ConfirmEmailRequest = {
    UserId: string
    Code: string
    ReturnUrl: string
}

[<CLIMutable>]
type ExternalLoginRequest = {
    [<Required>] Provider: string
}

[<CLIMutable>]
type ResetPasswordViewModel = {
    [<Required>] 
    [<EmailAddress>] 
    [<Display(Name = "Email")>] 
    Email: string

    Code: string

    [<Required>]
    [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
    [<DataType(DataType.Password)>]
    Password: string

    [<DataType(DataType.Password)>]
    [<Display(Name = "Confirm password")>]
    [<Compare("Password", ErrorMessage = "The password and confirmation password do not match.")>]
    ConfirmPassword: string
}

[<CLIMutable>]
type ForgotPasswordViewModel = {
    [<Required>] [<EmailAddress>] Email: string
}

[<CLIMutable>]
type LoginRequest = {
    [<Required>] [<EmailAddress>] Email: string
    [<Required>] [<DataType(DataType.Password)>] Password: string
    [<Display(Name = "Remember me?")>] RememberMe: bool
    [<Required>] ReturnUrl: string
}

module Empty =

    let newAppUserRequest returnUrl = { ReturnUrl = returnUrl; Title = ""; FirstName = ""; LastName = ""; Organisation = ""; Email = "" ; EmailConfirmation = ""; Password = ""; ConfirmPassword = "" }
