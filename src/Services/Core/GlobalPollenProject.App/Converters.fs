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

let toValidationResult domainResult =
    match domainResult with
    | Ok r -> Ok r
    | Error err -> validationError "" err

let optionToResult opt =
    match opt with
    | Some o -> Ok o
    | None -> Error NotFound

let isPositiveNumber f = if f > 0. && f <> nan && f <> infinity then true else false

let positiveNumber f onError =
    if isPositiveNumber f
    then f * 1.<um> |> Ok
    else onError 

module Identity =

    open ReadStore

    let inline deserialise< ^a> json = 
        let unwrap (Json j) = j
        Serialisation.deserialise< ^a> (unwrap json)

    let existingUserOrError get (id:Guid) =
        match RepositoryBase.getSingle<PublicProfile> id get deserialise with
        | Ok u -> u.UserId |> UserId |> Ok
        | Error e -> NotFound |> Error

    let existingGrainOrError get (id:Guid) =
        match RepositoryBase.getSingle<GrainDetail> id get deserialise with
        | Ok u -> u.Id |> GrainId |> Ok
        | Error e -> NotFound |> Error

    let tryGetMagnification (cal:Calibration) mag =
        cal.Magnifications
        |> List.tryFind (fun m -> m.Level = mag)

    let existingMagnificationOrError get (calId:Guid) (mag:int) =
        match RepositoryBase.getSingle<Calibration> calId get deserialise with
        | Ok c ->
            match tryGetMagnification c mag with
            | Some m -> MagnificationId (CalibrationId c.Id, mag) |> Ok
            | None -> validationError "Magnification" "Magnification does not exist"
        | Error e -> validationError "Calibration" "Calibration does not exist"

    let existingSlideIdOrError get (collection:Guid) (slide:string) =
        match RepositoryBase.getSingle<EditableRefCollection> collection get deserialise with
        | Error e -> validationError "CollectionId" "The collection does not exist"
        | Ok c ->
            c.Slides
            |> List.tryFind (fun s -> s.CollectionSlideId = slide)
            |> Option.map (fun s -> SlideId (CollectionId c.Id, slide))
            |> optionToResult

    let existingBackboneTaxonOrError get (taxon:Guid) =
        match RepositoryBase.getSingle<BackboneTaxon> taxon get deserialise with
        | Error e -> validationError "TaxonId" "The specified taxon does not exist"
        | Ok t -> t.Id |> TaxonId |> Ok


module Metadata =

    let createChemicalTreatment method =
        match method with
        | "acetolysis" -> Acetolysis |> Some |> Ok
        | "fresh" -> FreshGrains |> Some |> Ok
        | "hf" -> HydrofluoricAcid |> Some |> Ok
        | "unknown" -> None |> Ok
        | _ -> validationError "ChemicalTreatment" "An unrecognised chemical treatment was specified"

    let createPrepDate (date:Nullable<int>) =
        match date.HasValue with
        | false -> None |> Ok
        | true -> date.Value * 1<CalYr> |> Some |> Ok

    let createMountingMethod method =
        match method with
        | "glycerol" -> GlycerineJelly |> Some |> Ok
        | "siliconeoil" -> SiliconeOil |> Some |> Ok
        | "unknown" -> None |> Ok
        | _ -> validationError "MountingMaterial" "An unrecognised mounting method was specified"

    let createPerson firstNames lastName =
        if String.IsNullOrEmpty lastName then Person.Unknown
        else 
            let i = firstNames |> List.map (fun c -> c.ToString())
            Person (i,lastName)

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection

    let createAccess accessMethod institutionName institutionUrl =
        match accessMethod with
        | "digitial" -> DigitialOnly |> Ok
        | "institution" -> 
            let createInstitution name web = { Name = name; Web = web }
            createInstitution
            <!> ShortText.create institutionName
            <*> (Url.create institutionUrl |> Some |> Ok)
            |> lift Institutional
            |> lift PrimaryLocation
            |> toValidationResult
        | "private" -> Personal |> PrimaryLocation |> Ok
        | _ -> validationError "Access" "Material access info was not formatted correctly"


    let createCurator (firstNames:string) lastName email =
        if String.IsNullOrEmpty lastName 
        then validationError "CuratorLastName" "A curator requires a last name"
        else 
            let firstNames = firstNames.Split(' ') |> Array.toList
            let create forenames surname email = { Forenames = forenames; Surname = surname; Contact = Email email}
            create firstNames lastName
            <!> EmailAddress.create email
            |> toValidationResult

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

    let createAge (year:Nullable<int>) yearType =
        match yearType with
            | "Calendar" -> 
                match year.HasValue with
                | true -> 
                    // Age cannot be in the future
                    // Age cannot be before 1600
                    Ok (Some <| CollectionDate (year.Value * 1<CalYr>))
                | false -> validationError "Year" "The year of botanical collection was missing"
            | "Radiocarbon" ->
                match year.HasValue with
                | false -> validationError "Year" "An approximate radiocarbon age is required"
                | true -> 
                    // Radiocarbon dates are based on 1950, so negatives are allowed up to present year minus 1950 (e.g. -67)
                    Ok (Some <| Radiocarbon (year.Value * 1<YBP>))
            | "Lead210" ->
                match year.HasValue with
                | false -> validationError "Year" "An approximate lead210 age is required"
                | true -> 
                    // Lead210 dates are based on 1950, so negatives are allowed up to present year minus 1950.
                    Ok (Some <| Lead210 (year.Value * 1<YBP>))
            | "Unknown" -> None |> Ok
            | _ -> validationError "YearType" "The dating type was not in a correct format"

    let createSampleAge (year:Nullable<int>) samplingMethod =
        match samplingMethod with
        | "botanical" ->
            if (year.HasValue) 
                then Ok (Some <| CollectionDate (year.Value * 1<CalYr>)) 
                else Ok <| None
        | "environmental" -> validationError "SamplingMethod" "Not supported"
        | "morphological" -> validationError "SamplingMethod" "not supported"
        | _ -> validationError "SamplingMethod" "The sampling method was not valid"

