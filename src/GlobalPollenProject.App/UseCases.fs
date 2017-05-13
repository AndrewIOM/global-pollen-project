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

open ReadModels
open Converters

type ServiceError =
| CoreError
| ValidationError
| PersistenceError
| NotFound

type GetCurrentUser = unit -> Guid

// Load AppSettings
let appSettings = ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build()

// Image Store
let imageUploader = AzureImageStore.uploadToAzure "Development" appSettings.["imagestore:azureconnectionstring"] (fun x -> Guid.NewGuid().ToString())

// Write (Event) Store
let eventStore = 
    let ip = appSettings.["eventstore:eventstoreip"]
    let port = appSettings.["eventstore:eventstoreport"] |> int
    let username = appSettings.["eventstore:eventstoreuser"]
    let pass = appSettings.["eventstore:eventstorepassword"]
    let es = EventStore.connect ip port username pass |> Async.RunSynchronously
    EventStore.EventStore(es)

// Read Model 'Repository'
let redisGet,redisSet,redisSetSortedList = 
    let ip = appSettings.["readstore:redisip"]
    let redis = ReadStore.Redis.connect ip
    redis |> ReadStore.Redis.get, redis |> ReadStore.Redis.set, redis |> ReadStore.Redis.addToSortedList

let deserialise<'a> json = 
    let unwrap (ReadStore.Json j) = j
    Serialisation.deserialiseCli<'a> (unwrap json)

let serialise s = 
    let result = Serialisation.serialiseCli s
    match result with
    | Ok r -> Ok <| ReadStore.Json r
    | Error e -> Error e

eventStore.SaveEvent 
:> IObservable<string*obj>
|> Observable.subscribe (ProjectionHandler.router redisGet redisSet redisSetSortedList)
|> ignore

// App Core Dependencies
let domainDependencies = 
    let log = ignore
    let calculateIdentity = calculateTaxonomicIdentity ReadStore.TaxonomicBackbone.search
    let isValidTaxon query =
        match ReadStore.TaxonomicBackbone.validate query redisGet deserialise with
        | Ok t -> Some (TaxonId t.Id)
        | Error e -> None

    { GenerateId          = Guid.NewGuid
      Log                 = log
      UploadImage         = imageUploader
      GetGbifId           = ExternalLink.getGbifId
      GetNeotomaId        = ExternalLink.getNeotomaId
      ValidateTaxon       = isValidTaxon
      CalculateIdentity   = calculateIdentity }


// Digitisation Use Cases
module Digitise =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection

    let private issueCommand = 
        let aggregate = { initial = State.Initial; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.MakeCommandHandler "ReferenceCollection" aggregate domainDependencies

    let startNewCollection (request:StartCollectionRequest) getCurrentUser =
        let newId = CollectionId <| domainDependencies.GenerateId()
        let currentUser = UserId <| getCurrentUser()
        issueCommand <| CreateCollection { Id = newId; Name = request.Name; Owner = currentUser; Description = request.Description }
        Ok newId

    let addSlideRecord request = 
        let identification = Botanical (TaxonId request.BackboneTaxonId)
        issueCommand <| AddSlide { Id = CollectionId request.Collection; Taxon = identification; Place = None; Time = None }
        Ok (SlideId (CollectionId request.Collection,"SL001"))

    let uploadSlideImage request = 
        let base64 = Base64Image request.ImageBase64
        let toUpload = Single base64
        let uploaded = domainDependencies.UploadImage toUpload
        let slideId = SlideId ((CollectionId request.CollectionId), request.SlideId)
        issueCommand <| UploadSlideImage { Id = slideId; Image = uploaded }
        Ok

    let listCollections () = 
        ReadStore.RepositoryBase.getAll<ReferenceCollectionSummary> redisGet deserialise
    
    let myCollections getCurrentUser = 
        ReadStore.RepositoryBase.getAll<ReferenceCollectionSummary> redisGet deserialise
        // let userId = getCurrentUser()
        // let readModel = projections.ReferenceCollectionSummary |> Seq.filter (fun rc -> rc.User = userId) |> Seq.toList
        // Success readModel

    let getCollection id =
        ReadStore.RepositoryBase.getSingle id redisGet deserialise<ReferenceCollectionSummary>


module UnknownGrains =

    open GlobalPollenProject.Core.Aggregates.Grain

    let private issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.MakeCommandHandler "Specimen" aggregate domainDependencies

    let submitUnknownGrain grainId (images:string list) age (lat:float) lon =
        let id = GrainId grainId
        let uploadedImages = images |> List.map (fun x -> SingleImage (Url.create x))
        let spatial = Latitude (lat * 1.0<DD>), Longitude (lon * 1.0<DD>)
        let temporal = CollectionDate (age * 1<CalYr>)
        let userId = UserId (Guid.NewGuid())
        issueCommand <| SubmitUnknownGrain {Id = id; Images = uploadedImages; SubmittedBy = userId; Temporal = Some temporal; Spatial = spatial }
        Ok id

    let getDetail grainId =
        ReadStore.RepositoryBase.getSingle<GrainSummary> grainId redisGet deserialise

    let identifyUnknownGrain grainId taxonId =
        invalidOp "Not Implemented"
        handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId; IdentifiedBy = UserId (Guid.NewGuid()) })

    let listUnknownGrains =
        ReadStore.RepositoryBase.getAll<GrainSummary> redisGet deserialise

module Taxonomy =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let list (request:PageRequest) =
        ReadStore.RepositoryBase.getAll<TaxonSummary> redisGet deserialise

    let getByName family genus species =
        ReadStore.TaxonomicBackbone.tryFindByLatinName family genus species redisGet deserialise

module Backbone =

    open GlobalPollenProject.Core.Aggregates.Taxonomy
    open ImportTaxonomy

    let private issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.MakeCommandHandler "Taxon" aggregate domainDependencies

    let importAll filePath =
        let taxa = (readPlantListTextFile filePath) |> List.filter (fun x -> x.TaxonomicStatus = "accepted") |> List.take 5000
        let mutable commands : Command list = []
        for row in taxa do
            let additionalCommands = createImportCommands row commands domainDependencies.GenerateId
            commands <- List.append commands additionalCommands
        commands |> List.map issueCommand
        ()

    let search (request:BackboneSearchRequest) =
        request
        |> DtoToDomain.backboneSearchToIdentity
        |> Result.bind (fun s -> ReadStore.TaxonomicBackbone.search s redisGet deserialise)

module User = 

    open GlobalPollenProject.Core.Aggregates.User
    
    let private issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.MakeCommandHandler "User" aggregate domainDependencies
    
    let register (newUser:NewAppUserRequest) (getUserId:GetCurrentUser) =
        let id = UserId (getUserId())
        issueCommand <| Register { Id = id; Title = newUser.Title; FirstName = newUser.FirstName; LastName = newUser.LastName }
        Ok id
