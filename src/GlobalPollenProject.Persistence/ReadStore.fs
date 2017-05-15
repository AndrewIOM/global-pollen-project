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
        let config = ConfigurationOptions()
        config.SyncTimeout <- 100000
        config.ConnectTimeout <- 100000
        config.EndPoints.Add(ip)
        StackExchange.Redis.ConnectionMultiplexer.Connect(config)

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

    // BackboneTaxon:Id
    // BackboneTaxon:Family:Compositae
    // BackboneTaxon:Compositae:Fraxinus:excelsior

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

    // 1. Add by ID
    // 2. Add to name index
    // 3. Add to autocomplete index
    let import set setSortedList serialise (taxon:BackboneTaxon) = 
        RepositoryBase.setSingle (taxon.Id.ToString()) taxon set serialise |> ignore
        RepositoryBase.setKey taxon.Id (sprintf "BackboneTaxon:%s:%s" taxon.Rank taxon.LatinName) set serialise |> ignore
        RepositoryBase.setSortedListItem taxon.LatinName ("Autocomplete:BackboneTaxon:" + taxon.Rank) setSortedList 0. |> ignore
        Ok ()
