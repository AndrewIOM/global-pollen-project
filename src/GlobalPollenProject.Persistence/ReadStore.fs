module ReadStore

open System
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Aggregate
open ReadModels

type ListRequest =
| All 
| Paged of PagedRequest

and PagedRequest = {
    ItemsPerPage: int
    Page: int
}

type ProjectionRepository<'TProjection> = {
    GetById: Guid -> 'TProjection option
    GetMultiple: ListRequest -> 'TProjection list
    Exists: Guid -> bool
    Save: Guid -> 'TProjection -> unit
}

type BackboneRepository = {
    GetById: Guid -> BackboneTaxon option
    List: ListRequest -> BackboneTaxon list
    GetTaxonByName: string -> string -> string -> BackboneTaxon option
}

type TaxonRepository = {
    GetSummary: Guid -> TaxonSummary option
}

type IReadStore =
    abstract member TryCast : string -> obj -> Result<'a,string list>
    abstract member Get<'a> : obj -> Result<'a,string list>
    abstract member Set : obj -> obj -> Result<unit,string list>
    abstract member Connect: unit -> unit
    inherit IDisposable

type ReadStoreAction<'a> = ReadStoreAction of (IReadStore -> 'a)

// module ReadStoreAction = 

//     let run api (ReadStoreAction action) = 
//         let resultOfAction = action api
//         resultOfAction

//     let map f action = 
//         let newAction api =
//             let x = run api action 
//             f x
//         ReadStoreAction newAction

//     let retn x = 
//         let newAction api =
//             x
//         ReadStoreAction newAction

//     let apply fAction xAction = 
//         let newAction api =
//             let f = run api fAction 
//             let x = run api xAction 
//             f x
//         ReadStoreAction newAction

//     let bind f xAction = 
//         let newAction api =
//             let x = run api xAction 
//             run api (f x)
//         ReadStoreAction newAction

//     let execute (client:IReadStore) action = run client action

// module ReadStoreResult = 

//     let map f = ReadStoreAction.map (Result.map f)

//     let bind f xActionResult = 
//         let newAction api =
//             let xResult = ReadStoreAction.run api xActionResult 
//             let yAction = 
//                 match xResult with
//                 | Success x -> 
//                     f x
//                 | Failure err -> 
//                     (Failure err) |> ReadStoreAction.retn
//             ReadStoreAction.run api yAction  
//         ReadStoreAction newAction


let generateKey readModelName (id:string) = 
    if not <| id.StartsWith(readModelName + ":") 
    then readModelName + ":" + id
    else id

module RedisReadStore =

    open StackExchange.Redis

    let inline private (~~) (x:^a) : ^b = ((^a or ^b) : (static member op_Implicit: ^a -> ^b) x)
    let private serialise (event:'a) = Serialisation.serialiseCli event
    let private deserialise<'TEvent> projection = Serialisation.deserialiseCli<'TEvent> projection
    let private indexFormat modelType = modelType + ":index"

    let save (id:Guid) (model:'a) (redis:ConnectionMultiplexer) = 
        let key = generateKey (model.GetType().Name) (id.ToString())
        let db = redis.GetDatabase()
        let json = serialise model
        db.StringSet(~~key, ~~json) |> ignore
        let indexKey = indexFormat (model.GetType().Name)
        db.SetAdd(~~indexKey, ~~key) |> ignore

    let load<'TProjection> (id:string) (redis:ConnectionMultiplexer) =
        let key = generateKey (typeof<'TProjection>.Name) (id.ToString())
        let db = redis.GetDatabase()
        let serialised = db.StringGet(~~key)
        deserialise<'TProjection> ~~serialised

    let loadIndex<'TProjection> (redis:ConnectionMultiplexer) =
        let db = redis.GetDatabase()
        let indexKey = indexFormat (typeof<'TProjection>.Name)
        let allRefColKeys = db.SetMembers(~~indexKey) |> Array.map (fun x -> ~~x.ToString())
        let model = db.StringGet(allRefColKeys)
        model |> Array.choose (fun x -> deserialise<'TProjection> (x.ToString())) |> Array.toList


module BackboneTaxonomy =

    let listAll () = RedisReadStore.loadIndex<BackboneTaxon>
    let tryFindByName () = invalidOp "Not yet implemented"
    let tryFindById = RedisReadStore.load<BackboneTaxon>


module Digitisation =

    let getCollectionIds (userId:Guid) =
        let req (rs:IReadStore) = rs.Get<Guid list> userId
        ReadStoreAction req

    let getProductInfo (collectionId:Guid) =
        let action (rs:IReadStore) = rs.Get<ReferenceCollectionSummary> collectionId
        ReadStoreAction action


    let listUserCollections = RedisReadStore.loadIndex<ReferenceCollectionSummary>


module Statistics =
    let getMostWanted () = invalidOp "Not yet implemented"


module DataViews =

    let listDigitisedCollections () = invalidOp "Not yet implemented"
    let listTaxa () = invalidOp "Not yet implemented"
