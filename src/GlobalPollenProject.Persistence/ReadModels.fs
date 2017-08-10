module ReadModels

open System

// Helper DTOs
[<CLIMutable>]
type FocusableImage = {
    FrameUrls:  string list
}

[<CLIMutable>]
type StandardImage = {
    Url:        string
}

[<CLIMutable>]
type SlideImage = {
    Id:                     int
    Frames:                 List<string>
    PixelWidth:             float
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
}

[<CLIMutable>]
type Node = {
    Id:         Guid
    Name:       string
    Rank:       string
}

[<CLIMutable>]
type TaxonDetail = {
    Id:         Guid
    Family:     string
    Genus:      string
    Species:    string
    LatinName:  string
    Authorship: string
    Rank:       string
    Parent:     Node option
    Children:   Node list
    Slides:     SlideSummary list
    Grains:     GrainSummary list
}

// Grains
// - Crowdsourced / Unknown Grains
// - Grains split out of digitised slides

[<CLIMutable>]
type GrainDetail = {
    Id:                 Guid
    Images:             StandardImage list
    FocusImages:        FocusableImage list
    Identifications:    Guid list
    ConfirmedFamily:    string
    ConfirmedGenus:     string
    ConfirmedSpecies:   string
}

// Reference Slides
// - Not split into grains

[<CLIMutable>]
type SlideDetail = {
    CollectionId:       Guid
    CollectionSlideId:  string
    Thumbnail:          string
    FamilyOriginal:     string
    GenusOriginal:      string
    SpeciesOriginal:    string
    CurrentTaxonId:     Guid option
    CurrentFamily:      string
    CurrentGenus:       string
    CurrentSpecies:     string
    CurrentSpAuth:      string
    Rank:               string
    IsFullyDigitised:   bool
    Images:             List<SlideImage>
}

// Taxonomic Backbone

[<CLIMutable>]
type BackboneTaxon = {
    Id:             Guid
    Family:         string
    Genus:          string
    Species:        string
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
    Id:         Guid
    Name:       string
    Description:string
    SlideCount: int
    Published:  DateTime
    Version:    int
}

[<CLIMutable>]
type ReferenceCollectionDetail = {
    Id:             Guid
    Name:           string
    Contributors:   string list
    Description:    string
    Slides:         SlideDetail list
    Published:      DateTime
    Version:        int
}

// User Profiles

[<CLIMutable>]
type PublicProfile = {
    UserId:     Guid
    IsPublic:   bool
    FirstName:  string
    LastName:   string
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
    Id:             Guid
    User:           Guid
    Name:           string
    Camera:         string
    Ocular:         int
    Objectives:     int list
    Magnifications: Magnification list
}

[<CLIMutable>]
type EditableRefCollection = {
    Id:                 Guid
    Name:               string
    Description:        string
    EditUserIds:        Guid list
    LastEdited:         DateTime
    PublishedVersion:   int
    SlideCount:         int
    Slides:             SlideDetail list
}