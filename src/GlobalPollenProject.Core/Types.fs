module GlobalPollenProject.Core.DomainTypes

open System

let (|Prefix|_|) (p:string) (s:string) =
    if s.StartsWith(p) then
        Some(s.Substring(p.Length))
    else
        None

type LogMessage =
| DomainError of string
| Info of string

// Identities
type RootAggregateId = System.Guid
type UserId = UserId of RootAggregateId
type ClubId = ClubId of RootAggregateId
type CollectionId = CollectionId of RootAggregateId
type SlideId = SlideId of CollectionId * string
type GrainId = GrainId of RootAggregateId
type TaxonId = TaxonId of RootAggregateId
type CalibrationId = CalibrationId of RootAggregateId
type MagnificationId = MagnificationId of CalibrationId * int

[<AutoOpen>]
module Url =
    type Url = Url of string //TODO cannot make private due to Newtonsoft limitation
    type RelativeUrl = RelativeUrl of string
    let unwrap (Url u) = u
    let unwrapRelative (RelativeUrl u) = u
    let create surl =
        Url surl
    let createRelative baseUrl (absoluteUrl:Url) =
        match absoluteUrl |> unwrap with
        | Prefix baseUrl rest -> rest |> RelativeUrl |> Ok
        | _ -> Error "The base URL did not match the absolute URL"
    let relativeToAbsolute baseUrl (relativeUrl:RelativeUrl) =
        let unwrap (RelativeUrl u) = u
        baseUrl + (relativeUrl |> unwrap) |> Url

// Images
[<Measure>] type um
type Base64Image = Base64Image of string

type ImageForUpload =
    | Focus of Base64Image list * Stepping * MagnificationId
    | Single of Base64Image * FloatingCalibration
and Image = 
    | FocusImage of RelativeUrl list * Stepping * MagnificationId
    | SingleImage of RelativeUrl * FloatingCalibration

and Stepping =
| Fixed of float<um>
| Variable

and FloatingCalibration = {
    Point1: int * int
    Point2: int * int
    MeasuredDistance: float<um>
}

// Microscope
[<Measure>] type timesMagnified
[<Measure>] type pixels

type Microscope =
| Light of LightMicroscope

and LightMicroscope =
| Single of Magnification * CameraModel
| Compound of Ocular * Objective list * CameraModel
| Digital of Magnification * CameraModel

and CameraModel = string
and Magnification = int<timesMagnified>
and Ocular = int
and Objective = int

type Label = string


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

type Site = Point
type Country = string
type District = string
type Locality = string
type Region = string
type Ecoregion = string
type Continent = 
    | Africa
    | Asia
    | Europe
    | NorthAmerica
    | SouthAmerica
    | Antarctica
    | Australia

type SamplingLocation =
    | Site      of Point
    | Area      of Polygon
    | PlaceName of Locality * District * Region * Country
    | Country   of Country
    | Ecoregion of Ecoregion
    | Continent of Continent

type Age =
    | CollectionDate of int<CalYr>
    | Radiocarbon of int<YBP>
    | Lead210 of int<YBP>

// Sample Preperation
type ChemicalTreatment =
    | FreshGrains
    | Acetolysis
    | HydrofluoricAcid

type MountingMedium =
    | GlycerineJelly
    | SiliconeOil

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
type Initial = string
type LastName = string
type Person = 
| Person of Initial list * LastName
| Unknown

type IdentificationSource =
   | Unknown
   | Book of string

type TaxonIdentification =
    | Botanical of TaxonId * IdentificationSource * Person
    | Environmental of TaxonId
    | Morphological of TaxonId

type IdentificationStatus =
    | Unidentified
    | Partial of TaxonIdentification list
    | Confirmed of TaxonIdentification list * TaxonId

// Taxonomic Backbone
type BackboneQuery =
| Validate of TaxonomicIdentity
| ValidateById of TaxonId

type LinkRequest = {
    Family:     string
    Genus:      string option
    Species:    string option
    Identity:   TaxonomicIdentity }

[<AutoOpen>]
module ColVersion =
    type ColVersion = ColVersion of int
    let initial =
        ColVersion 0 //Draft
    let unwrap (ColVersion u) = u
    let increment (v:ColVersion) =
        v |> unwrap |> (+) 1 |> ColVersion


// Pollen Traits: Simple
type GrainDiameter = float<um> * float<um>
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
    | Smooth
    | Unsure

type Pores =
    | Pore
    | Furrow
    | PoreAndFurrow
    | No
    | Unsure
