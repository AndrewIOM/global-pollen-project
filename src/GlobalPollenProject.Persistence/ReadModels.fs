module ReadModels

open System

// Images
[<CLIMutable>]
type FocusableImage = {
    FrameUrls: string list
}

[<CLIMutable>]
type StandardImage = {
    Url: string
}

[<CLIMutable>]
type GrainSummary = {
    Id:Guid;
    Thumbnail:string
    HasTaxonomicIdentity: bool 
}

[<CLIMutable>]
type GrainDetail = {
    Id:Guid
    Images: StandardImage list
    FocusImages: FocusableImage list
    Identifications: Guid list
    ConfirmedFamily: string
    ConfirmedGenus: string
    ConfirmedSpecies: string
}

[<CLIMutable>]
type TaxonSummary = {
    Id:Guid;
    Family:string
    Genus:string
    Species:string
    LatinName:string
    Rank:string
    SlideCount:int
    GrainCount:int
    ThumbnailUrl:string
}

[<CLIMutable>]
type BackboneTaxon = {
    Id:Guid;
    Family:string
    Genus:string
    Species:string
    NamedBy:string
    LatinName:string
    Rank:string
    ReferenceName:string
    ReferenceUrl:string
    TaxonomicStatus:string
    TaxonomicAlias:string
}

[<CLIMutable>]
type ReferenceCollectionSummary = {
    Id:Guid;
    User:Guid;
    Name:string;
    Description:string;
    SlideCount:int;
}

[<CLIMutable>]
type Calibration = {
    Id: Guid
    User: Guid
    Device: string
    Ocular: int
    Objective: int
    Image: string
    PixelWidth: float
}

[<CLIMutable>]
type Frame = {
    Id: Guid
    Url: string
}

[<CLIMutable>]
type SlideImage = {
    Id: int
    Frames: List<Frame>
    CalibrationImageUrl: string
    CalibrationFocusLevel: int
    PixelWidth: float
}

[<CLIMutable>]
type Slide = {
    Id:Guid
    CollectionId: Guid
    CollectionSlideId: string
    Taxon: TaxonSummary option
    IdentificationMethod: string
    FamilyOriginal: string
    GenusOriginal: string
    SpeciesOriginal: string
    IsFullyDigitised: bool
    Images: List<SlideImage>
}

[<CLIMutable>]
type ReferenceCollection = {
    Id:Guid;
    User:Guid;
    Name:string;
    Status:string; // Draft etc.
    Version: int;
    Description:string;
    Slides: List<Slide>
}

[<CLIMutable>]
type SlideSummary = {
    Id: Guid
    ThumbnailUrl: string
    TaxonId: Guid
}

[<CLIMutable>]
type PublicProfile = {
    UserId:Guid
    IsPublic:bool
    FirstName:string
    LastName:string
}