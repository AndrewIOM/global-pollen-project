module ExternalLink

open GlobalPollenProject.Core.DomainTypes
open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open Newtonsoft.Json
open ReadModels

[<CLIMutable>]
type GbifTaxonResult = 
    { [<JsonProperty("scientificName")>] ScientificName:string
      [<JsonProperty("canonicalName")>] CanonicalName:string
      [<JsonProperty("rank")>] Rank:string
      [<JsonProperty("matchType")>] MatchType:string
      [<JsonProperty("usageKey")>] GbifId:int }

[<CLIMutable>]
type NeotomaTaxonResult = {
    [<JsonProperty("TaxonName")>] TaxonName:string
    [<JsonProperty("TaxonCode")>] TaxonCode:string
    [<JsonProperty("TaxonID")>] TaxonId:int
}

[<CLIMutable>]
type NeotomaResult = {
    [<JsonProperty("success")>] Success:int
    [<JsonProperty("data")>] Result: NeotomaTaxonResult list }

[<CLIMutable>]
type EolSearchResultItem =
    { [<JsonProperty("id")>] Id:int
      [<JsonProperty("title")>] LatinName:string
      [<JsonProperty("link")>] Url:string
      [<JsonProperty("content")>] Content:string }

[<CLIMutable>]
type EolSearchResult = 
    { [<JsonProperty("totalResults")>] TotalResults:int
      [<JsonProperty("results")>] Results:EolSearchResultItem list }

let tryGetRequest baseUri (query:string) =
    async {
        use client = new HttpClient()
        client.BaseAddress <- Uri(baseUri)
        client.DefaultRequestHeaders.Accept.Clear ()
        client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
        try
            let! result = client.GetAsync(query) |> Async.AwaitTask
            match result.IsSuccessStatusCode with
            | false -> return None
            | true ->
                use! responseStream = result.Content.ReadAsStreamAsync() |> Async.AwaitTask
                let streamReader = new StreamReader(responseStream)
                let jsonMessage = streamReader.ReadToEnd()
                return Some jsonMessage
        with
        | _ ->
            printfn "There was a problem requesting %s" query
            return None
    }

let tryDeserialiseJson<'a> str =
    try JsonConvert.DeserializeObject<'a> str |> Some
    with
    | e ->
        printfn "Error when de-serialising json from API: %s" e.Message
        None

let unwrap (LatinName l) = l
let unwrapS (SpecificEphitet s) = s
let unwrapAuth (Scientific s) = s

let toLinkRequest (taxon:BackboneTaxon) =
    match taxon.Rank with
    | "Family" -> { Family = taxon.Family; Genus = None; Species = None; Identity = Family (LatinName taxon.Family) } |> Ok
    | "Genus" -> { Family = taxon.Family; Genus = Some taxon.Genus; Species = None; Identity = Genus (LatinName taxon.Genus) } |> Ok
    | "Species" -> { Family = taxon.Family; 
                     Genus = Some taxon.Genus; 
                     Species = Some taxon.Species; 
                     Identity = Species (LatinName taxon.Genus,SpecificEphitet taxon.Species,Scientific taxon.NamedBy) } 
                     |> Ok
    | _ -> Error "Backbone taxon read model was not formatted correctly"

let getGbifId (req:LinkRequest) =
    let query =
        let qBase = sprintf "species/match?status=accepted&kingdom=Plantae&family=%s" req.Family
        let q1 = match req.Genus with
                 | Some g -> sprintf "%s&genus=%s" qBase g
                 | None -> qBase
        let q2 = match req.Species with
                 | Some s -> sprintf "%s&species=%s" q1 s
                 | None -> q1
        match req.Identity with
        | Family f -> sprintf "%s&rank=family&name=%s" q2 (unwrap f)
        | Genus g -> sprintf "%s&rank=genus&name=%s" q2 (unwrap g)
        | Species (s,_,_) -> sprintf "%s&rank=species&name=%s" q2 (unwrap s)

    let json = tryGetRequest "http://api.gbif.org/v1/" query |> Async.RunSynchronously
    match json with
    | None -> None
    | Some json ->
        let gbifResult = tryDeserialiseJson<GbifTaxonResult> json
        match gbifResult with
        | None -> None
        | Some gbif ->
            match gbif.MatchType with
            | "EXACT" -> Some gbif.GbifId
            | _ -> None

let getNeotomaId (req:LinkRequest) =
    let query = match req.Identity with
                | Family f -> unwrap f
                | Genus g -> unwrap g
                | Species (_,s,_) -> unwrapS s
    let response = tryGetRequest "http://api.neotomadb.org/v1/data/" ("taxa?taxonname=" + query) |> Async.RunSynchronously
    match response with
    | None -> None
    | Some json ->
        let result = tryDeserialiseJson<NeotomaResult> json
        match result with
        | None -> None
        | Some neoResult ->
            match neoResult.Success with
            | 0 -> None
            | _ ->
                match neoResult.Result.Length with
                | 1 -> Some neoResult.Result.Head.TaxonId
                | _ -> None

let getEncyclopediaOfLifeId (req:LinkRequest) =
    let query = match req.Identity with
                | Family f -> unwrap f
                | Genus g -> unwrap g
                | Species (_,s,auth) -> (unwrapS s) + " " + (unwrapAuth auth)
    let response = tryGetRequest "http://eol.org/api/search/" ("1.0.json?q=" + query + "&page=1&exact=true") |> Async.RunSynchronously
    match response with
    | None -> None
    | Some json ->
        let result = tryDeserialiseJson<EolSearchResult> json
        match result with
        | None -> None
        | Some eolResult ->
            match eolResult.TotalResults with
            | 0 -> None
            | _ -> Some eolResult.Results.Head.Id

