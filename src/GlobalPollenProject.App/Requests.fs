[<AutoOpen>]
module Requests

open System
open System.ComponentModel.DataAnnotations

[<CLIMutable>] type IdQuery = { Id: Guid }

type PageRequest = { Page: int; PageSize: int }

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
    [<Required>] Name: string
    [<Required>] Description: string 
}

[<CLIMutable>]
type SlideRecordRequest = {
    [<Required>] Collection: System.Guid
    [<Required>] OriginalFamily: string
    [<Required>] ValidatedTaxonId: System.Guid
    [<Required>] SamplingMethod: string
    OriginalGenus: string
    OriginalSpecies: string
    OriginalAuthor: string
    ExistingId: string
    YearCollected: System.Nullable<int>
    YearSlideMade: System.Nullable<int>
    LocationRegion: string
    LocationCountry: string
    PreperationMethod: string
    MountingMaterial: string
}

type SlideImageRequest = {
    [<Required>] CollectionId: System.Guid
    [<Required>] SlideId: string
    [<Required>] ImageBase64: string
    DigitisedYear: int
}

[<CLIMutable>]
type BackboneSearchRequest = {
    [<Required>]LatinName: string
    [<Required>]Rank: string
    Family: string
    Genus: string
    Species: string
    Authorship: string
}

[<CLIMutable>]
type AddUnknownGrainRequest = {
    StaticImagesBase64: string list
    [<Required>] LatitudeDD: float
    [<Required>] LongitudeDD: float
    [<Required>] SampleType: string
    Year: int option
    YearType: string
}

[<CLIMutable>]
type AddMicroscopeRequest = {
    [<Required>] Name: string
    [<Required>] Type: string
    [<Required>] Model: string
    Ocular: int
    Objectives: int list
}

[<CLIMutable>]
type CalibrateRequest = {
    [<Required>] CalibrationId: Guid
    [<Required>] [<Range(1, 100)>] Magnification: int
    [<Required>] [<Range(1, Int32.MaxValue)>] X1: int
    [<Required>] [<Range(1, Int32.MaxValue)>] X2: int
    [<Required>] [<Range(1, Int32.MaxValue)>] Y1: int
    [<Required>] [<Range(1, Int32.MaxValue)>] Y2: int
    [<Required>] [<Range(1, Int32.MaxValue)>] MeasuredLength: float
    [<Required>] ImageBase64: string
}