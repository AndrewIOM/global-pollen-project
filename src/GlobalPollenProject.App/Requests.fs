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
}

[<CLIMutable>]
type ConfirmEmailRequest = {
    UserId: string
    Code: string
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
}

[<CLIMutable>]
type StartCollectionRequest = {
    [<Required>] [<StringLength(50,MinimumLength=10)>] Name: string
    [<Required>] Description: string 
    [<Required>] CuratorFirstNames: string
    [<Required>] CuratorSurname: string
    [<Required>] CuratorEmail: string
    [<Required>] AccessMethod: string
    Institution: string
    InstitutionUrl: string
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
    [<Range(1700,2017)>] YearCollected: System.Nullable<int>
    CollectedByFirstNames: string list
    CollectedBySurname: string
    [<Range(1950,2017)>] YearSlideMade: System.Nullable<int>
    [<Required>] LocationType: string
    LocationLocality: string
    LocationDistrict: string
    LocationRegion: string
    LocationCountry: string
    LocationContinent: string
    PreperationMethod: string
    MountingMaterial: string
}

[<CLIMutable>]
type SlideImageRequest = {
    [<Required>] CollectionId: System.Guid
    [<Required>] SlideId: string
    [<Required>] IsFocusImage: bool
    [<Required>] FramesBase64: List<string>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointOneX: Nullable<int>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointOneY: Nullable<int>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointTwoX: Nullable<int>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointTwoY: Nullable<int>
    [<Range(0,Int32.MaxValue)>] MeasuredDistance: Nullable<float>
    CalibrationId: System.Guid
    [<Range(0,10000)>] Magnification: int
    [<Range(1950,2017)>] DigitisedYear: Nullable<int>
}

[<CLIMutable>]
type BackboneSearchRequest = {
    [<Required>] LatinName: string
    [<Required>] Rank: string
    Family: string
    Genus: string
    Species: string
    Authorship: string
}

[<CLIMutable>]
type FloatingCalibrationImage = {
    [<Required>] PngBase64: string
    [<Required>] [<Range(0, Int32.MaxValue)>] X1: int
    [<Required>] [<Range(0, Int32.MaxValue)>] X2: int
    [<Required>] [<Range(0, Int32.MaxValue)>] Y1: int
    [<Required>] [<Range(0, Int32.MaxValue)>] Y2: int
    [<Required>] [<Range(1, Int32.MaxValue)>] MeasuredLength: float
}

[<CLIMutable>]
type AddUnknownGrainRequest = {
    [<Required>] Images: FloatingCalibrationImage list
    [<Required>] SampleType: string
    [<Required>] LatitudeDD: float
    [<Required>] LongitudeDD: float
    Year: Nullable<int>
    [<Required>] YearType: string
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

[<CLIMutable>]
type IdentifyGrainRequest = {
    [<Required>] TaxonId: Guid
    [<Required>] GrainId: Guid
}

[<CLIMutable>]
type TaxonPageRequest = {
    Page: int
    PageSize: int
    Rank: string
    Lex: string
}

[<CLIMutable>]
type TaxonAutocompleteRequest = {
    PageSize: int
    Name: string
}

[<CLIMutable>]
type CurateCollectionRequest = {
    Approved: bool
    Comment: string
    Collection: Guid
}