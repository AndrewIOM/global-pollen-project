[<AutoOpen>]
module GlobalPollenProject.Core.Types

open System

type Dependencies = {GenerateId: unit -> Guid}

type RootAggregateId = System.Guid
type RootAggregate<'TState, 'TCommand, 'TEvent> = {
    initial : 'TState
    evolve : 'TState -> 'TEvent -> 'TState
    handle : Dependencies -> 'TCommand -> 'TState -> 'TEvent list
    getId: 'TCommand -> RootAggregateId }

// Identities
type UserId = UserId of RootAggregateId
type ClubId = ClubId of RootAggregateId
type CollectionId = CollectionId of RootAggregateId
type SlideId = SlideId of CollectionId * string
type GrainId = GrainId of RootAggregateId
type TaxonId = TaxonId of RootAggregateId

// Specialist Types
type Url = Url of string
type Base64Image = Base64Image of string

// Taxonomy
type Rank =
| Family
| Genus
| Species
| Subspecies

type LatinName = LatinName of string

// Services
type TaxonomicBackbone = TaxonomicBackbone of string

// Images
type FileUpload =
    | WaitingForUpload of Base64Image
    | Uploaded of Url

type Image = 
    | FocusImage of Url list
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

type Staining = bool


type SampleType =
    | Palaeopalynology 
    | Melissopalynology
    | Aeropalynology
    | PollinationBiology
    | ReferenceMaterial
    | Forensic


// Pollen Traits
[<Measure>]
type um // Micrometre

type GrainDiameter = float<um>
type WallThickness = float<um>

type GrainShape =
    | Circular
    | Triangular
    | Unsure

type Patterning =
    | Regular
    | No
    | Unsure

type Sculpturing =
    | Spikes
    | No

type Pores =
    | Pore
    | PoreWithFurrow
    | No