module Converters

open System
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Composition
open ReadModels

let validationError property message = 
    Validation [{Property=property; Errors = [message]}]
    |> Error

let toPersistenceError domainResult =
    match domainResult with
    | Ok r -> Ok r
    | Error str -> Error ServiceError.Persistence

let optionToResult opt =
    match opt with
    | Some o -> Ok o
    | None -> Error NotFound

module Identity =

    open ReadStore

    let deserialise<'a> json = 
        let unwrap (ReadStore.Json j) = j
        Serialisation.deserialiseCli<'a> (unwrap json)

    let existingUserOrError get (id:Guid) =
        match RepositoryBase.getSingle<PublicProfile> (id.ToString()) get deserialise with
        | Ok u -> u.UserId |> UserId |> Ok
        | Error e -> NotFound |> Error

    let tryGetMagnification (cal:Calibration) mag =
        cal.Magnifications
        |> List.tryFind (fun m -> m.Level = mag)

    let existingMagnificationOrError get (calId:Guid) (mag:int) =
        match RepositoryBase.getSingle<Calibration> (calId.ToString()) get deserialise with
        | Ok c ->
            match tryGetMagnification c mag with
            | Some m -> MagnificationId (CalibrationId c.Id, mag) |> Ok
            | None -> validationError "Magnification" "Magnification does not exist"
        | Error e -> validationError "Calibration" "Calibration does not exist"

    let existingSlideIdOrError get (collection:Guid) (slide:string) =
        match RepositoryBase.getSingle<EditableRefCollection> (collection.ToString()) get deserialise with
        | Error e -> validationError "CollectionId" "The collection does not exist"
        | Ok c ->
            c.Slides
            |> List.tryFind (fun s -> s.CollectionSlideId = slide)
            |> Option.map (fun s -> SlideId (CollectionId c.Id, slide))
            |> optionToResult

    let existingBackboneTaxonOrError get (taxon:Guid) =
        match RepositoryBase.getSingle<BackboneTaxon> (taxon.ToString()) get deserialise with
        | Error e -> validationError "TaxonId" "The specified taxon does not exist"
        | Ok t -> t.Id |> TaxonId |> Ok

module Spatial =

    let createPoint lat lon : Result<Site,ServiceError> =
        match lat with
        | y when y > -90. && y < 90. ->
            match lon with
            | x when x > -180. && x < 180. -> (Latitude (y * 1.0<DD>), Longitude (x * 1.0<DD>)) |> Ok
            | _ -> validationError "Longitude" "Longitude must be in decimal degrees (i.e. between -180 and 180)"
        | _ -> validationError "Latitude" "Latitude must be in decimal degrees (i.e. between -90 and 90)"

    let createLocality locality district region country =
        match String.IsNullOrEmpty country with
        | false ->
            match String.IsNullOrEmpty region with
            | false -> Some (PlaceName (locality,district,region,country)) |> Ok
            | true -> Some (Country country) |> Ok
        | true -> None |> Ok

    let createCountry country =
        match String.IsNullOrEmpty country with
        | true -> validationError "Country" "Country cannot be empty"
        | false -> country |> Country |> Some |> Ok

    let createContinent continent =
        match continent with
        | "Africa" -> Continent Africa |> Some |> Ok
        | "Asia" -> Continent Asia |> Some |> Ok
        | "Europe" -> Continent Europe |> Some |> Ok
        | "NorthAmerica" -> Continent NorthAmerica |> Some |> Ok
        | "SouthAmerica" -> Continent SouthAmerica |> Some |> Ok
        | "Antarctica" -> Continent Antarctica |> Some |> Ok
        | "Australia" -> Continent Australia |> Some |> Ok
        | _ -> validationError "Location" "The continent specified was not valid"

     // Locality, country, continent, unknown
    let createLocation locationType locality district region country continent =
        match locationType with
        | "Locality" -> createLocality locality district region country
        | "Country" -> createCountry country
        | "Continent" -> createContinent continent
        | "Unknown" -> None |> Ok
        | _ -> validationError "Location" "The specified location type was not valid"

