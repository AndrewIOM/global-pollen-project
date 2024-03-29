[<AutoOpen>]
module Requests

open System
open System.ComponentModel.DataAnnotations
open ReadModels

[<CLIMutable>] type IdQuery = { Id: Guid }

[<CLIMutable>]
type PageRequest = { Page: int; PageSize: int }

[<CLIMutable>]
type NewAppUserRequest = {
    [<Required>] [<Display(Name ="Title")>] Title: string
    [<Required>] [<Display(Name = "Forename(s)")>] FirstName: string
    [<Required>] [<Display(Name = "Surname")>] LastName: string
    [<Required>] [<Display(Name = "Organisation")>] Organisation: string
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
    [<Required>] Collection: Guid
    [<Required>] OriginalFamily: string
    [<Required>] ValidatedTaxonId: Guid
    [<Required>] SamplingMethod: string
    OriginalGenus: string
    OriginalSpecies: string
    OriginalAuthor: string
    ExistingId: string
    PlantIdMethod: PlantIdMethod
    [<Range(1700,2022)>] YearCollected: Nullable<int>
    [<Range(1950,2022)>] YearSlideMade: Nullable<int>
    [<Required>] LocationType: string
    LocationLocality: string
    LocationDistrict: string
    LocationRegion: string
    LocationCountry: string
    LocationContinent: string
    PreparedByFirstNames: string list
    PreparedBySurname: string
    PreperationMethod: string
    MountingMaterial: string
    CollectedByFirstNames: string list
    CollectedBySurname: string
}

[<CLIMutable>]
type SlideImageRequest = {
    [<Required>] CollectionId: Guid
    [<Required>] SlideId: string
    [<Required>] IsFocusImage: bool
    [<Required>] FramesBase64: List<string>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointOneX: Nullable<int>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointOneY: Nullable<int>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointTwoX: Nullable<int>
    [<Range(0,Int32.MaxValue)>] FloatingCalPointTwoY: Nullable<int>
    [<Range(0,Int32.MaxValue)>] MeasuredDistance: Nullable<float>
    CalibrationId: Guid
    [<Range(0,10000)>] Magnification: int
    [<Range(1950,2023)>] DigitisedYear: Nullable<int>
}

[<CLIMutable>]
type BackboneSearchRequest = {
    [<Required>] LatinName: string
    [<Required>] Rank: string
    Family: string option
    Genus: string option
    Species: string option
    Authorship: string option
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
    [<Required>] [<Range(1, 10000)>] Magnification: int
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
    [<Required>] Approved: bool
    Comment: string
    [<Required>] Collection: Guid
}

[<CLIMutable>]
type UserRoleRequest = {
    UserId: Guid
}

[<CLIMutable>]
type VoidSlideRequest = {
    [<Required>] SlideId: string
    [<Required>] CollectionId: Guid
}

module Empty =
    let addCollection =  { Name = ""; Description = ""; CuratorFirstNames = ""; CuratorSurname = ""; CuratorEmail = ""; AccessMethod = ""; Institution = ""; InstitutionUrl = "" }
    let recordSlide = {
        Collection = Guid.Empty
        OriginalFamily = ""
        ValidatedTaxonId = Guid.Empty
        SamplingMethod = ""
        OriginalGenus = ""
        OriginalSpecies = ""
        OriginalAuthor = ""
        ExistingId = ""
        PlantIdMethod = {
            Method = ""
            InstitutionCode = ""
            InternalId = ""
            IdentifiedByFirstNames = []
            IdentifiedBySurname = "" }
        YearCollected = Nullable<int>()
        YearSlideMade = Nullable<int>()
        LocationType = ""
        LocationLocality = ""
        LocationDistrict = ""
        LocationRegion = ""
        LocationCountry = ""
        LocationContinent = ""
        PreparedByFirstNames = []
        PreparedBySurname = ""
        PreperationMethod = ""
        MountingMaterial = ""
        CollectedByFirstNames = []
        CollectedBySurname = ""
    }