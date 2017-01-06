namespace GlobalPollenProject.App

open EventHandler
open System
open GlobalPollenProject.Core.Aggregates.Grain
open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.CommandHandlers
open System.Threading

open ReadStore

module GrainService =

    // Setup services
    let private eventHandler = EventHandler ()

    // Event handlers are subscribed such that, as events as handled
    // by the command handler, the event handlers are notified.
    // Thus, we only need to handle the command and save the changes for
    // side-effects to occur.
    let private store = 
        EventStore.SqlEventStore.create ()
        |> EventStore.SqlEventStore.subscribe eventHandler.Handle

    let readSide =
        new ReadContext()

    // It would be useful here to have a command router for individual handlers
    let private handle = Grain.create (EventStore.SqlEventStore.readStream store) (EventStore.SqlEventStore.appendToStream store)

    let submitUnknownGrain grainId base64string =

        let id = GrainId grainId
        let uploadedImage = Url base64string
        let image = SingleImage uploadedImage
        handle (SubmitUnknownGrain {Id = id; Images = [image] })

    let identifyUnknownGrain grainId taxonId =
        handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId })

    let listUnknownGrains() =
        readSide.GrainSummaries |> Seq.toList

    let listEvents() =
        store.context.Events |> Seq.toList