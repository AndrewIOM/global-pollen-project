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

type ServiceError =
| CoreError
| ValidationError
| PersistenceError
| NotFound

type GetCurrentUser = unit -> Guid

// Load AppSettings
let appSettings = ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build()

// Image Store
let saveImage = AzureImageStore.uploadToAzure "Development" appSettings.["imagestore:azureconnectionstring"] (fun x -> Guid.NewGuid().ToString())

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
    let router = ProjectionHandler.route readStoreGet readStoreGetList readStoreGetSortedList redisSet redisSetList redisSetSortedList
    let getEventCount = eventStore.Value.Checkpoint
    let result = (ProjectionHandler.readModelAgent router readStoreGet redisSet getEventCount).PostAndReply(fun rc -> e, rc)
    match result with 
    | Ok r -> ()
    | Error e -> invalidOp "Read model is corrupt"

eventStore.Value.SaveEvent 
:> IObservable<string*obj>
|> Observable.subscribe projectionHandler
|> ignore



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
    | Error str -> Error CoreError


module Digitise =

    open GlobalPollenProject.Core.Aggregates.ReferenceCollection
    open Converters

    let private issueCommand = 
        let aggregate = { initial = State.Initial; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "ReferenceCollection" aggregate domainDependencies

    let startNewCollection (request:StartCollectionRequest) getCurrentUser =
        let newId = CollectionId <| domainDependencies.GenerateId()
        let currentUser = UserId <| getCurrentUser()
        issueCommand <| CreateCollection { Id = newId; Name = request.Name; Owner = currentUser; Description = request.Description }
        Ok newId

    let addSlideRecord request = 
        request
        |> DtoToDomain.dtoToAddSlideCommand
        |> lift issueCommand
        |> toAppResult

    let uploadSlideImage request = 
        let base64 = Base64Image request.ImageBase64
        let toUpload = Single base64
        let uploaded = saveImage toUpload
        let slideId = SlideId ((CollectionId request.CollectionId), request.SlideId)
        issueCommand <| UploadSlideImage { Id = slideId; Image = uploaded }
        Ok

    let listCollections () = 
        ReadStore.RepositoryBase.getAll<ReferenceCollectionSummary> All readStoreGetList deserialise
    
    let private deserialiseGuid json =
        let unwrap (ReadStore.Json j) = j
        let s = (unwrap json).Replace("\"", "")
        match Guid.TryParse(s) with
        | true,g -> Ok g
        | false,g -> Error <| "Guid was not in correct format"

    let myCollections getCurrentUser = 
        let userId = getCurrentUser()
        let cols = ReadStore.RepositoryBase.getListKey<Guid> All ("CollectionAccessList:" + (userId.ToString())) readStoreGetList deserialiseGuid
        match cols with
        | Error e -> Error PersistenceError
        | Ok clist -> 
            let getCol id = ReadStore.RepositoryBase.getSingle<EditableRefCollection> (id.ToString()) readStoreGet deserialise
            clist 
            |> List.map getCol 
            |> List.choose (fun r -> match r with | Ok c -> Some c | Error e -> None)
            |> Ok

    let getCollection id =
        ReadStore.RepositoryBase.getSingle id readStoreGet deserialise<EditableRefCollection>


module UnknownGrains =

    open GlobalPollenProject.Core.Aggregates.Grain

    let private issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "Specimen" aggregate domainDependencies

    // let submitUnknownGrain grainId (images:string list) age (lat:float) lon =
    //     let id = GrainId grainId
    //     let uploadedImages = images |> List.map (fun x -> SingleImage (Url.create x))
    //     let spatial = Latitude (lat * 1.0<DD>), Longitude (lon * 1.0<DD>)
    //     let temporal = CollectionDate (age * 1<CalYr>)
    //     let userId = UserId (Guid.NewGuid())
    //     issueCommand <| SubmitUnknownGrain {Id = id; Images = uploadedImages; SubmittedBy = userId; Temporal = Some temporal; Spatial = spatial }
    //     Ok id

    let submitUnknownGrain (request:AddUnknownGrainRequest) getCurrentUser =

        let upload base64Strings = 
            base64Strings
            |> List.map (Base64Image >> Single >> saveImage)
            |> Ok

        let currentUser = Ok(UserId <| getCurrentUser())
        let newId = Ok(GrainId <| domainDependencies.GenerateId())

        request
        |> Converters.DtoToDomain.dtoToGrain newId currentUser
        <*> (upload request.StaticImagesBase64)
        |> Result.map issueCommand

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
        ReadStore.RepositoryBase.getAll<TaxonSummary> req readStoreGetList deserialise
        |> toAppResult

    let getByName family genus species =
        ReadStore.TaxonomicBackbone.tryFindByLatinName family genus species readStoreGetList readStoreGet deserialise

module Backbone =

    open GlobalPollenProject.Core.Aggregates.Taxonomy
    open ImportTaxonomy

    let private issueCommand = 
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
        |> DtoToDomain.backboneSearchToIdentity
        |> Result.bind (fun s -> ReadStore.TaxonomicBackbone.search s readLex deserialise)

    let tryMatch (request:BackboneSearchRequest) =
        request
        |> DtoToDomain.backboneSearchToIdentity
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
        >>= trace request.Authorship

module User = 

    open GlobalPollenProject.Core.Aggregates.User
    
    let private issueCommand = 
        let aggregate = { initial = State.InitialState; evolve = State.Evolve; handle = handle; getId = getId }
        eventStore.Value.MakeCommandHandler "User" aggregate domainDependencies
    
    let register (newUser:NewAppUserRequest) (getUserId:GetCurrentUser) =
        let id = UserId (getUserId())
        issueCommand <| Register { Id = id; Title = newUser.Title; FirstName = newUser.FirstName; LastName = newUser.LastName }
        Ok id
