namespace GlobalPollenProject.App

open System
open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.CommandHandlers
open System.Threading

open ReadStore
open EventStore

module Config =

    let eventStore = EventStore.SqlEventStore()
    let eventStream = eventStore.SaveEvent :> IObservable<string*obj>
    let readModelHandler = eventStream |> EventHandlers.projectionEventHandler
    let projections = new ReadContext()


module GrainAppService =

    open GlobalPollenProject.Core.Aggregates.Grain

    let aggregate = {
        initial = State.InitialState
        evolve = State.Evolve
        handle = handle
        getId = getId 
    }

    let private handle = create aggregate "Grain" Config.eventStore.ReadStream<GlobalPollenProject.Core.Aggregates.Grain.Event> Config.eventStore.Save

    let submitUnknownGrain grainId base64string =

        let id = GrainId grainId
        let uploadedImage = Url base64string
        let image = SingleImage uploadedImage
        handle (SubmitUnknownGrain {Id = id; Images = [image]; SubmittedBy = UserId (Guid.NewGuid()) })

    let identifyUnknownGrain grainId taxonId =
        handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId; IdentifiedBy = UserId (Guid.NewGuid()) })

    let listUnknownGrains() =
        Config.projections.GrainSummaries |> Seq.toList

    let listEvents() =
        Config.eventStore.Events |> Seq.toList


module UserAppService =

    open GlobalPollenProject.Core.Aggregates.User
    open GlobalPollenProject.Shared.Identity

    let private handle =
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        create aggregate "User" Config.eventStore.ReadStream<Event> Config.eventStore.Save

    let register userId title firstName lastName =
        handle ( Register { Id = UserId userId; Title = title; FirstName = firstName; LastName = lastName })


module TaxonomyAppService =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let private handle =
        let aggregate = {
            initial = State.InitialState
            evolve = State.Evolve
            handle = handle
            getId = getId
        }
        create aggregate "Taxon" Config.eventStore.ReadStream<Event> Config.eventStore.Save

    let import name =
        let domainName = LatinName name
        let id = Guid.NewGuid()
        handle ( Import { Id = TaxonId id; Name = domainName; Rank = Family; Parent = None })