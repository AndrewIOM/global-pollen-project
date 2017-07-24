module GlobalPollenProject.Core.DomainTypes

open System

type LogMessage =
| DomainError of string
| Info of string

// Identities
type RootAggregateId = System.Guid
type UserId = UserId of RootAggregateId
type ClubId = ClubId of RootAggregateId
type CalibrationId = CalibrationId of RootAggregateId
type CollectionId = CollectionId of RootAggregateId
type SlideId = SlideId of CollectionId * string
type GrainId = GrainId of RootAggregateId
type TaxonId = TaxonId of RootAggregateId


[<AutoOpen>]
module Url =
    type Url = private Url of string
    let create surl =
        Url surl
    let unwrap (Url u) = u

// Images
[<Measure>] type um
type Base64Image = Base64Image of string

type ImageForUpload =
    | Focus of Base64Image list * Stepping * CalibrationId
    | Single of Base64Image
and Image = 
    | FocusImage of Url list * Stepping * CalibrationId
    | SingleImage of Url

and Stepping =
| Fixed of float<um>
| Variable

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

// Taxonomy
type LatinName = LatinName of string
type SpecificEphitet = SpecificEphitet of string
type Authorship = Scientific of string

type TaxonomicGroup =
| Angiosperm
| Gymnosperm
| Pteridophyte
| Bryophyte

type TaxonomicIdentity =
| Family of LatinName
| Genus of LatinName
| Species of LatinName * SpecificEphitet * Authorship

type TaxonomicStatus =
| Accepted
| Doubtful
| Misapplied of TaxonId
| Synonym of TaxonId

// Taxonomic Identity
type IdentificationSource =
    | Book of string

type TaxonIdentification =
    | Botanical of TaxonId * IdentificationSource
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

// Taxonomic Backbone
type BackboneQuery =
| Validate of TaxonomicIdentity
| ValidateById of TaxonId

type LinkRequest = {Family:string;Genus:string option;Species:string option;Identity:TaxonomicIdentity}

// Dependencies
type ImageStore = (unit -> string) -> ImageForUpload -> Image

[<AutoOpen>]
module ColVersion =
    type ColVersion = private ColVersion of int
    let initial =
        ColVersion 0 //Draft
    let unwrap (ColVersion u) = u
    let increment (v:ColVersion) =
        v |> unwrap |> (+) 1 |> ColVersion