module Temporal =

    let createAge (year:int option) yearType =
        match yearType with
            | "Calendar" -> 
                match year with
                | Some y -> 
                    // Age cannot be in the future
                    // Age cannot be before 1600
                    Ok (Some <| CollectionDate (y * 1<CalYr>))
                | None -> validationError "Year" "The year of botanical collection was missing"
            | "Radiocarbon" ->
                match year with
                | None -> validationError "Year" "An approximate radiocarbon age is required"
                | Some y -> 
                    // Radiocarbon dates are based on 1950, so negatives are allowed up to present year minus 1950 (e.g. -67)
                    Ok (Some <| Radiocarbon (y * 1<YBP>))
            | "Lead210" ->
                match year with
                | None -> validationError "Year" "An approximate lead210 age is required"
                | Some y -> 
                    // Lead210 dates are based on 1950, so negatives are allowed up to present year minus 1950.
                    Ok (Some <| Lead210 (y * 1<YBP>))
            | _ -> validationError "YearType" "The dating type was not in a correct format"

    let createSampleAge (year:Nullable<int>) samplingMethod =
        match samplingMethod with
        | "botanical" ->
            if (year.HasValue) 
                then Ok (Some <| CollectionDate (year.Value * 1<CalYr>)) 
                else validationError "Year" "The year of botanical collection was missing"
        | "environmental" -> validationError "SamplingMethod" "Not supported"
        | "morphological" -> validationError "SamplingMethod" "not supported"
        | _ -> validationError "SamplingMethod" "The sampling method was not valid"

module Image =

    let toStaticImage (img:FloatingCalibrationImage) =

        let getDimensions base64String =
            base64String
            |> Base64Image
            |> AzureImageStore.getImageDimensions

        let isWithinBounds h w coordinate =
            match coordinate with
            | (x,y) when x<w && y<h && x>0 && y>0 -> true
            | _ -> false

        let createCoordinate coordinate h w =
            let valid = coordinate |> isWithinBounds h w
            if valid
            then Ok coordinate 
            else Error <| Validation [{Property="Coordinates"; Errors = ["The coordinates are outwidth the image frame"]}]

        let createFloatingCalibration (measured:float) p1 p2 = { 
            Point1 = p1
            Point2 = p2
            MeasuredDistance = measured * 1.0<um> }

        let dimensions = getDimensions img.PngBase64
        match dimensions with
        | Error e -> Error <| Validation [{Property="PngBase64"; Errors = ["The image was not a valid base64 PNG format"]}]
        | Ok (h,w) ->
            createFloatingCalibration img.MeasuredLength
            <!> createCoordinate (img.X1,img.Y1) h w
            <*> createCoordinate (img.X2,img.Y2) h w
            |> lift (fun fc -> ImageForUpload.Single ((img.PngBase64 |> Base64Image),fc))

    let toFocusImage get (frames: string list) (calId:Guid) (magnification:int) =

        let createBase64 base64 =
            match base64 with
            | Prefix "data:image/png;base64," b64 -> 
                try Convert.FromBase64String b64 |> ignore; base64 |> Base64Image |> Ok
                with
                | _ -> validationError "FramesBase64" "Base 64 was not in a valid format"
            | _ -> validationError "FramesBase64" "Base 64 was not a PNG image. Only PNGs are supported"

        let createImageForUpload frames magId =
            ImageForUpload.Focus (frames,Stepping.Variable,magId)

        match frames.Length with
        | 0 -> validationError "FramesBase64" "No frames were submitted"
        | 1 -> validationError "FramesBase64" "A focus image must have at least two frames"
        | _ ->
            let framesBase64 = frames |> mapResult createBase64
            let mag = Identity.existingMagnificationOrError get calId magnification
            createImageForUpload
            <!> framesBase64
            <*> mag

module Taxonomy =

    let backboneSearchToIdentity (dto:BackboneSearchRequest) =
        match dto.Rank with
        | "Family" -> Ok <| Family (LatinName dto.LatinName)
        | "Genus" -> Ok <| Genus (LatinName dto.LatinName)
        | "Species" -> Ok <| Species (LatinName dto.LatinName,SpecificEphitet dto.Species,Scientific "")
        | _ -> Error "DTO validation failed"

    let createPerson initials lastName =
        if String.IsNullOrEmpty lastName then Person.Unknown
        else 
            let i = initials |> List.map (fun c -> c.ToString())
            Person (i,lastName)

    let createIdentification samplingMethod initials surname taxonId =
        match samplingMethod with
        | "botanical" ->
            let person = createPerson initials surname
            Ok <| Botanical (taxonId, Unknown, person)
        | "environmental" -> Ok <| Environmental taxonId
        | "morphological" -> Ok <| Morphological taxonId
        | _ -> validationError "SamplingMethod" ("Not a valid sampling method: " + samplingMethod)

