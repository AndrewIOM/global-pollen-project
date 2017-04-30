module GlobalPollenProject.App.UseCases

open System
open System.IO
open System.Linq
open System.Threading
open Microsoft.Extensions.Configuration

open GlobalPollenProject.Core.Aggregate
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Dependencies
open GlobalPollenProject.Core.Composition
open GlobalPollenProject.Shared.Identity.Models

open AzureImageStore
open ReadModels
open ReadStore

type ServiceError =
| CoreError
| ValidationError
| PersistenceError
| NotFound

type GetCurrentUser = unit -> Guid

type BackboneService = {
    Search: BackboneSearchRequest -> PagedResult<BackboneTaxon>
    Import: string -> unit
}

type TaxonomyService = {
    List: PageRequest -> PagedResult<TaxonSummary>
    GetByName: string -> string -> string -> Result<TaxonSummary,ServiceError>
}

type GrainService = {
    SubmitUnknownGrain: Guid -> string list -> int -> float -> float -> Result<GrainId,ServiceError>
}

type DigitiseService = {
    ListAllCollections:     unit                        -> Result<ReferenceCollectionSummary list,ServiceError>
    GetMyCollections:       GetCurrentUser              -> Result<ReferenceCollectionSummary list,ServiceError>
    GetCollectionDetail:    Guid                        -> Result<ReferenceCollection, ServiceError>
    StartNewCollection:     StartCollectionRequest      -> GetCurrentUser -> Result<CollectionId,ServiceError>
    AddSlideRecord:         SlideRecordRequest          -> Result<SlideId,ServiceError>
    AddSlideImage:          SlideImageRequest           -> Result<unit,ServiceError>
}

type UserService = {
    RegisterProfile: NewAppUserRequest -> GetCurrentUser -> Result<UserId,ServiceError>
}

type AppServices = {
    Backbone: BackboneService
    Grain: GrainService
    Taxonomy: TaxonomyService
    User: UserService
    Digitise: DigitiseService
}

module Digitise =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection

    let appService (deps:Dependencies) eventStore redis =

        let issueCommand = 
            let aggregate = { initial = State.Initial; evolve = State.Evolve; handle = handle; getId = getId }
            EventStore.makeCommandHandler eventStore "ReferenceCollection" aggregate deps
        
        let startNewCollection (request:StartCollectionRequest) getCurrentUser =
            let newId = CollectionId <| deps.GenerateId()
            let currentUser = UserId <| getCurrentUser()
            issueCommand <| CreateCollection { Id = newId; Name = request.Name; Owner = currentUser; Description = request.Description }
            Success newId
        
        let addMeta request = 
            invalidOp "Not Implemented"
            // let identification = Botanical (TaxonId request.BackboneTaxonId)
            // handle (AddSlide { Id = CollectionId request.Collection; Taxon = identification; Place = None; Time = None })
            // Success (SlideId (CollectionId request.Collection,"SL001"))

        let addImage request = 
            invalidOp "Not Implemented"
            // let base64 = Base64Image request.ImageBase64
            // let toUpload = Single base64
            // let uploaded = deps.UploadImage toUpload
            // let slideId = SlideId ((CollectionId request.CollectionId), request.SlideId)
            // handle (UploadSlideImage { Id = slideId; Image = uploaded })
            // Success()

        let listCollections () = Success <| ReadStore.Digitisation.listUserCollections redis

        let myCollections getCurrentUser = 
            Success <| ReadStore.Digitisation.listUserCollections redis
            // let userId = getCurrentUser()
            // let readModel = projections.ReferenceCollectionSummary |> Seq.filter (fun rc -> rc.User = userId) |> Seq.toList
            // Success readModel

        let getCollection id =
            invalidOp "Hello"
            // let result = projections.ReferenceCollection.Include(fun x -> x.Slides) |> Seq.tryFind (fun rc -> rc.Id = id)
            // match result with
            // | Some rc -> Success rc
            // | None -> Failure NotFound

        { StartNewCollection    = startNewCollection
          ListAllCollections    = listCollections
          AddSlideRecord        = addMeta
          AddSlideImage         = addImage
          GetMyCollections      = myCollections
          GetCollectionDetail   = getCollection }


module Grain =

    open GlobalPollenProject.Core.Aggregates.Grain

    let appService (deps:Dependencies) eventStore =

        let issueCommand = 
            let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
            EventStore.makeCommandHandler eventStore "Specimen" aggregate deps

        let submitUnknownGrain grainId (images:string list) age (lat:float) lon =
            let id = GrainId grainId
            let uploadedImages = images |> List.map (fun x -> SingleImage (Url.create x))
            let spatial = Latitude (lat * 1.0<DD>), Longitude (lon * 1.0<DD>)
            let temporal = CollectionDate (age * 1<CalYr>)
            let userId = UserId (Guid.NewGuid())
            issueCommand <| SubmitUnknownGrain {Id = id; Images = uploadedImages; SubmittedBy = userId; Temporal = Some temporal; Spatial = spatial }
            Success id

        let identifyUnknownGrain grainId taxonId =
            invalidOp "Not Implemented"
            handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId; IdentifiedBy = UserId (Guid.NewGuid()) })

        let listUnknownGrains() =
            invalidOp "Not Implemented"
        
        {SubmitUnknownGrain = submitUnknownGrain }


