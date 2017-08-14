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

open ReadModels
open ReadStore
open Converters
open Responses

type GetCurrentUser = unit -> Guid

// Load AppSettings
let appSettings = ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build()

// Image Store
let saveImage = AzureImageStore.uploadToAzure appSettings.["imagestore:baseurl"] appSettings.["imagestore:container"] appSettings.["imagestore:azureconnectionstring"] (fun x -> Guid.NewGuid().ToString())
let generateThumbnail = AzureImageStore.generateThumbnail appSettings.["imagestore:baseurl"] appSettings.["imagestore:containerthumbnail"] appSettings.["imagestore:azureconnectionstring"]
let toAbsoluteUrl = Url.relativeToAbsolute appSettings.["imagestore:baseurl"]

// Write (Event) Store
let eventStore = lazy(
    let ip = appSettings.["eventstore:eventstoreip"]
    let port = appSettings.["eventstore:eventstoreport"] |> int
    let username = appSettings.["eventstore:eventstoreuser"]
    let pass = appSettings.["eventstore:eventstorepassword"]
    let es = EventStore.connect ip port username pass |> Async.RunSynchronously
    EventStore.EventStore(es) )

// Read Model 'Repository'
let readStoreGet,readStoreGetList,readStoreGetSortedList,readLex,redisSet,redisSetList,redisSetSortedList =
    let ip = appSettings.["readstore:redisip"]
    let redis = lazy (ReadStore.Redis.connect ip)
    redis.Value |> ReadStore.Redis.get, 
    redis.Value |> ReadStore.Redis.getListItems, 
    redis.Value |> ReadStore.Redis.getSortedListItems, 
    redis.Value |> ReadStore.Redis.lexographicSearch,
    redis.Value |> ReadStore.Redis.set, 
    redis.Value |> ReadStore.Redis.addToList,
    redis.Value |> ReadStore.Redis.addToSortedList

let deserialise<'a> json = 
    let unwrap (ReadStore.Json j) = j
    Serialisation.deserialiseCli<'a> (unwrap json)

let serialise s = 
    let result = Serialisation.serialiseCli s
    match result with
    | Ok r -> Ok <| ReadStore.Json r
    | Error e -> Error e

let projectionHandler e =
    let router = ProjectionHandler.route readStoreGet readStoreGetList readStoreGetSortedList redisSet redisSetList redisSetSortedList generateThumbnail toAbsoluteUrl
    let getEventCount = eventStore.Value.Checkpoint
    let result = (ProjectionHandler.readModelAgent router readStoreGet redisSet getEventCount).PostAndReply(fun rc -> e, rc)
    match result with 
    | Ok r -> ()
    | Error e -> invalidOp "Read model is corrupt"

eventStore.Value.SaveEvent 
:> IObservable<string*obj>
|> Observable.subscribe projectionHandler
|> ignore

let private deserialiseGuid json =
    let unwrap (ReadStore.Json j) = j
    let s = (unwrap json).Replace("\"", "")
    match Guid.TryParse(s) with
    | true,g -> Ok g
    | false,g -> Error <| "Guid was not in correct format"


// App Core Dependencies
let domainDependencies = 
    let log = ignore
    let calculateIdentity = calculateTaxonomicIdentity ReadStore.TaxonomicBackbone.findMatches
    let isValidTaxon query =
        match ReadStore.TaxonomicBackbone.validate query readStoreGet readStoreGetList deserialise with
        | Ok t -> Some (TaxonId t.Id)
        | Error e -> None

    { GenerateId          = Guid.NewGuid
      Log                 = log
      GetGbifId           = ExternalLink.getGbifId
      GetNeotomaId        = ExternalLink.getNeotomaId
      GetTime             = (fun x -> DateTime.Now)
      ValidateTaxon       = isValidTaxon
      CalculateIdentity   = calculateIdentity }


let toAppResult domainResult =
    match domainResult with
    | Ok r -> Ok r
    | Error str -> Error Core

let toPersistenceError domainResult =
    match domainResult with
    | Ok r -> Ok r
    | Error str -> Error ServiceError.Persistence

let toValidationError domainResult =
    match domainResult with
    | Ok r -> Ok r
    | Error str -> Error <| ServiceError.Validation [{ Property = ""; Errors = [str]}]