module Image =

    let toStaticImage (img:FloatingCalibrationImage) =

        let getDimensions base64String =
            base64String
            |> Base64Image
            |> AzureImageStore.getImageDimensionsFromBase64

        let isWithinBounds h w coordinate =
            match coordinate with
            | (x,y) when x<w && y<h && x>0 && y>0 -> true
            | _ -> false

        let createCoordinate coordinate h w =
            let removeUnit (x:int<_>) = int x
            let valid = coordinate |> isWithinBounds (removeUnit h) (removeUnit w)
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
        | Ok dims ->
            createFloatingCalibration img.MeasuredLength
            <!> createCoordinate (img.X1,img.Y1) dims.Height dims.Width
            <*> createCoordinate (img.X2,img.Y2) dims.Height dims.Width
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
        | "Species" ->
            match dto.Species with
            | Some sp -> Ok <| Species (LatinName dto.LatinName,SpecificEphitet sp,Scientific "")
            | None -> Ok <| Species (LatinName dto.LatinName,SpecificEphitet "",Scientific "")
        | _ -> Error "Invalid request format"

    let createLivingSpecimen institutionCode internalId =
        { BotanicGardenCode = institutionCode; InternalIdentifier = internalId }

    let createHerbariumVoucher institutionCode internalId =
        { HerbariumCode = institutionCode; InternalIdentifier = internalId }

    let createIdentification samplingMethod (plantIdMethod:PlantIdMethod) collectedByFirstNames collectedByLastName taxonId =
        let collector = Metadata.createPerson collectedByFirstNames collectedByLastName
        match samplingMethod with
        | "botanical" ->
            match plantIdMethod.Method with
            | "unknown" -> Ok <| Botanical (taxonId, Unknown, collector)
            | "field" ->
                let person = Metadata.createPerson plantIdMethod.IdentifiedByFirstNames plantIdMethod.IdentifiedBySurname
                Ok <| Botanical (taxonId, Field person, collector)
            | "livingCollection" ->
                createLivingSpecimen
                <!> InstitutionCode.create plantIdMethod.InstitutionCode
                <*> ShortText.create plantIdMethod.InternalId
                |> lift (fun l -> Botanical (taxonId, LivingCollection l, collector))
                |> toValidationResult
            | "voucher" ->
                createHerbariumVoucher
                <!> InstitutionCode.create plantIdMethod.InstitutionCode
                <*> ShortText.create plantIdMethod.InternalId
                |> lift (fun l -> Botanical (taxonId, HerbariumVoucher l, collector))
                |> toValidationResult
            | _ -> validationError "PlantIdMethod" "Not a valid plant id method"
        // TODO Implement digitisation functions for env and morph types:
        | "environmental"
        | "morphological" -> validationError "SamplingMethod" "environmental and morphological not implemented"
        | _ -> validationError "SamplingMethod" ("Not a valid sampling method: " + samplingMethod)