module Dto =

    let createUnknownGrainCommand id user images temporal spatial =
            GlobalPollenProject.Core.Aggregates.Grain.Command.SubmitUnknownGrain {  
                Id = id 
                Images = images
                SubmittedBy = user
                Temporal = temporal
                Spatial = spatial }

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
            PrepMethod = None
            PrepDate = None
            Mounting = None
        }

    let toSubmitUnknownGrain grainId userId saveImage (request:AddUnknownGrainRequest) =

        let imagesOrError =
            match request.Images.Length with
            | 0 ->
                Validation [{ Property = "Images"; Errors = ["You must submit at least one image"]}]
                |> Error
            | _ ->
                request.Images 
                |> mapResult Image.toStaticImage
                |> bind (mapResult (saveImage >> toPersistenceError))

        let ageOrError =
            Temporal.createSampleAge request.Year request.YearType

        let spaceOrError =
            Spatial.createPoint request.LatitudeDD request.LongitudeDD

        createUnknownGrainCommand grainId userId
        <!> imagesOrError
        <*> ageOrError
        <*> spaceOrError

    let toAddSlideCommand get (dto:SlideRecordRequest) =

        let existingId =
            match String.IsNullOrEmpty dto.ExistingId with
            | true -> None
            | false -> Some dto.ExistingId

        let ageOrError =
            Temporal.createSampleAge dto.YearCollected dto.SamplingMethod

        let placeOrError =
            Spatial.createLocation dto.LocationType dto.LocationLocality dto.LocationDistrict dto.LocationRegion dto.LocationCountry dto.LocationContinent

        let taxonIdOrError =
            Identity.existingBackboneTaxonOrError get dto.ValidatedTaxonId 

        let taxonOrError =
            taxonIdOrError
            |> bind (Taxonomy.createIdentification dto.SamplingMethod dto.CollectedByInitials dto.CollectedBySurname)

        createAddSlideCommand (CollectionId dto.Collection) existingId dto.OriginalFamily dto.OriginalGenus dto.OriginalSpecies dto.OriginalAuthor
        <!> taxonOrError
        <*> placeOrError
        <*> ageOrError

    let toAddSlideImageCommand readStoreGet saveImage (request:SlideImageRequest) =
        let imageForUploadOrError =
            match request.IsFocusImage with
            | true -> Image.toFocusImage readStoreGet request.FramesBase64 request.CalibrationId request.Magnification
            | false -> 
                match request.FramesBase64.Length with
                | 0 -> Error <| Validation [{ Property = "FramesBase64"; Errors = ["No frames were submitted"]}]
                | 1 -> 
                    match request.FloatingCalPointOneX.HasValue
                       && request.FloatingCalPointOneY.HasValue
                       && request.FloatingCalPointTwoX.HasValue
                       && request.FloatingCalPointTwoY.HasValue 
                       && request.MeasuredDistance.HasValue with
                        | true ->
                            let img = {
                                PngBase64 = request.FramesBase64.Head
                                X1 = request.FloatingCalPointOneX.Value
                                X2 = request.FloatingCalPointTwoX.Value
                                Y1 = request.FloatingCalPointOneY.Value
                                Y2 = request.FloatingCalPointTwoY.Value
                                MeasuredLength = request.MeasuredDistance.Value
                            }
                            Image.toStaticImage img
                        | false -> Error <| Validation [{ Property = "Coordinates"; Errors = ["Invalid coordinates"]}]
                | _ -> Error <| Validation [{ Property = "FramesBase64"; Errors = ["You submitted more than one frame"]}]

        let slideIdOrError = 
            Identity.existingSlideIdOrError readStoreGet request.CollectionId request.SlideId

        let yearTakenOrError =
            1970<CalYr> |> Ok

        let createUploadImageCommand id image yearTaken =
            GlobalPollenProject.Core.Aggregates.ReferenceCollection.Command.UploadSlideImage {
                Id = id
                Image = image
                YearTaken = yearTaken }

        createUploadImageCommand
        <!> slideIdOrError
        <*> (imageForUploadOrError |> bind (saveImage >> toPersistenceError))
        <*> yearTakenOrError


module DomainToDto =
    let unwrapGrainId (GrainId e) = e
    let unwrapTaxonId (TaxonId e) = e
    let unwrapUserId (UserId e) = e
    let unwrapRefId (CollectionId e) : Guid = e
    let unwrapCalId (CalibrationId e) = e
    let unwrapSlideId (SlideId (e,f)) = e,f
    let unwrapLatin (LatinName ln) = ln
    let unwrapId (TaxonId id) = id
    let unwrapEph (SpecificEphitet e) = e
    let unwrapAuthor (Scientific a) = a
    let unwrapColVer (ColVersion a) = a

    let image getMag toAbsoluteUrl (domainImage:Image) : SlideImage =
        match domainImage with
        | SingleImage (i,cal) ->
            { Id = 0
              Frames = [i |> toAbsoluteUrl |> Url.unwrap]
              PixelWidth = 2. }
        | FocusImage (urls,stepping,calId) ->
            let magnification = getMag calId
            match magnification with
            | None -> invalidOp "DTO validation failed"
            | Some (mag:Magnification) ->
                { Id = 0
                  Frames = urls |> List.map (toAbsoluteUrl >> Url.unwrap)
                  PixelWidth = mag.PixelWidth }
