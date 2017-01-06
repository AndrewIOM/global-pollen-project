[<AutoOpen>]
module GlobalPollenProject.Core.Types

open System

// Identities
type UserId = UserId of string
type OrgId = OrgId of string
type CollectionId = CollectionId of int
type SlideId = SlideId of CollectionId * string
type GrainId = GrainId of Guid
type TaxonId = TaxonId of int

// Specialist Types
type Url = Url of string
type Base64Image = Base64Image of string

// Services
type TaxonomicBackbone = TaxonomicBackbone of string

// Images
type FileUpload =
    | WaitingForUpload of Base64Image
    | Uploaded of Url

type Image = 
    | FocusImage of Url list
    | SingleImage of Url

// Space and Time
[<Measure>]
type DD

[<Measure>]
type CalYr

[<Measure>]
type YBP


type Latitude = Latitude of float<DD>
type Longitude = Longitude of float<DD>
type Point = Latitude * Longitude
type Polygon = Point list

type Continent = 
    | Asia
    | America
    | Europe

type SamplingLocation =
    | Site of Point
    | Region of Polygon
    | Country of string // Country name
    | Continent of Continent

type Age =
    | CollectionDate of int<CalYr>
    | Radiocarbon of int<YBP>
    | Lead210 of int<YBP>

// Reference Material Metadata
type StorageMedium =
    | Glycerol

// // Pollen Traits
// [<Measure>]
// type um // Micrometre

// type GrainDiameter = float<um>
// type WallThickness = float<um>

// type Patterning =
//     | None
//     | Some

// type Sculpturing =
//     | Spikes
//     | None

// type PollenTraits = {
//     WallThickness: WallThickness option
//     Patterning: Patterning option
// }

