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

type PageRequest = { Page: int; PageSize: int }
type PagedResult<'TProjection> = {
    Items: 'TProjection list
    CurrentPage: int
    TotalPages: int
    ItemsPerPage: int
}

// Definition: User Use Cases
type BackboneService = {
    Search: string -> PagedResult<BackboneTaxon>
    Import: string -> unit
}

type TaxonomyService = {
    List: PageRequest -> PagedResult<TaxonSummary>
}

type GrainService = {
    SubmitUnknownGrain: Guid -> string list -> int -> float -> float -> unit
}

type AppServices = {
    Backbone: BackboneService
    Grain: GrainService
    Taxonomy: TaxonomyService
}


// Responsible for setting up app service for grain use cases
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
            projections.GrainSummaries |> Seq.toList

        let listEvents() =
            eventStore.Events |> Seq.toList
        
        {SubmitUnknownGrain = submitUnknownGrain }


// module User =

//     open GlobalPollenProject.Core.Aggregates.User
//     open GlobalPollenProject.Shared.Identity

//     let handle =
//         let aggregate = {
//             initial = State.InitialState
//             evolve = State.Evolve
//             handle = handle
//             getId = getId
//         }
//         create aggregate "User" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

//     let register userId title firstName lastName =
//         handle ( Register { Id = UserId userId; Title = title; FirstName = firstName; LastName = lastName })


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
            let result = projections.TaxonSummaries.Skip(request.Page - 1 * request.PageSize).Take(request.PageSize) |> Seq.toList
            { Items = result; CurrentPage = request.Page; TotalPages = 2; ItemsPerPage = request.PageSize }

        {List = list}


// module Digitise =

//     open GlobalPollenProject.Core.Aggregates.ReferenceCollection

//     let handle =
//         let aggregate = {
//             initial = State.Initial
//             evolve = State.Evolve
//             handle = handle
//             getId = getId
//         }
//         GlobalPollenProject.Core.CommandHandlers.create aggregate "ReferenceCollection" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save


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

            let taxa = (readPlantListTextFile filePath) |> List.filter (fun x -> x.TaxonomicStatus = "accepted") |> List.take 2000
            let mutable commands : Command list = []
            for row in taxa do
                let additionalCommands = createImportCommands row commands deps.GenerateId
                additionalCommands |> List.map H |> ignore
                commands <- List.append commands additionalCommands
            ()
            //commands |> List.map H

        let search name =
            let result = projections.BackboneTaxa.Where(fun m -> m.LatinName.Contains(name)) |> Seq.toList
            { CurrentPage = 1; TotalPages = 1; ItemsPerPage = 9999; Items =  result}

        { Search = search; Import = importAll }


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
    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.grainProjections projections
    |> ignore

    //TEMP
    let getById id =
        let unwrappedId (TaxonId i) : Guid = i
        backboneRepo.GetById (unwrappedId id)

    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.taxonomyProjections projections getById backboneRepo.GetTaxonByName
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
      Grain         = Grain.appService dependencies eventStore projections }