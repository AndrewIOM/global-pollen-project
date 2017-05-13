module ReadStore

open System
open ReadModels
open GlobalPollenProject.Core.DomainTypes

type Json = Json of string
type Serialise = obj -> Result<Json,string>
type Deserialise<'a> = Json -> Result<'a,string>

type GetFromKeyValueStore = string -> Result<Json,string>
type SetStoreValue = string -> Json -> Result<unit,string>
type SetEntryInList = string -> string -> Result<unit,string>
type SetEntryInSortedList = string -> string ->float -> Result<unit,string>

type ListRequest =
| All 
| Paged of PagedRequest

and PagedRequest = {
    ItemsPerPage: int
    Page: int
}

module KeyValueStore =

    let getKey<'a> key (getFromStore:GetFromKeyValueStore) (deserialise:Deserialise<'a>) =
        getFromStore key
        |> Result.bind deserialise

    let setKey key item (setStoreValue:SetStoreValue) (serialise:Serialise) =
        serialise item
        |> Result.bind (setStoreValue key)

    let setItemInList listKey item (set:SetEntryInList) =
        set listKey item 

    let setItemInSortedList listKey item (set:SetEntryInSortedList) =
        set listKey item 


module RepositoryBase =

    let generateKey typeName (id:string) = 
        if not <| id.StartsWith(typeName + ":") 
        then typeName + ":" + id
        else id

    let generateIndexKey typeName = 
        generateKey typeName "index"

    let getTypeName<'a> =
        typeof<'a>.Name

    let getSingle<'a> id =
        generateKey getTypeName<'a> (id.ToString())
        |> KeyValueStore.getKey<'a>

    let getKey<'a> key =
        KeyValueStore.getKey<'a> key

    let getAll<'a> =
        getTypeName<'a>
        |> generateIndexKey
        |> KeyValueStore.getKey<'a>

    let setKey item key =
        KeyValueStore.setKey key item

    let setSingle id item =
        generateKey (item.GetType().Name) id
        |> setKey item

    let setListItem item list =
        KeyValueStore.setItemInList list item

    let setSortedListItem item list score =
        KeyValueStore.setItemInSortedList list item score


