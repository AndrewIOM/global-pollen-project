[<AutoOpen>]
module Requests

open System.ComponentModel.DataAnnotations

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
}

[<CLIMutable>]
type LoginRequest = {
    [<Required>] [<EmailAddress>] Email: string
    [<Required>] [<DataType(DataType.Password)>] Password: string
    [<Display(Name = "Remember me?")>] RememberMe: bool
}

type StartCollectionRequest = {
    Name: string
    Description: string 
}

type SlideRecordRequest = {
    Collection: System.Guid
    BackboneTaxonId: System.Guid
}

type SlideImageRequest = {
    CollectionId: System.Guid
    SlideId: string
    ImageBase64: string
}

[<CLIMutable>]
type BackboneSearchRequest = {
    LatinName: string
    Rank: string
    Family: string
    Genus: string
    Species: string
}