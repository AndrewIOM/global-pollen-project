module Converters

open GlobalPollenProject.Core.DomainTypes

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