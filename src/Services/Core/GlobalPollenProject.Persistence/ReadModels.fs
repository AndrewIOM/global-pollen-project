module ReadModels

open System

// Helper DTOs
[<CLIMutable>]
type SlideImage = {
    Id:                     int
    Frames:                 List<string>
    PixelWidth:             float
    FramesSmall:            string list
    PixelWidthSmall:        float
}

// GPP Master Reference Collection

[<CLIMutable>]
type SlideSummary = {
    ColId:      Guid
    SlideId:    string
    LatinName:  string
    Rank:       string
    Thumbnail:  string
}

[<CLIMutable>]
type GrainSummary = {
    Id:         Guid
    Thumbnail:  string
    Submitted:  DateTime
}

[<CLIMutable>]
type Node = {
    Id:         Guid
    Name:       string
    Rank:       string
}

[<CLIMutable>]
type TaxonSummary = {
    Id:         Guid
    Family:     string
    Genus:      string
    Species:    string
    LatinName:  string
    Authorship: string
    Rank:       string
    SlideCount: int
    GrainCount: int
    ThumbnailUrl:string
    DirectChildren:Node list
}

[<CLIMutable>]
type EncyclopediaOfLifeCache = {
    CommonEnglishName:      string
    PhotoUrl:               string
    PhotoAttribution:       string
    PhotoLicence:           string
    Description:            string
    DescriptionAttribution: string
    DescriptionLicence:     string
    Retrieved:              DateTime
}

[<CLIMutable>]
type TaxonDetail = {
    Id:                 Guid
    Family:             string
    Genus:              string
    Species:            string
    LatinName:          string
    Authorship:         string
    Rank:               string
    Parent:             Node option
    Children:           Node list
    Slides:             SlideSummary list
    Grains:             GrainSummary list
    NeotomaId:          int
    GbifId:             int
    EolId:              int
    EolCache:           EncyclopediaOfLifeCache
    ReferenceName:      string
    ReferenceUrl:       string
    BackboneChildren:   int
}

[<CLIMutable>]
type NeotomaSite = {
    AgeOldest: int
    AgeYoungest: int
    Latitude: float
    Longitude: float
    Proxy: string
    SiteId: int
}

[<CLIMutable>]
type NeotomaCache = {
    RefreshTime: DateTime
    Occurrences: NeotomaSite list
}

[<CLIMutable>]
type TaxonAutocompleteItem = {
    LatinName: string
    Rank: string
    Heirarchy: string list
}

// Grains
// - Crowdsourced / Unknown Grains
// - Grains split out of digitised slides

[<CLIMutable>]
type IdentificationSummary = {
    User: Guid
    IdentificationMethod: string
    Rank: string
    Family: string
    Genus: string
    Species: string
    SpAuth: string
}

[<CLIMutable>]
type GrainDetail = {
    Id:                 Guid
    Images:             SlideImage list
    Identifications:    IdentificationSummary list
    ConfirmedFamily:    string
    ConfirmedGenus:     string
    ConfirmedSpecies:   string
    ConfirmedSpAuth:    string
    ConfirmedRank:      string
    Latitude:           float
    Longitude:          float
    AgeType:            string
    Age:                int
}

// Reference Slides
// - Not split into grains

[<CLIMutable>]
type PlantIdMethod = {
    Method: string
    InstitutionCode: string
    InternalId: string
    IdentifiedByFirstNames: string list
    IdentifiedBySurname: string
}

[<CLIMutable>]
type SlideDetail = {
    CollectionId:       Guid
    CollectionSlideId:  string
    CollectorName:      string
    IdMethod:           string
    PlantId:            PlantIdMethod
    Thumbnail:          string
    FamilyOriginal:     string
    GenusOriginal:      string
    SpeciesOriginal:    string
    CurrentTaxonId:     Guid option
    CurrentFamily:      string
    CurrentGenus:       string
    CurrentSpecies:     string
    CurrentSpAuth:      string
    CurrentTaxonStatus: string
    Rank:               string
    IsFullyDigitised:   bool
    Images:             List<SlideImage>
    Age:                int
    AgeType:            string
    Location:           string
    LocationType:       string
    PrepYear:           string
    PrepMethod:         string
    PreppedBy:          string
    Mount:              string
    Voided:             bool
}

// Taxonomic Backbone

[<CLIMutable>]
type BackboneTaxon = {
    Id:             Guid
    Group:          string
    Family:         string
    Genus:          string
    Species:        string
    FamilyId:       Guid
    GenusId:        Nullable<Guid>
    SpeciesId:      Nullable<Guid>
    NamedBy:        string
    LatinName:      string
    Rank:           string
    ReferenceName:  string
    ReferenceUrl:   string
    TaxonomicStatus:string
    TaxonomicAlias: string
}

// Read-Only Reference Collections

[<CLIMutable>]
type ReferenceCollectionSummary = {
    Id:                 Guid
    Name:               string
    Description:        string
    CuratorFirstNames:  string
    CuratorSurname:     string
    CuratorEmail:       string
    AccessMethod:       string
    Institution:        string
    InstitutionUrl:     string
    SlideCount:         int
    Published:          DateTime
    Version:            int
}

[<CLIMutable>]
type ReferenceCollectionDetail = {
    Id:                 Guid
    Name:               string
    Digitisers:         string list
    Collectors:         string list
    Description:        string
    CuratorFirstNames:  string
    CuratorSurname:     string
    CuratorEmail:       string
    AccessMethod:       string
    Institution:        string
    InstitutionUrl:     string
    Slides:             SlideDetail list
    Published:          DateTime
    Version:            int
}

[<CLIMutable>]
type PublicProfile = {
    UserId:     Guid
    IsPublic:   bool
    Title:      string
    FirstName:  string
    LastName:   string
    Score:      float
    Curator:    bool
    Groups:     string list
}

// Digitisation Features
[<CLIMutable>]
type Magnification = {
    Level:      int
    Image:      string
    PixelWidth: float
}

[<CLIMutable>]
type Calibration = {
    Id:                 Guid
    User:               Guid
    Name:               string
    Camera:             string
    UncalibratedMags:   int list
    Magnifications:     Magnification list
}

[<CLIMutable>]
type EditableRefCollection = {
    Id:                 Guid
    Name:               string
    Description:        string
    CuratorFirstNames:  string
    CuratorSurname:     string
    CuratorEmail:       string
    AccessMethod:       string
    Institution:        string
    InstitutionUrl:     string
    EditUserIds:        Guid list
    LastEdited:         DateTime
    PublishedVersion:   int
    SlideCount:         int
    Slides:             SlideDetail list
    CommentsFromReview: string
    AwaitingReview:     bool
}