module Dto =

    let createUnknownGrainCommand id user images temporal spatial =
            GlobalPollenProject.Core.Aggregates.Grain.Command.SubmitUnknownGrain {  
                Id = id 
                Images = images
                SubmittedBy = user
                Temporal = temporal
                Spatial = spatial }

    let createAddSlideCommand colId id f g s auth preparedBy taxon place age treatment treatmentDate mounting =
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
            PrepMethod = treatment
            PrepDate = treatmentDate
            Mounting = mounting
            PreparedBy = preparedBy
        }

    let createDelineateGrainCommand userId id image delineations =
            GlobalPollenProject.Core.Aggregates.ReferenceCollection.DelineateSpecimensOnImage {  
                Slide = id 
                Image = image
                By = userId
                Delineations = delineations }

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
            Temporal.createAge request.Year request.YearType

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
            |> bind (Taxonomy.createIdentification dto.SamplingMethod dto.PlantIdMethod dto.CollectedByFirstNames dto.CollectedBySurname)

        let treatmentOrError =
            Metadata.createChemicalTreatment dto.PreperationMethod

        let prepDateOrError = 
            Metadata.createPrepDate dto.YearSlideMade

        let mountingOrError =
            Metadata.createMountingMethod dto.MountingMaterial

        let preparedBy =
            Metadata.createPerson dto.PreparedByFirstNames dto.PreparedBySurname

        createAddSlideCommand (CollectionId dto.Collection) existingId dto.OriginalFamily dto.OriginalGenus dto.OriginalSpecies dto.OriginalAuthor preparedBy
        <!> taxonOrError
        <*> placeOrError
        <*> ageOrError
        <*> treatmentOrError
        <*> prepDateOrError
        <*> mountingOrError

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
            match request.DigitisedYear.HasValue with
            | true -> Some (request.DigitisedYear.Value * 1<CalYr>) |> Ok
            | false -> None |> Ok

        let createUploadImageCommand id image yearTaken =
            GlobalPollenProject.Core.Aggregates.ReferenceCollection.Command.UploadSlideImage {
                Id = id
                Image = image
                YearTaken = yearTaken }

        createUploadImageCommand
        <!> slideIdOrError
        <*> (imageForUploadOrError |> bind (saveImage >> toPersistenceError))
        <*> yearTakenOrError

    let toDelinateGrainCommand userId readStoreGet (request:DelineateSpecimenRequest) =

        let slideIdOrError = 
            Identity.existingSlideIdOrError readStoreGet request.CollectionId request.SlideId

        let imageNumberOrError =
            if request.ImageNumber > 100 || request.ImageNumber < 0 
            then Error <| Validation [{ Property = "ImageNumber"; Errors = ["Image number must be 1-100"]}]
            else Ok request.ImageNumber

        let delineationsOrError =
            if request.Delineations |> Seq.isEmpty
            then Error <| Validation [{ Property = "Delineations"; Errors = ["Must submit at least one delineation"]}]
            else
                request.Delineations
                |> List.map(fun d -> 
                    if isPositiveNumber d.X1 && isPositiveNumber d.X2 
                        && isPositiveNumber d.Y1 && isPositiveNumber d.Y2 then Ok d
                    else Error <| Validation [{ Property = "Delineations"; Errors = ["All delineations must have valid positive pixel numbers"]}]
                )
                |> List.map(fun d ->
                    match d with
                    | Error e -> Error e
                    | Ok d ->
                        { TopLeft = { X = List.min [ d.X1; d.X2 ] * 1<pixels>; Y = List.min [ d.Y1; d.Y2] * 1<pixels> }
                          BottomRight = { X = List.max [ d.X1; d.X2 ] * 1<pixels>; Y = List.max [ d.Y1; d.Y2] * 1<pixels> }} |> Ok)
                |> mapResult id

        createDelineateGrainCommand userId
        <!> slideIdOrError
        <*> imageNumberOrError
        <*> delineationsOrError