module Digitise =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection
    open Converters
   
    let myCollections getCurrentUser = 
        let userId = getCurrentUser()
        let cols = ReadStore.RepositoryBase.getListKey<Guid> All ("CollectionAccessList:" + (userId.ToString())) readStoreGetList deserialiseGuid
        match cols with
        | Error e -> Error Persistence
        | Ok clist -> 
            let getCol id = ReadStore.RepositoryBase.getSingle<EditableRefCollection> (id.ToString()) readStoreGet deserialise
            clist 
            |> List.map getCol 
            |> List.choose (fun r -> match r with | Ok c -> Some c | Error e -> None)
            |> Ok

    let getCollection id =
        ReadStore.RepositoryBase.getSingle id readStoreGet deserialise<EditableRefCollection>
        |> toAppResult

    let private issueCommand = 
        let aggregate = { initial = State.Initial; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "ReferenceCollection" aggregate domainDependencies

    let startNewCollection getCurrentUser (request:StartCollectionRequest) =
        let newId = CollectionId <| domainDependencies.GenerateId()
        let currentUser = UserId <| getCurrentUser()
        issueCommand <| CreateCollection { Id = newId; Name = request.Name; Owner = currentUser; Description = request.Description }
        Ok newId

    let publish getCurrentUser colId =
        let currentUser = UserId <| getCurrentUser()
        let id = CollectionId colId
        Publish id
        |> issueCommand

    let addSlideRecord request = 
        request
        |> Dto.toAddSlideCommand readStoreGet
        |> lift issueCommand
        |> toAppResult

    let uploadSlideImage (request:SlideImageRequest) = 
        request
        |> Converters.Dto.toAddSlideImageCommand readStoreGet saveImage
        |> lift issueCommand
        |> toAppResult


module Calibrations =

    open GlobalPollenProject.Core.Aggregates.Calibration
    open Converters
    let private issueCommand = 
        let aggregate = { initial = State.Initial; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "Calibration" aggregate domainDependencies

    let private deserialiseGuid json =
        let unwrap (ReadStore.Json j) = j
        let s = (unwrap json).Replace("\"", "")
        match Guid.TryParse(s) with
        | true,g -> Ok g
        | false,g -> Error <| "Guid was not in correct format"

    let getMyCalibrations getCurrentUser =
        let userId = getCurrentUser()
        let cols = ReadStore.RepositoryBase.getListKey<Guid> All ("Calibration:User:" + (userId.ToString())) readStoreGetList deserialiseGuid
        match cols with
        | Error e -> Error Persistence
        | Ok clist -> 
            let getCol id = ReadStore.RepositoryBase.getSingle<ReadModels.Calibration> (id.ToString()) readStoreGet deserialise
            clist 
            |> List.map getCol 
            |> List.choose (fun r -> match r with | Ok c -> Some c | Error e -> None)
            |> Ok

    let setupMicroscope getCurrentUser (req:AddMicroscopeRequest) =
        let microscope = Microscope.Light <| LightMicroscope.Compound (10, [ 10; 20; 40; 100 ], "Nikon")
        let cmd = UseMicroscope { Id = CalibrationId <| domainDependencies.GenerateId()
                                  User = getCurrentUser() |> UserId
                                  FriendlyName = req.Name
                                  Microscope = microscope }
        issueCommand cmd
        |> Ok

    let calibrateMagnification (req:CalibrateRequest) =
        let getUrl img =
            match img with
            | SingleImage (u,cal) -> Ok u
            | FocusImage _ -> Error "Cannot use focus images"
        let floatingCalibration = {
            Point1 = req.X1,req.Y1
            Point2 = req.X2,req.Y2
            MeasuredDistance = req.MeasuredLength * 1.<um>
        }
        let id = req.CalibrationId |> CalibrationId
        let generateCommand url =
            Calibrate (id,400<timesMagnified>, { Image = url ; 
                                                 StartPoint = floatingCalibration.Point1; 
                                                 EndPoint = floatingCalibration.Point2; 
                                                 MeasureLength = floatingCalibration.MeasuredDistance })

        ImageForUpload.Single ((Base64Image req.ImageBase64),floatingCalibration)
        |> saveImage
        |> bind getUrl
        |> lift generateCommand
        |> lift issueCommand
        |> Ok


module UnknownGrains =

    open GlobalPollenProject.Core.Aggregates.Grain

    let private issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "Specimen" aggregate domainDependencies

    let submitUnknownGrain getCurrentUser (request:AddUnknownGrainRequest) =
        let id = domainDependencies.GenerateId() |> GrainId
        let user = getCurrentUser() |> UserId
        request
        |> Dto.toSubmitUnknownGrain id user saveImage
        |> lift issueCommand
        |> toAppResult

    let getDetail grainId =
        ReadStore.RepositoryBase.getSingle<GrainSummary> grainId readStoreGet deserialise

    let identifyUnknownGrain grainId taxonId =
        invalidOp "Not Implemented"
        handle (IdentifyUnknownGrain { Id = GrainId grainId; Taxon = TaxonId taxonId; IdentifiedBy = UserId (Guid.NewGuid()) })

    let listUnknownGrains =
        ReadStore.RepositoryBase.getAll<GrainSummary> All readStoreGetList deserialise

module Taxonomy =

    open GlobalPollenProject.Core.Aggregates.Taxonomy

    let list (request:PageRequest) =
        let req = Paged {ItemsPerPage = request.PageSize; Page = request.Page }
        let key = "TaxonSummary:Genus"
        let unwrapJson (Json x) : Result<string,string> = x |> Ok
        let namesOnPage = RepositoryBase.getListKey<string> req key readStoreGetSortedList unwrapJson
        match namesOnPage with
        | Error e -> Error e
        | Ok names ->

            let getSummary name =
                RepositoryBase.getKey<Guid> ("Taxon:" + name) readStoreGet deserialise
                |> bind (fun x -> RepositoryBase.getKey<TaxonSummary> ("TaxonSummary:" + (x.ToString())) readStoreGet deserialise)

            names
            |> List.map getSummary
            |> List.choose ( fun x -> match x with | Ok r -> Some r | Error e -> None )
            |> Ok
        |> toAppResult

    let private toNameSearchKey family genus species =
        match genus with
        | None -> "Taxon:" + family
        | Some g ->
            match species with
            | None -> sprintf "Taxon:%s:%s" family g
            | Some s -> 
                sprintf "Taxon:%s:%s:%s %s" family g g s

    let getByName family genus species =
        let key = toNameSearchKey family genus species
        let taxonId = RepositoryBase.getKey<Guid> key readStoreGet deserialise
        match taxonId with
        | Ok i -> RepositoryBase.getKey<TaxonDetail> ("TaxonDetail:" + (i.ToString())) readStoreGet deserialise
        | Error e -> Error e
        |> toAppResult

    let getSlide colId slideId =
        let key = sprintf "SlideDetail:%s:%s" colId slideId
        RepositoryBase.getKey<SlideDetail> key readStoreGet deserialise
        |> toAppResult

module IndividualReference =

    let list (request:PageRequest) =
        let cols = RepositoryBase.getListKey<Guid> All "ReferenceCollectionSummary:index" readStoreGetList deserialiseGuid
        match cols with
        | Error e -> Error Persistence
        | Ok clist -> 
            let getCol id = ReadStore.RepositoryBase.getSingle<ReferenceCollectionSummary> (id.ToString()) readStoreGet deserialise
            clist 
            |> List.map getCol 
            |> List.choose (fun r -> match r with | Ok c -> Some c | Error e -> None)
            |> Ok

    let getDetail id version =
        let key = sprintf "ReferenceCollectionDetail:%s:V%i" id version
        RepositoryBase.getKey<ReferenceCollectionDetail> key readStoreGet deserialise
        |> toAppResult


module Backbone =

    open GlobalPollenProject.Core.Aggregates.Taxonomy
    open ImportTaxonomy

    let issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "Taxon" aggregate domainDependencies

    let importAll filePath =

        let processTaxa' initialCommands allTaxa taxaToProcess =
            let mutable generatedCommands : Command list = initialCommands
            let mutable results : (ParsedTaxon * Result<Command list,ImportError>) list = []
            for taxon in taxaToProcess do
                let added = createImportCommands taxon allTaxa generatedCommands domainDependencies.GenerateId
                match added with 
                | Ok cmds -> 
                    cmds |> List.map issueCommand |> ignore
                    generatedCommands <- List.append generatedCommands cmds
                | Error e ->
                    match e with
                    | Postpone -> printfn "Postponing %s" taxon.ScientificName
                    | SynonymOfSubspecies -> printfn "Synonym of Subspecies (Skipping): %s" taxon.ScientificName 
                results <- List.append results [taxon,added]
                if generatedCommands.Length % 20000 = 0 then (printfn "Commands %i" generatedCommands.Length) else ignore()
            results

        let rec processTaxa commands allTaxa taxaToProcess =
            let results = processTaxa' commands allTaxa taxaToProcess
            let toReprocess = 
                results 
                |> List.filter(fun result -> match (snd result) with | Ok r -> false | Error e -> match e with | ImportError.Postpone -> true | ImportError.SynonymOfSubspecies -> false ) 
                |> List.map fst
            let currentCommands = 
                results 
                |> List.choose(fun result -> match (snd result) with | Ok r -> Some r | Error e -> None ) 
                |> List.concat
                |> List.append commands
            printfn "Generated commands for %i taxa, with %i remaining" currentCommands.Length toReprocess.Length
            match toReprocess.Length with
            | 0 -> currentCommands
            | _ -> processTaxa currentCommands allTaxa toReprocess

        let taxa = readPlantListTextFile filePath
        let commands : Command list = processTaxa [] taxa taxa
        //printfn "Issuing %i import commands..." commands.Length
        //commands |> List.map issueCommand |> ignore
        ()

    let searchNames (request:BackboneSearchRequest) =
        request
        |> Converters.Taxonomy.backboneSearchToIdentity
        |> Result.bind (fun s -> ReadStore.TaxonomicBackbone.search s readLex deserialise)
        |> toAppResult

    let tryMatch (request:BackboneSearchRequest) =
        request
        |> Converters.Taxonomy.backboneSearchToIdentity
        |> Result.bind (fun s -> ReadStore.TaxonomicBackbone.findMatches s readStoreGetSortedList readStoreGet deserialise)

    // Traces a backbone taxon to its most recent name (e.g. synonym -> synonym -> accepted name)
    let tryTrace (request:BackboneSearchRequest) =

        // TODO Remove this active pattern. Backbone was serialised incorrectly.
        let (|Prefix|_|) (p:string) (s:string) =
            if s.StartsWith(p) then
                Some(s.Substring(p.Length))
            else
                None

        let rec lookupSynonym (id:string) =
            let guid =
                match id with
                | Prefix "TaxonId " rest -> Guid.TryParse rest
                | _ -> Guid.TryParse id
            match fst guid with
            | false -> Error "Invalid taxon specified"
            | true -> 
                ReadStore.TaxonomicBackbone.getById (TaxonId (snd guid)) readStoreGet deserialise
                |> Result.bind (fun syn ->
                    match syn.TaxonomicStatus with
                    | "accepted" -> Ok [syn]
                    | "synonym"
                    | "misapplied" -> lookupSynonym syn.TaxonomicAlias
                    | "doubtful" -> Ok [syn]
                    | _ -> Error "Could not determine taxonomic status" )

        let trace (auth:string) (matches:BackboneTaxon list) =
            match matches.Length with
            | 0 -> Error "Unknown taxon specified"
            | 1 ->
                match matches.[0].TaxonomicStatus with
                | "doubtful"
                | "accepted" -> Ok ([matches.[0]])
                | "synonym"
                | "misapplied" ->  lookupSynonym matches.[0].TaxonomicAlias
                | _ -> Error "Could not determine taxonomic status"
            | _ ->
                match String.IsNullOrEmpty auth with
                | true -> matches |> Ok
                | false ->
                    // Search by author (NB currently not fuzzy)
                    let m = matches |> List.tryFind(fun t -> t.NamedBy = auth)
                    match m with
                    | None -> matches |> Ok
                    | Some t ->
                        match t.TaxonomicStatus with
                        | "doubtful"
                        | "accepted" -> Ok ([t])
                        | "synonym"
                        | "misapplied" ->  lookupSynonym t.TaxonomicAlias
                        | _ -> Error "Could not determine taxonomic status"

        request
        |> tryMatch
        |> Result.bind (trace request.Authorship)
        |> toAppResult

module User = 

    open GlobalPollenProject.Core.Aggregates.User
    
    let private issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "User" aggregate domainDependencies
    
    let register (newUser:NewAppUserRequest) (getUserId:GetCurrentUser) =
        let id = UserId (getUserId())
        issueCommand <| Register { Id = id; Title = newUser.Title; FirstName = newUser.FirstName; LastName = newUser.LastName }
        Ok id

// Additional event handlers:

eventStore.Value.SaveEvent 
:> IObservable<string*obj>
|> Observable.subscribe (EventHandlers.ExternalConnections.refresh readStoreGet Backbone.issueCommand)
|> ignore