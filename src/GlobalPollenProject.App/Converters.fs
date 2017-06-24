module Converters

open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Composition

module DomainToDto =
    let unwrapGrainId (GrainId e) = e
    let unwrapTaxonId (TaxonId e) = e
    let unwrapUserId (UserId e) = e
    let unwrapRefId (CollectionId e) = e
    let unwrapSlideId (SlideId (e,f)) = e,f
    let unwrapLatin (LatinName ln) = ln
    let unwrapId (TaxonId id) = id
    let unwrapEph (SpecificEphitet e) = e
    let unwrapAuthor (Scientific a) = a

module DtoToDomain =

    let backboneSearchToIdentity (dto:BackboneSearchRequest) =
        match dto.Rank with
        | "Family" -> Ok <| Family (LatinName dto.Family)
        | "Genus" -> Ok <| Genus (LatinName dto.Genus)
        | "Species" -> Ok <| Species (LatinName dto.LatinName,SpecificEphitet dto.Species,Scientific "")
        | _ -> Error "DTO validation failed"

    let createAge (year:int option) yearType =
        match yearType with
            | "CollectedBotanically" -> 
                match year with
                | Some y -> Ok (Some <| CollectionDate (y * 1<CalYr>))
                | None -> Error "The year of botanical collection was missing"
            | "Radiocarbon" ->
                match year with
                | None -> Error "An approximate radiocarbon age is required"
                | Some y -> Ok (Some <| Radiocarbon (y * 1<YBP>))
            | "Lead210" ->
                match year with
                | None -> Error "An approximate lead210 age is required"
                | Some y -> Ok (Some <| Lead210 (y * 1<YBP>))
            | _ -> Error "The dating type was not in a correct format"

    let createLocation lat lon : Result<Site,string> =
        Ok <| (Latitude (lat * 1.0<DD>), Longitude (lon * 1.0<DD>))

    let createAddGrainCommand id user time space images =
        GlobalPollenProject.Core.Aggregates.Grain.Command.SubmitUnknownGrain {
            Id = id
            Images = images
            SubmittedBy = user
            Temporal = time
            Spatial = space
        }

    let dtoToGrain (grainId:Result<GrainId,string>) (userId:Result<UserId,string>) (dto:AddUnknownGrainRequest) =

        let timeOrError =
            createAge dto.Year dto.YearType

        let locationOrError =
            createLocation dto.LatitudeDD dto.LongitudeDD

        createAddGrainCommand
        <!> grainId
        <*> userId
        <*> timeOrError
        <*> locationOrError