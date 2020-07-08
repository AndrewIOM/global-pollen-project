module ReadStore

open System
open ReadModels
open GlobalPollenProject.Core.DomainTypes
open GlobalPollenProject.Core.Composition

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

type ListResult<'a> =
| AllPages of 'a list
| SinglePage of PagedResult<'a>

and PagedResult<'a> = {
    ItemsPerPage: int
    CurrentPage: int
    Items: 'a list
    TotalPages: int
    TotalItems: int
}

type Key = string
type SortScore = float
type SearchTerm = string

type GetFromKeyValueStore = Key -> Result<Json,string>
type GetListFromKeyValueStore = ListRequest -> Key -> Result<Json list,string>
type GetSortedListFromKeyValueStore = ListRequest -> Key -> Result<ListResult<Json>,string>
type GetLexographic = Key -> SearchTerm -> ListRequest -> Result<ListResult<string>,string>

type SetStoreValue = string -> Json -> Result<unit,string>
type SetEntryInList = string -> string -> Result<unit,string>
type SetEntryInSortedList = string -> string ->float -> Result<unit,string>

module Seq =
    let skipSafe (num: int) (source: seq<'a>) : seq<'a> =
        seq {
            use e = source.GetEnumerator()
            let idx = ref 0
            let loop = ref true
            while !idx < num && !loop do
                if not(e.MoveNext()) then
                    loop := false
                idx := !idx + 1

            while e.MoveNext() do
                yield e.Current 
        }

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

    let getSortedList<'a> listReq key (get:GetSortedListFromKeyValueStore) (deserialise: Deserialise<'a>) =
        match get listReq key with
        | Error e -> Error e
        | Ok lr ->
            match lr with
            | AllPages p ->
                p
                |> List.map deserialise
                |> List.choose (fun r ->
                                match r with
                                | Ok o -> Some o
                                | Error e -> None )
                |> AllPages |> Ok
            | SinglePage p ->
                { ItemsPerPage = p.ItemsPerPage
                  CurrentPage = p.CurrentPage
                  Items = p.Items
                          |> List.map deserialise
                          |> List.choose (fun r ->
                                match r with
                                | Ok o -> Some o
                                | Error e -> None )
                  TotalPages = p.TotalPages
                  TotalItems = p.TotalItems }
                  |> SinglePage
                  |> Ok
        
    let getLexographic key searchTerm req (get:GetLexographic) =
        get key searchTerm req

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
        printfn "Connecting to redis at host: %s" host
        let config = ConfigurationOptions()
        config.AllowAdmin <- true //To allow flushing read model
        config.EndPoints.Add host
        StackExchange.Redis.ConnectionMultiplexer.Connect(config)

    let reset (redis:ConnectionMultiplexer) () =
        let endPoints = redis.GetEndPoints()
        match endPoints.Length with
        | 1 -> redis.GetServer(endPoints.[0]).FlushAllDatabases() |> Ok
        | _ -> Error "Unsupported configuration at present"

    let get (redis:ConnectionMultiplexer) (key:string) =
        let db = redis.GetDatabase()
        let result : string = ~~db.StringGet(~~key)
        match result with
        | NotNull -> Ok <| Json result
        | _ -> Error (sprintf "Could not get read model from Redis: %s" key)

    let delete (redis:ConnectionMultiplexer) (key:string) =
        let db = redis.GetDatabase()
        match db.KeyDelete(~~key) with
        | true -> Ok()
        | false -> "Could not remove the key from redis: " + key |> Error

    let getListItems (redis:ConnectionMultiplexer) (pageReq:ListRequest) (key:string) =
        let db = redis.GetDatabase()
        let result : string seq = db.SetMembers(~~key) |> Seq.map ( ~~ )
        Ok <| (result |> Seq.map Json |> Seq.toList)

    let getSortedListItems (redis:ConnectionMultiplexer) (listReq:ListRequest) (key:Key) =
        let db = redis.GetDatabase()
        match listReq with
        | All -> 
            db.SortedSetRangeByRank(~~key) 
            |> Seq.map ( ~~ )
            |> Seq.toList
            |> List.map Json
            |> AllPages
        | Paged p -> 
            let start = (p.Page - 1) * p.ItemsPerPage
            let totalItems = db.SortedSetLength(~~key)
            let items : string seq = 
                db.SortedSetRangeByScore(~~key, float start, start + p.ItemsPerPage |> float) 
                |> Seq.map ( ~~ )
            { ItemsPerPage = p.ItemsPerPage
              CurrentPage = p.Page
              Items = items |> Seq.map Json |> Seq.toList
              TotalPages = ceil ((float totalItems) / (float p.ItemsPerPage)) |> int
              TotalItems = totalItems |> int }
            |> SinglePage
        |> Ok

    let lexographicSearch (redis:ConnectionMultiplexer) (key:Key) (searchTerm:string) (listReq:ListRequest) =
        let db = redis.GetDatabase()
        match listReq with
        | All ->
            db.SortedSetRangeByValue(~~key, ~~searchTerm, ~~(searchTerm + "\xff")) 
            |> Seq.map ( ~~ )
            |> Seq.toList 
            |> AllPages
        | Paged p ->
            let allItems : string seq = db.SortedSetRangeByValue(~~key, ~~searchTerm, ~~(searchTerm + "\xff")) |> Seq.map ( ~~ )
            let items =
                allItems
                |> Seq.skipSafe ((p.Page - 1) * p.ItemsPerPage)
                |> Seq.truncate p.ItemsPerPage
            let totalItems = allItems |> Seq.length
            { ItemsPerPage = p.ItemsPerPage
              CurrentPage = p.Page
              Items = items |> Seq.toList
              TotalPages = ceil ((float totalItems) / (float p.ItemsPerPage)) |> int
              TotalItems = totalItems |> int }
            |> SinglePage
        |> Ok

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
            | None -> "BackboneTaxon:Genus:" + g
            | Some s -> "BackboneTaxon:Species:" + s

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

    let private getPage r = match r with | AllPages p -> p | _ -> []

    let tryFindByLatinName family genus species getSortedList getSingle deserialise =
        let tryFindId key = KeyValueStore.getSortedList<Guid> All key getSortedList deserialiseGuid
        let tryFindReadModel (ids:Guid list) = 
            // Where there are multiple matches for a genus, return the accepted one...
            match ids |> List.length with
            | 1 -> RepositoryBase.getSingle<BackboneTaxon> (ids.Head.ToString()) getSingle deserialise
            | id when ids.Length > 1 ->
                let results = ids |> List.map (fun i -> RepositoryBase.getSingle<BackboneTaxon> (i.ToString()) getSingle deserialise)
                let m = results |> List.tryFind(fun x -> match x with | Ok t -> t.TaxonomicStatus = "accepted" | Error e -> false)
                match m with
                | Some t -> t
                | None -> Error "No accepted matches by latin name"
            | _ -> Error "No matches by latin name"
        (family,genus,species)
        |> toNameSearchKey
        |> tryFindId
        |> lift getPage
        |> bind tryFindReadModel

    // Search names to find possible matches, returning whole taxa
    let findMatches identity getSortedList getSingle deserialise : Result<BackboneTaxon list,string> =
        let search key = KeyValueStore.getSortedList All key getSortedList deserialiseGuid
        let fetchAllById ids = 
            ids 
            |> List.map (fun id -> RepositoryBase.getSingle<BackboneTaxon> (id.ToString()) getSingle deserialise)
            |> List.choose (fun x -> match x with | Ok r -> Some r | Error e -> None)
            |> Ok
        identity
        |> toRankSearchKey
        |> search
        |> lift getPage
        |> bind fetchAllById

    // Search names to find possible matches, returning taxon names
    let search identity (getLexographical:GetLexographic) pageReq deserialise : Result<string list, string> =
        let rank,ln =
            match identity with
            | Family ln -> "Family", unwrapLatinName ln
            | Genus ln -> "Genus", unwrapLatinName ln
            | Species (ln,eph,auth) -> "Species", unwrapLatinName ln
        let key = "Autocomplete:BackboneTaxon:" + rank
        KeyValueStore.getLexographic key ln All getLexographical
        |> lift (fun r -> match r with | AllPages p -> p | SinglePage p -> p.Items )

    let validate (query:BackboneQuery) get getList deserialise =
        match query with
        | ValidateById id -> getById id get deserialise
        | Validate i -> 
            let matches = findMatches i getList get deserialise
            Error "Not implemented"

    // Traces a backbone taxon to its most recent name (e.g. synonym -> synonym -> accepted name)
    let tryTrace rank auth readStoreGet deserialise (matches:BackboneTaxon list) =

        let rec lookupSynonym (id:string) =
            let guid =
                match id with
                | Prefix "TaxonId " rest -> Guid.TryParse rest
                | _ -> Guid.TryParse id
            match fst guid with
            | false -> Error "Invalid taxon specified"
            | true -> 
                getById (TaxonId (snd guid)) readStoreGet deserialise
                |> Result.bind (fun syn ->
                    match syn.TaxonomicStatus with
                    | "accepted" -> Ok [syn]
                    | "synonym"
                    | "misapplied" -> lookupSynonym syn.TaxonomicAlias
                    | "doubtful" -> Ok [syn]
                    | _ -> Error "Could not determine taxonomic status" )

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
            match auth with
            | None -> 
                // Return only the accepted genus if in multiple families
                match rank with
                | "Genus" -> matches |> List.filter (fun t -> t.TaxonomicStatus = "accepted") |> Ok
                | _ -> matches |> Ok
            | Some auth ->
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

    let import set setSortedList serialise (taxon:BackboneTaxon) = 
        RepositoryBase.setSingle (taxon.Id.ToString()) taxon set serialise |> ignore
        RepositoryBase.setSortedListItem (taxon.Id.ToString()) (sprintf "BackboneTaxon:%s:%s" taxon.Rank taxon.LatinName) 0. setSortedList |> ignore
        RepositoryBase.setSortedListItem taxon.LatinName ("Autocomplete:BackboneTaxon:" + taxon.Rank) 0. setSortedList |> ignore
        Ok ()