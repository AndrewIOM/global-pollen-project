module Converters

open System
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Composition
open ReadModels

module DomainToDto =
    let unwrapGrainId (GrainId e) = e
    let unwrapTaxonId (TaxonId e) = e
    let unwrapUserId (UserId e) = e
    let unwrapRefId (CollectionId e) = e
    let unwrapCalId (CalibrationId e) = e
    let unwrapSlideId (SlideId (e,f)) = e,f
    let unwrapLatin (LatinName ln) = ln
    let unwrapId (TaxonId id) = id
    let unwrapEph (SpecificEphitet e) = e
    let unwrapAuthor (Scientific a) = a

    let image getMag (domainImage:Image) : SlideImage =
        match domainImage with
        | SingleImage (i,cal) ->
            { Id = 0
              Frames = [i |> Url.unwrap]
              PixelWidth = 2. }
        | FocusImage (urls,stepping,calId) ->
            let magnification = getMag calId
            match magnification with
            | None -> invalidOp "DTO validation failed"
            | Some (mag:Magnification) ->
                { Id = 0
                  Frames = urls |> List.map Url.unwrap
                  PixelWidth = mag.PixelWidth }

module DtoToDomain =

    let backboneSearchToIdentity (dto:BackboneSearchRequest) =
        match dto.Rank with
        | "Family" -> Ok <| Family (LatinName dto.LatinName)
        | "Genus" -> Ok <| Genus (LatinName dto.LatinName)
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

    let createSampleAge (year:Nullable<int>) samplingMethod =
        match samplingMethod with
        | "botanical" ->
            if (year.HasValue) 
                then Ok (Some <| CollectionDate (year.Value * 1<CalYr>)) 
                else Error "The year of botanical collection was missing"
        | "environmental" -> Error "Not supported"
        | "morphological" -> Error "not supported"
        | _ -> Error "The sampling method was not valid"

    let createPoint lat lon : Result<Site,string> =
        Ok <| (Latitude (lat * 1.0<DD>), Longitude (lon * 1.0<DD>))

    let createRegion region country =
        match String.IsNullOrEmpty country with
        | false ->
            match String.IsNullOrEmpty region with
            | false -> Some (Region (region,country)) |> Ok
            | true -> Some (Country country) |> Ok
        | true -> None |> Ok

    let createAddGrainCommand id user time space images =
        GlobalPollenProject.Core.Aggregates.Grain.Command.SubmitUnknownGrain {
            Id = id
            Images = images
            SubmittedBy = user
            Temporal = time
            Spatial = space
        }

    let createAddSlideCommand colId id f g s auth taxon place age =
        GlobalPollenProject.Core.Aggregates.ReferenceCollection.Command.AddSlide {
            Collection = colId
            ExistingId = id
            Taxon = taxon
            Place = place
            OriginalFamily = f
            OriginalGenus = g
            OriginalSpecies = s
            OriginalAuthor = auth
            Time = age
        }

    let dtoToGrain (grainId:Result<GrainId,string>) (userId:Result<UserId,string>) (dto:AddUnknownGrainRequest) =

        let timeOrError =
            createAge dto.Year dto.YearType

        let locationOrError =
            createPoint dto.LatitudeDD dto.LongitudeDD

        createAddGrainCommand
        <!> grainId
        <*> userId
        <*> timeOrError
        <*> locationOrError

    let dtoToAddSlideCommand (dto:SlideRecordRequest) =

        let existingId =
            match String.IsNullOrEmpty dto.ExistingId with
            | true -> None
            | false -> Some dto.ExistingId

        let ageOrError =
            createSampleAge dto.YearCollected dto.SamplingMethod

        let placeOrError =
            createRegion dto.LocationRegion dto.LocationCountry

        let taxonIdOrError =
            // Check taxon exists
            // Check 'original' values are valid
            // Check original matches taxon through trace to ensure concurrent info
            dto.ValidatedTaxonId
            |> TaxonId
            |> Ok

        let taxon t =
            match dto.SamplingMethod with
            | "botanical" -> Ok <| Botanical (t, Book "Fake plant identification book")
            | "environmental" -> Ok <| Environmental t
            | "morphological" -> Ok <| Morphological t
            | _ -> Error <| "Not a valid sampling method: " + dto.SamplingMethod

        let taxonOrError =
            taxonIdOrError
            >>= taxon

        createAddSlideCommand (CollectionId dto.Collection) existingId dto.OriginalFamily dto.OriginalGenus dto.OriginalSpecies dto.OriginalAuthor
        <!> taxonOrError
        <*> placeOrError
        <*> ageOrError
