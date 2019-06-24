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
type RootAggregateId = Guid
type UserId = UserId of RootAggregateId
type ClubId = ClubId of RootAggregateId
type CollectionId = CollectionId of RootAggregateId
type SlideId = SlideId of CollectionId * string
type GrainId = GrainId of RootAggregateId
type TaxonId = TaxonId of RootAggregateId
type CalibrationId = CalibrationId of RootAggregateId
type MagnificationId = MagnificationId of CalibrationId * int

[<AutoOpen>]
module ShortText =
    type ShortText = ShortText of string
    let create (str:string) =
        if String.IsNullOrEmpty str then Error "The string was empty"
        else
            match str.Length with
            | l when l < 100 && l > 0 -> ShortText str |> Ok
            | _ -> Error "The string was too long"

[<AutoOpen>]
module LongformText =
    type LongformText = LongformText of string
    let create (str:string) =
        if String.IsNullOrEmpty str then Error "The string was empty"
        else
            match str.Length with
            | l when l > 0 -> LongformText str |> Ok
            | _ -> Error "The string was empty"

[<AutoOpen>]
module InstitutionCode =
    type InstitutionCode = InstitutionCode of string
    let create (str:string) =
        if String.IsNullOrEmpty str then Error "The string was empty"
        else
            let m = System.Text.RegularExpressions.Regex.Match(str, "^[A-Z]{1,8}$")
            match m.Success with
            | true -> InstitutionCode str |> Ok
            | false -> Error "The institution code must be between one and eight capital letters"

[<AutoOpen>]
module Url =
    type Url = Url of string
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

[<AutoOpen>]
module EmailAddress =
    type EmailAddress = EmailAddress of string
    let create mail =
        let regex = Text.RegularExpressions.Regex(@"\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*")
        match regex.IsMatch mail with
        | true -> EmailAddress mail |> Ok
        | false -> Error "The email address is not in a valid format"

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

type CartesianBox = {
    TopLeft: int * int
    BottomRight: int * int
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
type FirstName = string
type Surname = string
type Person = 
| Person of FirstName list * Surname
| Unknown
type PlantMaterialCollector = Person

type HerbariumVoucher = {
    HerbariumCode: InstitutionCode
    InternalIdentifier: ShortText
}

type PlantInLivingCollection = {
    BotanicGardenCode: InstitutionCode
    InternalIdentifier: ShortText 
}

type PlantIdentificationMethod =
   | Unknown
   | HerbariumVoucher of HerbariumVoucher
   | LivingCollection of PlantInLivingCollection
   | Field of Person

type TaxonIdentification =
    | Botanical of TaxonId * PlantIdentificationMethod * PlantMaterialCollector
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

type CitizenScienceTrait =
| Shape of GrainShape
| Size of GrainDiameter
| Wall of WallThickness
| Pattern of Patterning
| Pores of Pores