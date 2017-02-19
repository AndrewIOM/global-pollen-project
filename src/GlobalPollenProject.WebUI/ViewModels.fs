namespace GlobalPollenProject.WebUI.ViewModels

open System.ComponentModel.DataAnnotations
open Microsoft.AspNetCore.Mvc.Rendering

// Taxonomy
type ImportTaxon = {
    Id: int
    LatinName: string
    Rank: string
}

// Account
[<CLIMutable>]
type ExternalLoginConfirmationViewModel = {
    [<Required>] 
    [<EmailAddress>] 
    Email: string
    
    [<Required>] 
    [<EmailAddress>] 
    [<Compare("Email", ErrorMessage = "The email and confirmation email do not match.")>]
    [<Display(Name = "Confirm Email")>]
    EmailConfirmation: string

    [<Required>]
    [<Display(Name = "Title")>]
    Title: string

    [<Required>]
    [<Display(Name = "Forename(s)")>]
    FirstName: string

    [<Required>]
    [<Display(Name = "Surname")>]
    LastName: string

    [<Required>]
    [<Display(Name = "Organisation")>]
    Organisation: string
}

[<CLIMutable>]
type ResetPasswordViewModel = {
    [<Required>] [<EmailAddress>] Email:string

    [<Required>]
    [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
    [<DataType(DataType.Password)>]
    Password:string

    [<DataType(DataType.Password)>]
    [<Display(Name = "Confirm password")>]
    [<Compare("Password", ErrorMessage = "The password and confirmation password do not match.")>]
    ConfirmPassword:string

    Code:string
}

[<CLIMutable>]
type ForgotPasswordViewModel = {
    [<Required>] [<EmailAddress>] Email:string
}

[<CLIMutable>]
type LoginViewModel = {
    [<Required>] [<EmailAddress>] Email:string
    [<Required>] [<DataType(DataType.Password)>] Password:string
    [<Required>] [<Display(Name="Remember me?")>] RememberMe:bool    
}

[<CLIMutable>]
type RegisterViewModel = {
    [<Required>]
    [<Display(Name ="Title")>]
    Title:string

    [<Required>]
    [<Display(Name = "Forename(s)")>]
    FirstName:string

    [<Required>]
    [<Display(Name = "Surname")>]
    LastName:string

    [<Required>]
    Organisation:string

    [<Required>]
    [<EmailAddress>]
    Email:string

    [<Required>]
    [<EmailAddress>]
    [<Compare("Email", ErrorMessage = "The email and confirmation email do not match.")>]
    [<Display(Name = "Confirm Email")>]
    EmailConfirmation:string

    [<Required>]
    [<StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)>]
    [<DataType(DataType.Password)>]
    [<Display(Name = "Password")>]
    Password:string

    [<DataType(DataType.Password)>]
    [<Display(Name = "Confirm password")>]
    [<Compare("Password", ErrorMessage = "The password and confirmation password do not match.")>]
    ConfirmPassword:string
}