module Taxonomy =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let appService (deps:Dependencies) eventStore =

        let issueCommand = 
            let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
            EventStore.makeCommandHandler eventStore "Taxon" aggregate deps

        let list (request:PageRequest) =
            invalidOp "Not Implemented"
            // let result = projections.TaxonSummary.Skip(request.Page - 1 * request.PageSize).Take(request.PageSize) |> Seq.toList
            // { Items = result; CurrentPage = request.Page; TotalPages = 2; ItemsPerPage = request.PageSize; ItemTotal = result.Length }

        let getByName family genus species =
            invalidOp "Not Implemented"
            // let taxon = projections.TaxonSummary
            //             |> Seq.tryFind (fun taxon -> taxon.Family = family && taxon.Genus = genus && taxon.Species = species)
            // match taxon with
            // | Some t -> Success t
            // | None -> Failure NotFound

        {List = list; GetByName = getByName }


module Backbone =

    open GlobalPollenProject.Core.Aggregates.Taxonomy
    open ImportTaxonomy

    let appService (deps:Dependencies) eventStore =

        let issueCommand = 
            let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
            EventStore.makeCommandHandler eventStore "Taxon" aggregate deps

        let importAll filePath =
            let taxa = (readPlantListTextFile filePath) |> List.filter (fun x -> x.TaxonomicStatus = "accepted") |> List.take 5000
            let mutable commands : Command list = []
            for row in taxa do
                let additionalCommands = createImportCommands row commands deps.GenerateId
                commands <- List.append commands additionalCommands
            commands |> List.map issueCommand
            ()

        let search (request:BackboneSearchRequest) =
            invalidOp "Not Implemented"
            // let genus = if isNull request.Genus then "" else request.Genus
            // let species = if isNull request.Species then "" else request.Species

            // let result = projections.BackboneTaxon.Where(fun m ->
            //     m.Rank = request.Rank
            //     && m.LatinName.Contains request.LatinName
            //     && m.Family.Contains request.Family
            //     && m.Genus.Contains genus
            //     && m.Species.Contains species ) |> Seq.toList
            // let totalPages = float result.Length / 10. |> Math.Ceiling |> int
            // { CurrentPage = 1; ItemTotal = result.Length; TotalPages = totalPages; ItemsPerPage = 10; Items = if result.Length > 10 then result |> List.take 10 else result }

        { Search = search; Import = importAll }


module User = 

    open GlobalPollenProject.Core.Aggregates.User
    
    let appService (deps:Dependencies) eventStore =

        let issueCommand = 
            let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
            EventStore.makeCommandHandler eventStore "User" aggregate deps
        
        let register (newUser:NewAppUserRequest) (getUserId:GetCurrentUser) =
            let id = UserId (getUserId())
            issueCommand <| Register { Id = id; Title = newUser.Title; FirstName = newUser.FirstName; LastName = newUser.LastName }
            Success id
        
        { RegisterProfile = register }

let composeApp () : AppServices =

    // Load AppSettings
    let appSettings = ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build()

    // Connect to Persistence Stores
    let imageUploader = AzureImageStore.uploadToAzure "Development" appSettings.["imagestore:azureconnectionstring"] (fun x -> Guid.NewGuid().ToString())
    let redis = 
        let ip = appSettings.["readstore:redisip"]
        StackExchange.Redis.ConnectionMultiplexer.Connect(ip)
    let eventStore = 
        let ip = appSettings.["eventstore:eventstoreip"]
        let port = appSettings.["eventstore:eventstoreport"] |> int
        let username = appSettings.["eventstore:eventstoreuser"]
        let pass = appSettings.["eventstore:eventstorepassword"]
        EventStore.connect ip port username pass
        |> EventStore.subscribe (ProjectionHandler.project redis)

    // App Core Dependencies
    let dependencies = 
        let log = ignore
        let gbifLink = ExternalLink.getGbifId
        let neotomaLink = ExternalLink.getNeotomaId

        // TODO Move this to backbone repository
        let taxonomicBackbone (query:BackboneQuery) : TaxonId option =
            match query with
            | ValidateById id -> 
                let u (TaxonId id) = id
                let t = ReadStore.BackboneTaxonomy.tryFindById (u id) redis
                match t with
                | Some t -> Some (TaxonId t.Id)
                | None -> None
            | Validate identity ->
                None // TODO implement

        let calculateIdentity = calculateTaxonomicIdentity taxonomicBackbone
    
        { GenerateId          = Guid.NewGuid
          Log                 = log
          UploadImage         = imageUploader
          GetGbifId           = gbifLink
          GetNeotomaId        = neotomaLink
          ValidateTaxon       = taxonomicBackbone
          CalculateIdentity   = calculateIdentity }
    
    // App Services
    { Backbone      = Backbone.appService dependencies eventStore
      Taxonomy      = Taxonomy.appService dependencies eventStore
      Grain         = Grain.appService dependencies eventStore
      Digitise      = Digitise.appService dependencies eventStore redis
      User          = User.appService dependencies eventStore }