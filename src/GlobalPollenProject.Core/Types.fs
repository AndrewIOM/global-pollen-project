[<AutoOpen>]
module GlobalPollenProject.Core.Types

open System

type LogMessage =
| Error of string
| Info of string

type RootAggregateId = System.Guid

// Identities
type UserId = UserId of RootAggregateId
type ClubId = ClubId of RootAggregateId
type CalibrationId = CalibrationId of RootAggregateId
type CollectionId = CollectionId of RootAggregateId
type SlideId = SlideId of CollectionId * string
type GrainId = GrainId of RootAggregateId
type TaxonId = TaxonId of RootAggregateId

// Specialist Types
type Url = Url of string

// Taxonomy
type Rank =
| Family
| Genus
| Species
| Subspecies

type LatinName = LatinName of string

// Images
[<Measure>] type um
type Base64Image = Base64Image of string

type ImageUpload =
    | WaitingForUpload of ImageForUpload
    | Success of Image
    | Fail of LogMessage

and Stepping =
| Fixed of float<um>
| Variable

and ImageForUpload =
    | FocusImage of Base64Image list * Stepping * CalibrationId
    | SingleImage of Base64Image

and Image = 
    | FocusImage of Url list * Stepping * CalibrationId
    | SingleImage of Url

// Sample Collection (Space + Time)
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

type Site = Latitude * Longitude

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

// Sample Preperation
type ChemicalTreatments =
    | HF
    | No

type MountingMedium =
    | Glycerol


type SampleType =
    | Palaeopalynology 
    | Melissopalynology
    | Aeropalynology
    | PollinationBiology
    | ReferenceMaterial
    | Forensic

type PalaeoSiteType =
    | Lake

// Taxonomic Identity
type TaxonIdentification =
    | Botanical of TaxonId
    | Environmental of TaxonId
    | Morphological of TaxonId

type IdentificationStatus =
    | Unidentified
    | Partial of TaxonIdentification list
    | Confirmed of TaxonIdentification list * TaxonId

// Pollen Traits
type GrainDiameter = float<um>
type WallThickness = float<um>

type GrainShape =
    | Bisacchate
    | Circular
    | Ovular
    | Triangular
    | Trilobate
    | Pentagon
    | Hexagon
    | Unsure

type Patterning =
    | Patterned
    | Clean
    | Unsure

type Pores =
    | Pore
    | Furrow
    | PoreAndFurrow
    | No
    | Unsure

// Infrastructure
type Dependencies = 
    {GenerateId:        unit -> Guid; 
     Log:               LogMessage -> unit
     UploadImage:       ImageUpload -> Image
     CalculateIdentity: TaxonIdentification list -> TaxonId option }

type RootAggregate<'TState, 'TCommand, 'TEvent> = {
    initial:    'TState
    evolve:     'TState -> 'TEvent -> 'TState
    handle:     Dependencies -> 'TCommand -> 'TState -> 'TEvent list
    getId:      'TCommand -> RootAggregateId }