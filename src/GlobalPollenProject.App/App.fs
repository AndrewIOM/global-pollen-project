module GlobalPollenProject.App

(*  App Orchestration 
    This assembly orcestrates app configuration, and sets up communication
    between any user interfaces and the domain. *)

open System
open System.IO
open System.Linq
open System.Threading
open Microsoft.Extensions.Configuration

open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.CommandHandlers
open GlobalPollenProject.Core.Dependencies
open GlobalPollenProject.Shared.Identity.Models

open ReadStore
open EventStore
open AzureImageService

open Microsoft.EntityFrameworkCore

type PageRequest = { Page: int; PageSize: int }

// Definitions
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
    SubmitUnknownGrain: Guid -> string list -> int -> float -> float -> unit
}

type DigitiseService = {
    GetMyCollections:       GetCurrentUser              -> Result<ReferenceCollectionSummary list,ServiceError>
    GetCollectionDetail:    Guid                        -> Result<ReferenceCollection, ServiceError>
    StartNewCollection:     StartCollectionRequest      -> GetCurrentUser -> Result<CollectionId,ServiceError>
    AddSlideRecord:         SlideRecordRequest          -> Result<SlideId,ServiceError>
    AddSlideImage:          SlideImageRequest           -> Result<unit,ServiceError>
}

type UserService = {
    RegisterProfile: NewAppUserRequest -> GetCurrentUser -> Result<unit,ServiceError>
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

    let appService (deps:Dependencies) (eventStore: EventStore) (projections: EntityFramework.ReadContext) =
        let aggregate = {
            initial = State.Initial
            evolve = State.Evolve
            handle = handle
            getId = getId 
        }
        let handle = GlobalPollenProject.Core.CommandHandlers.create aggregate "ReferenceCollection" deps eventStore.ReadStream<Event> eventStore.Save

        let startNewCollection (request:StartCollectionRequest) getCurrentUser =
            let id = CollectionId (deps.GenerateId())
            handle (CreateCollection { Id = id; Name = request.Name; Owner = UserId (getCurrentUser()); Description = request.Description })
            Success id
        
        let addMeta request = 
            let identification = Botanical (TaxonId request.BackboneTaxonId)
            handle (AddSlide { Id = CollectionId request.Collection; Taxon = identification; Place = None; Time = None })
            Success (SlideId (CollectionId request.Collection,"SL001"))

        let addImage request = 
            let base64 = Base64Image request.ImageBase64
            let toUpload = Single base64
            let uploaded = deps.UploadImage toUpload
            let slideId = SlideId ((CollectionId request.CollectionId), request.SlideId)
            handle (UploadSlideImage { Id = slideId; Image = uploaded })
            Success()

        let myCollections getCurrentUser = 
            let userId = getCurrentUser()
            let readModel = projections.ReferenceCollectionSummary |> Seq.filter (fun rc -> rc.User = userId) |> Seq.toList
            Success readModel

        let getCollection id =
            let result = projections.ReferenceCollection.Include(fun x -> x.Slides) |> Seq.tryFind (fun rc -> rc.Id = id)
            match result with
            | Some rc -> Success rc
            | None -> Failure NotFound

        { StartNewCollection    = startNewCollection
          AddSlideRecord        = addMeta
          AddSlideImage         = addImage
          GetMyCollections      = myCollections
          GetCollectionDetail   = getCollection }


module Grain =

    open GlobalPollenProject.Core.Aggregates.Grain

    let appService (deps:Dependencies) (eventStore: EventStore) (projections: EntityFramework.ReadContext) =
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId 
        }
        let handle = create aggregate "Grain" deps eventStore.ReadStream<Event> eventStore.Save

        let submitUnknownGrain grainId (images:string list) age (lat:float) lon =
            let id = GrainId grainId
            let uploadedImages = images |> List.map (fun x -> SingleImage (Url.create x))
            let spatial = Latitude (lat * 1.0<DD>), Longitude (lon * 1.0<DD>)
            let temporal = CollectionDate (age * 1<CalYr>)
            let userId = UserId (Guid.NewGuid())
            handle (SubmitUnknownGrain {Id = id; Images = uploadedImages; SubmittedBy = userId; Temporal = Some temporal; Spatial = spatial })

        let identifyUnknownGrain grainId taxonId =
            handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId; IdentifiedBy = UserId (Guid.NewGuid()) })

        let listUnknownGrains() =
            projections.GrainSummary |> Seq.toList

        let listEvents() =
            eventStore.Events |> Seq.toList
        
        {SubmitUnknownGrain = submitUnknownGrain }