[<CLIMutable>]
type EolVernacularName = 
    { [<JsonProperty("vernacularName")>] Name:string
      [<JsonProperty("language")>] Language:string
      [<JsonProperty("eol_preferred")>] Preferred: bool }

[<CLIMutable>]
type EolDataObject = 
    { [<JsonProperty("vettedStatus")>] VettedStatus:string
      [<JsonProperty("mimeType")>] MimeType:string
      [<JsonProperty("rights")>] Rights: string
      [<JsonProperty("rightsHolder")>] RightsHolder: string
      [<JsonProperty("eolMediaURL")>] MediaUrl: string
      [<JsonProperty("description")>] Description: string
      [<JsonProperty("license")>] License: string }

[<CLIMutable>]
type EolPageResult = 
    { [<JsonProperty("vernacularNames")>] VernacularNames:EolVernacularName list
      [<JsonProperty("dataObjects")>] DataObjects: EolDataObject list }

[<CLIMutable>]
type EoLResult = { [<JsonProperty("taxonConcept")>] Concept: EolPageResult }

let stripTags html =
    System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "")

let capitaliseFirstLetters s =
    System.Text.RegularExpressions.Regex.Replace(s, @"(^\w)|(\s\w)", fun (m:System.Text.RegularExpressions.Match) -> m.Value.ToUpper())

let getEncyclopediaOfLifeCacheData taxonId =
    let req = taxonId |> sprintf "1.0.json?batch=false&id=%i&images_per_page=1&images_page=1&videos_per_page=0&videos_page=0&sounds_per_page=0&sounds_page=0&maps_per_page=0&maps_page=0&texts_per_page=2&texts_page=1&subjects=overview&licenses=all&details=true&common_names=true&synonyms=false&references=true&taxonomy=false&vetted=0&cache_ttl=&language=en"
    let response = tryGetRequest "https://eol.org/api/pages/" req |> Async.RunSynchronously
    match response with
    | None ->
        printfn "Could not get EoL cache data for %i" taxonId
        None
    | Some json ->
        let deserialiseResult = tryDeserialiseJson<EoLResult> json
        match deserialiseResult with
        | None ->
            printfn "Error: could not deserialise EoL result: %s" json
            None
        | Some result ->
            printfn "%A" result
            let commonEnglishName =
                let n =
                    result.Concept.VernacularNames
                    |> List.filter (fun n -> n.Preferred)
                    |> List.tryFind (fun n -> n.Language = "en")
                match n with
                | Some name -> name.Name |> capitaliseFirstLetters
                | None -> ""             
            let photoUrl,photoAttribution =
                let r =
                    result.Concept.DataObjects
                    |> List.filter (fun o -> o.MimeType = "image/jpeg")
                    |> List.tryFind (fun o -> o.VettedStatus = "Trusted")
                match r with
                | Some i -> i.MediaUrl, i.RightsHolder
                | None -> "",""
            let desc,descAttribution =
                let r =
                    result.Concept.DataObjects
                    |> List.filter (fun o -> o.MimeType = "text/html" || o.MimeType = "text/plain")
                    |> List.tryFind (fun o -> o.VettedStatus = "Trusted")
                match r with
                | Some t -> 
                    (if isNull t.Description then "" else t.Description |> stripTags), 
                    (if isNull t.RightsHolder then "" else t.RightsHolder |> stripTags)
                | None -> "",""            
            { CommonEnglishName         = commonEnglishName
              PhotoUrl                  = photoUrl
              PhotoAttribution          = photoAttribution
              Description               = desc
              DescriptionAttribution    = descAttribution
              Retrieved                 = DateTime.Now }
            |> Some
        
[<CLIMutable>]
type NeotomaSite = {
    LongitudeWest: float
    LongitudeEast: float
    LatitudeNorth: float
    LatitudeSouth: float
    SiteID: int
}

[<CLIMutable>]
type NeotomaOccurrenceData = {
    AgeOldest: Nullable<float>
    AgeYoungest: Nullable<float>
    DatasetType: string
    DatasetID: int
    Site: NeotomaSite
}
      
[<CLIMutable>]
type NeotomaOccurrenceApiResult = {
    [<JsonProperty("success")>] Success: int
    [<JsonProperty("data")>] Data: NeotomaOccurrenceData list
}

/// Caches occurrences from 50kyBP for a neotoma taxon id.
let getNeotomaCacheData neotomaId =
    let neotomaUri = sprintf "https://api.neotomadb.org/v1/data/datasets?taxonids=%i&ageof=taxon&ageold=%i&ageyoung=%i" neotomaId 50000 1000
    let response = tryGetRequest "https://eol.org/api/pages/" neotomaUri |> Async.RunSynchronously
    match response with
    | None ->
        printfn "Could not get neotoma cache data for %i" neotomaId
        None
    | Some json ->
        let deserialiseResult = tryDeserialiseJson<NeotomaOccurrenceApiResult> json
        match deserialiseResult with
        | None ->
            printfn "Error: could not deserialise neotoma result. Has their API changed? %s" json
            None
        | Some result ->
            if result.Success <> 1 then
                printfn "Neotoma API reported an error"
                None
            else
                result.Data
                |> List.where(fun ds -> ds.AgeOldest.HasValue && ds.AgeYoungest.HasValue)
                |> List.map(fun ds -> {
                    AgeOldest = int ds.AgeOldest.Value
                    AgeYoungest = int ds.AgeYoungest.Value
                    Latitude = ds.Site.LatitudeNorth
                    Longitude = ds.Site.LongitudeEast
                    Proxy = ds.DatasetType
                    SiteId = ds.Site.SiteID
                }) |> List.distinctBy(fun s -> s.SiteId)
                |> fun occ -> {
                    RefreshTime = DateTime.Now
                    Occurrences = occ
                } |> Some