module Redis =

    open StackExchange.Redis

    let inline private (~~) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit: ^a -> ^b) x)

    let private (|NotNull|_|) value = 
        if obj.ReferenceEquals(value, null) then None 
        else Some()

    let connect (ip:string) =
        //TODO Validate IP
        StackExchange.Redis.ConnectionMultiplexer.Connect(ip)

    let get (redis:ConnectionMultiplexer) (key:string) =
        let db = redis.GetDatabase()
        let result : string = ~~db.StringGet(~~key)
        match result with
        | NotNull -> Ok <| Json result
        | _ -> Error "Could not get read model from Redis"

    let set (redis:ConnectionMultiplexer) (key:string) (thing:Json) =
        let db = redis.GetDatabase()
        let u (Json s) = s
        match db.StringSet(~~key, ~~(u thing)) with
        | true -> Ok ()
        | false -> Error <| "Could not save key" + key + " to redis"

    let addToIndex (redis:ConnectionMultiplexer) (key:string) (model:'a) =
        let db = redis.GetDatabase()
        let indexKey = RepositoryBase.generateIndexKey (model.GetType().Name)
        db.SetAdd(~~indexKey, ~~key)

    let addToSortedList (redis:ConnectionMultiplexer) (key:string) (model:string) (score:float) =
        let db = redis.GetDatabase()
        match db.SortedSetAdd(~~key, ~~model, score) with
        | true -> Ok ()
        | false -> Error <| "Could not update read model"


module TaxonomicBackbone =

    let private toLowerCase (s:string) = s.ToLower()

    let private toNameSearchKey (family,genus,species) =
        match genus with
        | None -> "BackboneTaxon:Family:" + family
        | Some g ->
            match species with
            | None -> "BackboneTaxon:Genus:" + family + ":" + g
            | Some s -> "BackboneTaxon:Species:" + family + ":" + g + ":" + s

    let private unwrapLatinName (LatinName n) = n
    let private taxonIdToGuid (TaxonId n) : Guid = n

    let private toRankSearchKey = function
        | Family ln -> "BackboneTaxon:Family:" + (unwrapLatinName ln)
        | Genus ln -> "BackboneTaxon:Genus:" + (unwrapLatinName ln)
        | Species (ln,eph,auth) -> "BackboneTaxon:Genus:" + (unwrapLatinName ln)

    let private deserialiseGuid json =
        let unwrap (Json j) = j
        let s = (unwrap json).Replace("\"", "")
        match Guid.TryParse(s) with
        | true,g -> Ok g
        | false,g -> Error <| "Guid was not in correct format"

    let list (request:ListRequest) =
        RepositoryBase.getAll<BackboneTaxon>

    let getById id =
        (taxonIdToGuid id).ToString()
        |> RepositoryBase.getSingle<BackboneTaxon>

    let tryFindByLatinName family genus species get deserialise =
        let tryFindId key = RepositoryBase.getKey<Guid> key get deserialiseGuid
        let tryFindReadModel id = RepositoryBase.getSingle<BackboneTaxon> (id.ToString()) get deserialise
        (family,genus,species)
        |> toNameSearchKey
        |> tryFindId
        |> Result.bind tryFindReadModel

    let search identity get deserialise : Result<BackboneTaxon,string> =
        let search key = RepositoryBase.getKey<Guid> key get deserialiseGuid
        let fetchById id = RepositoryBase.getSingle<BackboneTaxon> (id.ToString()) get deserialise
        identity
        |> toRankSearchKey
        |> search
        |> Result.bind fetchById

    let validate (query:BackboneQuery) get deserialise =
        match query with
        | Validate i -> search i get deserialise
        | ValidateById id -> getById id get deserialise

    // BackboneTaxon:Id
    // BackboneTaxon:Family:Compositae
    // BackboneTaxon:Compositae:Fraxinus:excelsior

    let import set setSortedList serialise (taxon:BackboneTaxon) = 
        // 1. Add by ID
        // 2. Add to name index
        // 3. Add to autocomplete index
        RepositoryBase.setSingle (taxon.Id.ToString()) taxon set serialise |> ignore
        RepositoryBase.setKey taxon.Id (sprintf "BackboneTaxon:%s:%s" taxon.Rank taxon.LatinName) set serialise |> ignore
        RepositoryBase.setSortedListItem taxon.LatinName ("Autocomplete:BackboneTaxon:" + taxon.Rank) setSortedList 0. |> ignore
        Ok ()

    // let private unwrap id =
    //     let unwrapId (TaxonId id) : Guid = id
    //     (unwrapId id).ToString()


    // ReadStore.RedisReadStore.saveKey (sprintf "BackboneTaxon:%s" (projection.Id.ToString())) projection redis |> ignore
    // ReadStore.RedisReadStore.saveKey (sprintf "BackboneTaxon:%s:%s" projection.Rank projection.LatinName) projection.Id redis |> ignore 
    
    // //Pre-compute autocomplete for the taxonomic backbone
    // ReadStore.RedisReadStore.sortedSetAdd ("Autocomplete:BackboneTaxon:" + projection.Rank + ":") projection.LatinName 0. redis

//     let loadIndex<'TProjection> (redis:ConnectionMultiplexer) =
//         let db = redis.GetDatabase()
//         let indexKey = indexFormat (typeof<'TProjection>.Name)
//         let allRefColKeys = db.SetMembers(~~indexKey) |> Array.map (fun x -> ~~x.ToString())
//         let model = db.StringGet(allRefColKeys)
//         model |> Array.choose (fun x -> deserialise<'TProjection> (x.ToString())) |> Array.toList


// module ReadStore

// open System
// open GlobalPollenProject.Core.DomainTypes
// open GlobalPollenProject.Core.Aggregate
// open ReadModels

// type ListRequest =
// | All 
// | Paged of PagedRequest

// and PagedRequest = {
//     ItemsPerPage: int
//     Page: int
// }

// type ProjectionRepository<'TProjection> = {
//     GetById: Guid -> 'TProjection option
//     GetMultiple: ListRequest -> 'TProjection list
//     Exists: Guid -> bool
//     Save: Guid -> 'TProjection -> unit
// }

// type BackboneRepository = {
//     GetById: Guid -> BackboneTaxon option
//     List: ListRequest -> BackboneTaxon list
//     GetTaxonByName: string -> string -> string -> BackboneTaxon option
// }

// type TaxonRepository = {
//     GetSummary: Guid -> TaxonSummary option
// }

// type ReadStoreAction<'a> = ReadStoreAction of (IReadStore -> 'a)

// // module ReadStoreAction = 

// //     let run api (ReadStoreAction action) = 
// //         let resultOfAction = action api
// //         resultOfAction

// //     let map f action = 
// //         let newAction api =
// //             let x = run api action 
// //             f x
// //         ReadStoreAction newAction

// //     let retn x = 
// //         let newAction api =
// //             x
// //         ReadStoreAction newAction

// //     let apply fAction xAction = 
// //         let newAction api =
// //             let f = run api fAction 
// //             let x = run api xAction 
// //             f x
// //         ReadStoreAction newAction

// //     let bind f xAction = 
// //         let newAction api =
// //             let x = run api xAction 
// //             run api (f x)
// //         ReadStoreAction newAction

// //     let execute (client:IReadStore) action = run client action

// // module ReadStoreResult = 

// //     let map f = ReadStoreAction.map (Result.map f)

// //     let bind f xActionResult = 
// //         let newAction api =
// //             let xResult = ReadStoreAction.run api xActionResult 
// //             let yAction = 
// //                 match xResult with
// //                 | Success x -> 
// //                     f x
// //                 | Failure err -> 
// //                     (Failure err) |> ReadStoreAction.retn
// //             ReadStoreAction.run api yAction  
// //         ReadStoreAction newAction


// module ReadStore =

//     let set key value (sourceSet: string -> 'a -> unit) = 
//         sourceSet key value

//     let setIndex index entry value (sourceSet: string -> 'a -> float -> unit) =
//         sourceSet index entry value

//     let get<'TProjection> (source:string->'TProjection) key =
//         source key

//     let getAll<'TProjection> s () =
        
        




// ///////////

// let generateKey readModelName (id:string) = 
//     if not <| id.StartsWith(readModelName + ":") 
//     then readModelName + ":" + id
//     else id

// module RedisReadStore =

//     open StackExchange.Redis

//     let inline private (~~) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit: ^a -> ^b) x)
//     let private serialise (event:'a) = Serialisation.serialiseCli event
//     let private deserialise<'TEvent> projection = Serialisation.deserialiseCli<'TEvent> projection
//     let private indexFormat modelType = modelType + ":index"

//     let saveKey (key:string) (model:'a) (redis:ConnectionMultiplexer) =
//         let db = redis.GetDatabase()
//         let json = serialise model
//         db.StringSet(~~key, ~~json) |> ignore
//         let indexKey = indexFormat (model.GetType().Name)
//         db.SetAdd(~~indexKey, ~~key) |> ignore

//     let save (id:Guid) (model:'a) (redis:ConnectionMultiplexer) = 
//         let key = generateKey (model.GetType().Name) (id.ToString())
//         saveKey key model redis

//     let load<'TProjection> (id:string) (redis:ConnectionMultiplexer) =
//         let key = generateKey (typeof<'TProjection>.Name) (id.ToString())
//         let db = redis.GetDatabase()
//         let serialised = db.StringGet(~~key)
//         deserialise<'TProjection> ~~serialised

//     let loadKey<'TProjection> (key:string) (redis:ConnectionMultiplexer) =
//         let db = redis.GetDatabase()
//         let serialised = db.StringGet(~~key)
//         deserialise<'TProjection> ~~serialised

//     let loadIndex<'TProjection> (redis:ConnectionMultiplexer) =
//         let db = redis.GetDatabase()
//         let indexKey = indexFormat (typeof<'TProjection>.Name)
//         let allRefColKeys = db.SetMembers(~~indexKey) |> Array.map (fun x -> ~~x.ToString())
//         let model = db.StringGet(allRefColKeys)
//         model |> Array.choose (fun x -> deserialise<'TProjection> (x.ToString())) |> Array.toList

//     let sortedSetAdd (key:string) model (score:float) (redis:ConnectionMultiplexer) =
//         let db = redis.GetDatabase()
//         let json = serialise model
//         db.SortedSetAdd(~~key, ~~json, score) |> ignore


// module BackboneTaxonomy =

//     let listAll () = RedisReadStore.loadIndex<BackboneTaxon>
//     let tryFindByName () = invalidOp "Not yet implemented"
//     let tryFindById = RedisReadStore.load<BackboneTaxon>


// module Digitisation =

//     let getCollectionIds (userId:Guid) =
//         let req (rs:IReadStore) = rs.Get<Guid list> userId
//         ReadStoreAction req

//     let getProductInfo (collectionId:Guid) =
//         let action (rs:IReadStore) = rs.Get<ReferenceCollectionSummary> collectionId
//         ReadStoreAction action


//     let listUserCollections = RedisReadStore.loadIndex<ReferenceCollectionSummary>


// module Statistics =
//     let getMostWanted () = invalidOp "Not yet implemented"


// module DataViews =

//     let listDigitisedCollections () = invalidOp "Not yet implemented"
//     let listTaxa () = invalidOp "Not yet implemented"
