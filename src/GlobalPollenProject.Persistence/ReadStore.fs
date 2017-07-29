module ReadStore

open System
open ReadModels
open GlobalPollenProject.Core.DomainTypes

type Json = Json of string
type Serialise = obj -> Result<Json,string>
type Deserialise<'a> = Json -> Result<'a,string>

type ListRequest =
| All 
| Paged of PagedRequest

and PagedRequest = {
    ItemsPerPage: int
    Page: int
}

type GetFromKeyValueStore = string -> Result<Json,string>
type GetListFromKeyValueStore = ListRequest -> string -> Result<Json list,string>
type GetLexographic = string -> string -> Result<string list,string>

type SetStoreValue = string -> Json -> Result<unit,string>
type SetEntryInList = string -> string -> Result<unit,string>
type SetEntryInSortedList = string -> string ->float -> Result<unit,string>


module KeyValueStore =

    let getKey<'a> key (getFromStore:GetFromKeyValueStore) (deserialise:Deserialise<'a>) =
        getFromStore key
        |> Result.bind deserialise

    let getList<'a> listReq key (getListFromStore:GetListFromKeyValueStore) (deserialise: Deserialise<'a>) =
        let deserialiseList jsonList =
            jsonList
            |> List.map deserialise
            |> List.choose (fun r ->
                match r with
                | Ok o -> Some o
                | Error e -> None )
            |> Ok
        getListFromStore listReq key
        |> Result.bind deserialiseList

    let getLexographic key searchTerm (get:GetLexographic) =
        get key searchTerm

    let setKey key item (setStoreValue:SetStoreValue) (serialise:Serialise) =
        serialise item
        |> Result.bind (setStoreValue key)

    let setItemInList listKey item (set:SetEntryInList) =
        set listKey item 

    let setItemInSortedList listKey item score (set:SetEntryInSortedList) =
        set listKey item score


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

    let getAll<'a> listReq =
        getTypeName<'a>
        |> generateIndexKey
        |> KeyValueStore.getList<'a> listReq

    let getListKey<'a> listReq key =
        KeyValueStore.getList<'a> listReq key

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

    let connect (host:string) =
        printfn "Connecting to redis..."
        let config = ConfigurationOptions()
        config.SyncTimeout <- 100000
        config.ConnectTimeout <- 100000
        //because of https://github.com/dotnet/corefx/issues/8768:
        let ip = System.Net.Dns.GetHostAddressesAsync(host) 
                 |> Async.AwaitTask 
                 |> Async.RunSynchronously
                 |> Array.map (fun x -> x.MapToIPv4().ToString()) |> Array.head
        config.EndPoints.Add("127.0.0.1")
        printfn "Connecting to redis ip: %s" ip
        StackExchange.Redis.ConnectionMultiplexer.Connect(config)

    let get (redis:ConnectionMultiplexer) (key:string) =
        let db = redis.GetDatabase()
        let result : string = ~~db.StringGet(~~key)
        match result with
        | NotNull -> Ok <| Json result
        | _ -> Error "Could not get read model from Redis"

    let delete (redis:ConnectionMultiplexer) (key:string) =
        let db = redis.GetDatabase()
        match db.KeyDelete(~~key) with
        | true -> Ok()
        | false -> "Could not remove the key from redis: " + key |> Error

    let getListItems (redis:ConnectionMultiplexer) (pageReq:ListRequest) (key:string) =
        let db = redis.GetDatabase()
        let result : string seq = db.SetMembers(~~key) |> Seq.map ( ~~ )
        Ok <| (result |> Seq.map Json |> Seq.toList)

    let getSortedListItems (redis:ConnectionMultiplexer) (listReq:ListRequest) (key:string) =
        let db = redis.GetDatabase()
        let result : string seq =
            match listReq with
            | All -> db.SortedSetRangeByRank(~~key) |> Seq.map ( ~~ )
            | Paged p -> 
                let start = (p.Page - 1) * p.ItemsPerPage
                db.SortedSetRangeByScore(~~key, float start, start + p.ItemsPerPage |> float) |> Seq.map (~~)
        Ok <| (result |> Seq.map Json |> Seq.toList)

    let lexographicSearch (redis:ConnectionMultiplexer) (key:string) (searchTerm:string) =
        let db = redis.GetDatabase()
        let result : string seq = db.SortedSetRangeByValue(~~key, ~~searchTerm, ~~(searchTerm + "\xff")) |> Seq.map ( ~~ )
        Ok <| (result |> Seq.toList)

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

    let addToList (redis:ConnectionMultiplexer) (key:string) (model:string) =
        let db = redis.GetDatabase()
        match db.SetAdd(~~key, ~~model) with
        | true -> Ok()
        | false -> Error <| "Could not update read model"

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
        | Species (ln,eph,auth) -> "BackboneTaxon:Species:" + (unwrapLatinName ln)

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

    let tryFindByLatinName family genus species getList getSingle deserialise =
        let tryFindId key = RepositoryBase.getListKey<Guid> All key getList deserialiseGuid
        let tryFindReadModel (id:Guid list) = 
            // TODO modify to handle multiple possible matches
            RepositoryBase.getSingle<BackboneTaxon> (id.Head.ToString()) getSingle deserialise
        (family,genus,species)
        |> toNameSearchKey
        |> tryFindId
        |> Result.bind tryFindReadModel

    // Search names to find possible matches, returning whole taxa
    let findMatches identity getList getSingle deserialise : Result<BackboneTaxon list,string> =
        let search key = RepositoryBase.getListKey All key getList deserialiseGuid
        let fetchAllById ids = 
            ids 
            |> List.map (fun id -> RepositoryBase.getSingle<BackboneTaxon> (id.ToString()) getSingle deserialise)
            |> List.choose (fun x -> match x with | Ok r -> Some r | Error e -> None)
            |> Ok
        identity
        |> toRankSearchKey
        |> search
        |> Result.bind fetchAllById

    // Search names to find possible matches, returning taxon names
    let search identity (getLexographical:GetLexographic) deserialise : Result<string list, string> =
        let rank,ln =
            match identity with
            | Family ln -> "Family", unwrapLatinName ln
            | Genus ln -> "Genus", unwrapLatinName ln
            | Species (ln,eph,auth) -> "Species", unwrapLatinName ln
        let key = "Autocomplete:BackboneTaxon:" + rank
        KeyValueStore.getLexographic key ln getLexographical

    let validate (query:BackboneQuery) get getList deserialise =
        match query with
        | ValidateById id -> getById id get deserialise
        | Validate i -> 
            let matches = findMatches i getList get deserialise
            Error "Not implemented"

    let import set setSortedList serialise (taxon:BackboneTaxon) = 
        RepositoryBase.setSingle (taxon.Id.ToString()) taxon set serialise |> ignore
        RepositoryBase.setSortedListItem (taxon.Id.ToString()) (sprintf "BackboneTaxon:%s:%s" taxon.Rank taxon.LatinName) 0. setSortedList |> ignore
        RepositoryBase.setSortedListItem taxon.LatinName ("Autocomplete:BackboneTaxon:" + taxon.Rank) 0. setSortedList |> ignore
        Ok ()