module Taxonomy =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let appService (deps:Dependencies) (eventStore: EventStore) (projections: EntityFramework.ReadContext) =
        let handle =
            let aggregate = {
                initial = State.InitialState
                evolve = State.Evolve
                handle = handle
                getId = getId
            }
            create aggregate "Taxon" deps eventStore.ReadStream<Event> eventStore.Save

        let list (request:PageRequest) =
            let result = projections.TaxonSummary.Skip(request.Page - 1 * request.PageSize).Take(request.PageSize) |> Seq.toList
            { Items = result; CurrentPage = request.Page; TotalPages = 2; ItemsPerPage = request.PageSize; ItemTotal = result.Length }

        let getByName family genus species =
            let taxon = projections.TaxonSummary
                        |> Seq.tryFind (fun taxon -> taxon.Family = family && taxon.Genus = genus && taxon.Species = species)
            match taxon with
            | Some t -> Success t
            | None -> Failure NotFound

        {List = list; GetByName = getByName }


module Backbone =

    open GlobalPollenProject.Core.Aggregates.Taxonomy
    open ImportTaxonomy

    let appService (deps:Dependencies) (eventStore: EventStore) (projections: EntityFramework.ReadContext) =

        let H = 
            let aggregate = {
                initial = State.InitialState
                evolve = State.Evolve
                handle = handle
                getId = getId }
            GlobalPollenProject.Core.CommandHandlers.create aggregate "Taxonomy" deps eventStore.ReadStream<Event> eventStore.Save

        let importAll filePath =

            let taxa = (readPlantListTextFile filePath) |> List.filter (fun x -> x.TaxonomicStatus = "accepted") |> List.take 5000
            let mutable commands : Command list = []
            for row in taxa do
                let additionalCommands = createImportCommands row commands deps.GenerateId
                additionalCommands |> List.map H |> ignore
                commands <- List.append commands additionalCommands
            ()
            //commands |> List.map H

        let search (request:BackboneSearchRequest) =

            let genus = if isNull request.Genus then "" else request.Genus
            let species = if isNull request.Species then "" else request.Species

            let result = projections.BackboneTaxon.Where(fun m ->
                m.Rank = request.Rank
                && m.LatinName.Contains request.LatinName
                && m.Family.Contains request.Family
                && m.Genus.Contains genus
                && m.Species.Contains species ) |> Seq.toList
            let totalPages = float result.Length / 10. |> Math.Ceiling |> int
            { CurrentPage = 1; ItemTotal = result.Length; TotalPages = totalPages; ItemsPerPage = 10; Items = if result.Length > 10 then result |> List.take 10 else result }

        { Search = search; Import = importAll }


module User = 

    open GlobalPollenProject.Core.Aggregates.User
    
    let appService (deps:Dependencies) (eventStore: EventStore) (projections: EntityFramework.ReadContext) =

        let h = 
            let aggregate = {
                initial = State.InitialState
                evolve = State.Evolve
                handle = handle
                getId = getId }
            GlobalPollenProject.Core.CommandHandlers.create aggregate "User" deps eventStore.ReadStream<Event> eventStore.Save
        
        let register (newUser:NewAppUserRequest) (getUserId:GetCurrentUser) : Result<unit,ServiceError> =
            let id = UserId (getUserId())
            h (Register { Id = id; Title = newUser.Title; FirstName = newUser.FirstName; LastName = newUser.LastName })
            Success()
        
        { RegisterProfile = register }


let composeApp () : AppServices =

    // Load AppSettings
    let appSettings = ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build()

    // Connect to Persistence Stores
    let eventStore = EventStore.SqlEventStore()
    let projections = new EntityFramework.ReadContext()
    let imageUploader = AzureImageService.uploadToAzure "Development" appSettings.["imagestore:azureconnectionstring"] (fun x -> Guid.NewGuid().ToString())

    // Projection Repositories
    let backboneRepo = EntityFramework.backboneRepo projections
    let grainRepo = EntityFramework.grainRepo projections

    // Setup Event Handlers (Projections)
    //TEMP
    let getById id =
        let unwrappedId (TaxonId i) : Guid = i
        backboneRepo.GetById (unwrappedId id)

    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.projectionHandler projections getById backboneRepo.GetTaxonByName
    |> ignore

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
                let t = backboneRepo.GetById (u id)
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
    { Backbone      = Backbone.appService dependencies eventStore projections
      Taxonomy      = Taxonomy.appService dependencies eventStore projections
      Grain         = Grain.appService dependencies eventStore projections
      Digitise      = Digitise.appService dependencies eventStore projections
      User          = User.appService dependencies eventStore projections }