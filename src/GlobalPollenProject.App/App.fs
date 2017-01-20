namespace GlobalPollenProject.App

open System
open GlobalPollenProject.Core.Types
open GlobalPollenProject.Core.CommandHandlers
open System.Threading

open ReadStore
open EventStore

// ~~~~~~~~~~~~~~~~~~~~~~
// Configure App
// ~~~~~~~~~~~~~~~~~~~~~~

// A. Set up all event handlers
// -----------------------------
// Event handlers are subscribed such that, as events as handled
// by the command handler, the event handlers are notified.
// Thus, we only need to handle the command and save the changes for
// side-effects to occur.

module Config =

    let eventStore = EventStore.SqlEventStore()
    let eventStream = eventStore.SaveEvent :> IObservable<string*obj>
    let readModelHandler = eventStream |> EventHandlers.projectionEventHandler
    

    // B. Set up projections database
    let projections =
        new ReadContext()


module GrainService =

    open GlobalPollenProject.Core.Aggregates.Grain

    let aggregate = {
        initial = GlobalPollenProject.Core.Aggregates.Grain.InitialState
        evolve = GlobalPollenProject.Core.Aggregates.Grain.State.Evolve
        handle = GlobalPollenProject.Core.Aggregates.Grain.handle
        getId = GlobalPollenProject.Core.Aggregates.Grain.getId }

    let private handle = create aggregate "Grain" Config.eventStore.ReadStream<GlobalPollenProject.Core.Aggregates.Grain.Event> Config.eventStore.Save

    let submitUnknownGrain grainId base64string =

        let id = GrainId grainId
        let uploadedImage = Url base64string
        let image = SingleImage uploadedImage
        handle (SubmitUnknownGrain {Id = id; Images = [image] })

    let identifyUnknownGrain grainId taxonId =
        handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId })

    let listUnknownGrains() =
        Config.projections.GrainSummaries |> Seq.toList

    let listEvents() =
        Config.eventStore.Events |> Seq.toList


// module TaxonomyService = 

//     open GlobalPollenProject.Core.Aggregates.Taxonomy
//     open GlobalPollenProject.Core.CommandHandlers

//     let private handle = 
//         let aggregate = {
//             initial = GlobalPollenProject.Core.Aggregates.Taxonomy.State.InitialState
//             evolve = GlobalPollenProject.Core.Aggregates.Taxonomy.State.Evolve
//             handle = GlobalPollenProject.Core.Aggregates.Taxonomy.handle
//             getId = GlobalPollenProject.Core.Aggregates.Taxonomy.getId }
//         create aggregate "Taxonomy" (EventStore.SqlEventStore.readStream Config.eventStore) (EventStore.SqlEventStore.appendToStream Config.eventStore)

//     let import latinName rank =
//         handle (Import {Id = TaxonId (Guid.NewGuid()); Name = LatinName latinName; Rank = Rank.Family; Parent = None})