module Traits =

    let toTrait t (value: string option) (value1: float option) (value2: float option) =
        let value = if value.IsSome then value.Value else ""
        let value1 = if value1.IsSome then value1.Value else Int32.MinValue
        let value2 = if value2.IsSome then value2.Value else Int32.MinValue
        match t with
        | "shape" -> 
            match value with
            | "bisacchate" -> Shape Bisacchate |> Ok
            | "circular" -> Shape Circular |> Ok
            | "ovular" -> Shape Ovular |> Ok
            | "triangular" -> Shape Triangular |> Ok
            | "trilobate" -> Shape Trilobate |> Ok
            | "pentagon" -> Shape Pentagon |> Ok
            | "hexagon" -> Shape Hexagon |> Ok
            | "unsure"-> Shape GrainShape.Unsure |> Ok
            | _ -> Error <| Validation [{ Property = "value"; Errors = [sprintf "%s is not a valid shape" value]}]
        | "pattern" ->
            match value with
            | "patterned" -> Pattern Patterned |> Ok
            | "smooth" -> Pattern Smooth |> Ok
            | "unsure" -> Pattern Patterning.Unsure |> Ok
            | _ -> Error <| Validation [{ Property = "value"; Errors = [sprintf "%s is not a valid patterning type" value]}]
        | "pores" ->
            match value with
            | "pore" -> Pores Pore |> Ok
            | "furrow" -> Pores Furrow |> Ok
            | "poreandfurrow" -> Pores PoreAndFurrow |> Ok
            | "no" -> Pores No |> Ok
            | "unsure" -> Pores Unsure |> Ok
            | _ -> Error <| Validation [{ Property = "value"; Errors = [sprintf "%s is not a valid pore type" value]}]
        | "wall" ->
            positiveNumber value1
                (Error <| Validation [{ Property = "value1"; Errors = ["value1 must be a valid wall thickness measurement"]}])
            |> Result.map (fun f -> WallThickness f)
        | "size" ->
            let makeSize v1 v2 = Size (v1, v2)
            let v1 = 
                positiveNumber value1
                    (Error <| Validation [{ Property = "value1"; Errors = ["value1 must be a valid size measurement"]}])
            let v2 = 
                positiveNumber value2
                    (Error <| Validation [{ Property = "value2"; Errors = ["value2 must be a valid size measurement"]}])
            makeSize <!> v1 <*> v2
        | _ -> Error <| Validation [{ Property = "trait"; Errors = [sprintf "%s is not a valid trait" t]}]

