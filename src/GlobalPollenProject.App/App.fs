namespace GlobalPollenProject.App

open System
open System.IO
open Microsoft.Extensions.Configuration

open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.CommandHandlers
open GlobalPollenProject.Shared.Identity.Models
open System.Threading

open ReadStore
open EventStore

open AzureImageService
open GlobalPollenProject.Core.Dependencies

module Config =

    let appSettings = ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build()
    let eventStore = EventStore.SqlEventStore()
    let projections = new ReadContext()

    let isNull (x : BackboneTaxon) = match box x with null -> true | _ -> false
    let getTaxon (id:TaxonId) : BackboneTaxon option =
        let unwrap (TaxonId id) = id
        let result = projections.BackboneTaxa.Find (unwrap id)
        if not (isNull result) 
            then Some result 
            else None

    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.grainProjections 
    |> ignore

    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.taxonomyProjections getTaxon
    |> ignore

    let dependencies = 
        let generateId = Guid.NewGuid
        let log = ignore
        let uploadImage = AzureImageService.uploadToAzure "Development" appSettings.["imagestore:azureconnectionstring"] (fun x -> Guid.NewGuid().ToString())
        let gbifLink = ExternalLink.getGbifId
        let neotomaLink = ExternalLink.getNeotomaId

        let taxonomicBackbone (query:BackboneQuery) : TaxonId option =
            match query with
            | ValidateById id -> 
                let t = getTaxon id
                match t with
                | Some t -> Some (TaxonId t.Id)
                | None -> None
            | Validate identity ->
                None // TODO implement

        let calculateIdentity = calculateTaxonomicIdentity taxonomicBackbone
    
        { GenerateId        = generateId
          Log               = log
          UploadImage       = uploadImage
          GetGbifId         = gbifLink
          GetNeotomaId      = neotomaLink
          ValidateTaxon     = taxonomicBackbone
          CalculateIdentity = calculateIdentity }

module GrainAppService =

    open GlobalPollenProject.Core.Aggregates.Grain

    let aggregate = {
        initial = State.InitialState
        evolve = State.Evolve
        handle = handle
        getId = getId 
    }

    let handle = create aggregate "Grain" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

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
        Config.projections.GrainSummaries |> Seq.toList

    let listEvents() =
        Config.eventStore.Events |> Seq.toList


module UserAppService =

    open GlobalPollenProject.Core.Aggregates.User
    open GlobalPollenProject.Shared.Identity

    let handle =
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        create aggregate "User" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

    let register userId title firstName lastName =
        handle ( Register { Id = UserId userId; Title = title; FirstName = firstName; LastName = lastName })


module TaxonomyAppService =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let handle =
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        create aggregate "Taxon" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

    let list() =
        Config.projections.TaxonSummaries |> Seq.toList


module DigitiseAppService =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection

    let handle =
        let aggregate = {
            initial = State.Initial
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        GlobalPollenProject.Core.CommandHandlers.create aggregate "ReferenceCollection" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save


module BackboneAppService =

    open GlobalPollenProject.Core.Aggregates.Taxonomy
    open ImportTaxonomy

    let H = 
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId }
        GlobalPollenProject.Core.CommandHandlers.create aggregate "Taxonomy" Config.dependencies Config.eventStore.ReadStream<Event> Config.eventStore.Save

    let importAll filePath =

        let taxa = (readPlantListTextFile filePath) |> List.filter (fun x -> x.TaxonomicStatus = "accepted") |> List.take 2000
        let mutable commands : Command list = []
        for row in taxa do
            let additionalCommands = createImportCommands row commands Config.dependencies.GenerateId
            additionalCommands |> List.map H |> ignore
            commands <- List.append commands additionalCommands
        ()
        //commands |> List.map H

    let list () =

        Config.projections.BackboneTaxa |> Seq.toList