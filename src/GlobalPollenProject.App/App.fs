namespace GlobalPollenProject.App

open System
open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.CommandHandlers
open GlobalPollenProject.Shared.Identity.Models
open System.Threading

open ReadStore
open EventStore

open AzureImageService
open GlobalPollenProject.Core.Dependencies

module Config =

    let eventStore = EventStore.SqlEventStore()
    eventStore.SaveEvent 
    :> IObservable<string*obj>
    |> EventHandlers.projectionEventHandler 
    |> ignore

    let projections = new ReadContext()

    let dependencies = 
        let generateId = Guid.NewGuid
        let log = ignore
        let uploadImage = AzureImageService.uploadToAzure "Development" "connectionString" (fun x -> Guid.NewGuid().ToString())

        let taxonomicBackbbone = "doesn't exist yet"
        let calculateIdentity = calculateTaxonomicIdentity taxonomicBackbbone
    
        { GenerateId = generateId
          Log = log
          UploadImage = uploadImage
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
        let uploadedImages = images |> List.map (fun x -> SingleImage (Url x))
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

    let import name =
        let domainName = LatinName name
        let id = Guid.NewGuid()
        handle ( Import { Id = TaxonId id; Name = domainName; Rank = Family; Parent = None })

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

    