module DomainToDto =
    let unwrapGrainId (GrainId e) : Guid = e
    let unwrapTaxonId (TaxonId e) : Guid = e
    let unwrapUserId (UserId e) : Guid = e
    let unwrapRefId (CollectionId e) : Guid = e
    let unwrapCalId (CalibrationId e) : Guid = e
    let unwrapSlideId (SlideId (e,f)) = e,f
    let unwrapLatin (LatinName ln) = ln
    let unwrapEph (SpecificEphitet e) = e
    let unwrapAuthor (Scientific a) = a
    let unwrapColVer (ColVersion a) = a
    let unwrapMagId (MagnificationId (a,b)) : Guid * int = a |> unwrapCalId,b
    let unwrapEmail (EmailAddress e) = e
    let unwrapShortText (ShortText s) = s
    let unwrapLongText (LongformText s) = s
    let unwrapInstitutionCode (InstitutionCode c) = c
    let getPixelWidth p1 p2 dist =
        let removeUnit (x:float<_>) = float x
        let x1,y1 = p1
        let x2,y2 = p2
        let pixelDistance = sqrt ((((float x2)-(float x1))**2.) + (((float y2)-(float y1))**2.))
        let scale (actual:float<_>) image = actual / image
        scale (removeUnit dist) pixelDistance

    let image generateCachedImage getMag toAbsoluteUrl (domainImage:Image) : SlideImage =
        match domainImage with
        | SingleImage (i,cal) ->
            let cachedImage,cachedScaleFactor = i |> generateCachedImage
            let pixelWidth = getPixelWidth cal.Point1 cal.Point2 cal.MeasuredDistance
            { Id = 0
              Frames = [i |> toAbsoluteUrl |> Url.unwrap]
              PixelWidth = pixelWidth
              FramesSmall = [cachedImage |> Url.unwrap]
              PixelWidthSmall = pixelWidth * cachedScaleFactor
              Delineations = [] }
        | FocusImage (urls,_,magId) ->
            let magnification = getMag magId
            match magnification with
            | Error e -> invalidOp "DTO validation failed"
            | Ok o ->
                match o with
                | None -> invalidOp "DTO validation failed"
                | Some (mag:Magnification) ->
                    let smallImages = urls |> List.map generateCachedImage
                    let smallImageScaleFactor = smallImages |> List.map snd |> List.head
                    { Id = 0
                      Frames = urls |> List.map (toAbsoluteUrl >> Url.unwrap)
                      PixelWidth = mag.PixelWidth
                      FramesSmall = smallImages |> List.map (fst >> Url.unwrap)
                      PixelWidthSmall = mag.PixelWidth * smallImageScaleFactor
                      Delineations = [] }

    let calibration id user name microscope =

        let magnifications,cameraName =
            match microscope with
            | Light lm ->
                match lm with
                | Compound (ocular,objectives,camName) -> objectives |> List.map ((*) ocular), camName
                | Single (mag,camName) -> [ int mag ], camName
                | Digital (mag,camName) -> [ int mag ], camName

        { Id                = id |> unwrapCalId
          User              = user |> unwrapUserId
          Name              = name
          Camera            = cameraName
          UncalibratedMags  = magnifications
          Magnifications    = [] }

    let age (domainAge:Age option) =
        let removeUnit (x:int<_>) = int x
        match domainAge with
        | None -> "Unknown",0
        | Some a ->
            match a with
            | CollectionDate d -> "Calendar", d |> removeUnit
            | Radiocarbon d -> "Radiocarbon", d |> removeUnit
            | Lead210 d -> "Lead210", d |> removeUnit

    let location (domainLocation:SamplingLocation option) =
        match domainLocation with
        | None -> "Unknown", ""
        | Some l ->
            match l with
            | Site (lat,lon) -> 
                let removeDD (l:float<_>) : string = (float l).ToString()
                let unwrapLat (Latitude l) = l
                let unwrapLon (Longitude l) = l
                "Site", sprintf "Latitude = %s; Longitude = %s" (unwrapLat lat |> removeDD) (unwrapLon lon |> removeDD)
            | Area poly -> "Area", ""
            | PlaceName (lo,d,r,c) -> "Place name", sprintf "Locality = %s; District = %s; Region = %s; Country = %s" lo d r c 
            | Country c -> "Country", c
            | Ecoregion e -> "Ecoregion", e
            | Continent c ->
                match c with
                | Africa -> "Continent", "Africa"
                | Asia -> "Continent", "Asia"
                | Europe -> "Continent", "Europe"
                | NorthAmerica -> "Continent", "North America"
                | SouthAmerica -> "Continent", "South America"
                | Antarctica -> "Continent", "Antarctica"
                | Australia -> "Continent", "Australia"

    let person (p:Person) =
        let toString : char seq -> string = Seq.map string >> String.concat ""
        match p with
        | Person (firstNames,surname) ->
            match firstNames.Length with
            | 0 -> surname
            | _ -> 
                let initials = firstNames |> List.map (Seq.take 1 >> toString) |> String.concat ". "
                initials + ". " + surname
        | Person.Unknown -> "Unknown"

    let collectorName (identification:TaxonIdentification) =
        match identification with
        | Botanical (id,src,p) -> person p
        | _ -> "Unknown"

    let prepMethod method =
        match method with
        | None -> "Unknown"
        | Some m ->
            match m with
            | Acetolysis -> "acetolysis"
            | FreshGrains -> "fresh"
            | HydrofluoricAcid -> "hf"

    let prepDate date =
        match date with
        | None -> "Unknown"
        | Some y ->
            let removeYr (l:int<_>) : string = (int l).ToString()
            removeYr y

    let mount medium =
        match medium with
        | None -> "Unknown"
        | Some m ->
            match m with
            | SiliconeOil -> "Silicone Oil"
            | GlycerineJelly -> "Glycerine Jelly"

    // Converts taxonomic identification to a tuple of idType(string)*plantId(PlantIdMethod)
    let taxonomicIdentification identification =
        let emptyPlantId = {Method="";InstitutionCode="";InternalId="";IdentifiedByFirstNames=[];IdentifiedBySurname=""}
        match identification with
        | Environmental _ -> "Environmental",emptyPlantId
        | Morphological _ -> "Morphological",emptyPlantId
        | Botanical (_, plantIdMethod, _) -> 
            match plantIdMethod with
            | Unknown -> "Botanical", { emptyPlantId with Method = "Unknown" }
            | HerbariumVoucher v -> 
                "Botanical", 
                { emptyPlantId with Method = "Voucher"
                                    InstitutionCode = v.HerbariumCode |> unwrapInstitutionCode
                                    InternalId = v.InternalIdentifier |> unwrapShortText }
            | LivingCollection l -> 
                "Botanical", 
                { emptyPlantId with Method = "LivingCollection"
                                    InstitutionCode = l.BotanicGardenCode |> unwrapInstitutionCode
                                    InternalId = l.InternalIdentifier |> unwrapShortText }
            | Field p -> 
                "Botanical", 
                { emptyPlantId with Method = "Field"
                                    IdentifiedByFirstNames = []
                                    IdentifiedBySurname = person p }
