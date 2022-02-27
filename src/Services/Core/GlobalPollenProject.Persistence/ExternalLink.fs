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
    [<JsonProperty("taxonname")>] TaxonName:string
    [<JsonProperty("taxonid")>] TaxonId: int
}

[<CLIMutable>]
type NeotomaResult = {
    [<JsonProperty("status")>] Status: string
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
            | false -> 
                printfn "Problem getting request [%s]: %s" (result.StatusCode.ToString()) (result.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously)
                return None
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

    let json = tryGetRequest "https://api.gbif.org/v1/" query |> Async.RunSynchronously
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
    let response = tryGetRequest "https://api.neotomadb.org/v2.0/" ("data/taxa?taxonname=" + query) |> Async.RunSynchronously
    match response with
    | None -> None
    | Some json ->
        let result = tryDeserialiseJson<NeotomaResult> json
        match result with
        | None -> None
        | Some neoResult ->
            match neoResult.Status with
            | "success" ->
                match neoResult.Result.Length with
                | 1 -> Some neoResult.Result.Head.TaxonId
                | _ -> None
            | _ -> None

let getEncyclopediaOfLifeId (req:LinkRequest) =
    let query = match req.Identity with
                | Family f -> unwrap f
                | Genus g -> unwrap g
                | Species (_,s,auth) -> (unwrapS s) + " " + (unwrapAuth auth)
    let response = tryGetRequest "https://eol.org/api/search/" ("1.0.json?q=" + query + "&page=1&exact=true") |> Async.RunSynchronously
    match response with
    | None -> None
    | Some json ->
        let result = tryDeserialiseJson<EolSearchResult> json
        match result with
        | None -> None
        | Some eolResult ->
            match eolResult.TotalResults with
            | 0 -> None
            | _ -> 
                // EoL seems to now ignore the strict option. Additional check here.
                match eolResult.Results |> Seq.tryFind(fun r -> r.LatinName = query) with
                | Some r -> Some r.Id
                | None -> None

[<CLIMutable>]
type EolVernacularName = 
    { [<JsonProperty("vernacularName")>] Name:string
      [<JsonProperty("language")>] Language:string
      [<JsonProperty("eol_preferred")>] Preferred: bool }

[<CLIMutable>]
type EolDataObject = 
    { [<JsonProperty("vettedStatus")>] VettedStatus:string
      [<JsonProperty("mimeType")>] MimeType:string
      [<JsonProperty("dataType")>] DataType:string
      [<JsonProperty("rights")>] Rights: string
      [<JsonProperty("rightsHolder")>] RightsHolder: string
      [<JsonProperty("eolMediaURL")>] MediaUrl: string
      [<JsonProperty("description")>] Description: string
      [<JsonProperty("license")>] License: string
      [<JsonProperty("language")>] Language: string }

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
    let req = taxonId |> sprintf "1.0.json?batch=false&id=%i&images_per_page=1&images_page=1&videos_per_page=0&videos_page=0&sounds_per_page=0&sounds_page=0&maps_per_page=0&maps_page=0&texts_per_page=75&texts_page=1&subjects=overview&licenses=all&details=true&common_names=true&synonyms=false&references=false&taxonomy=false&vetted=0&language=en"
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
            let photoUrl,photoAttribution,photoLicense =
                if result.Concept.DataObjects |> box |> isNull then "", "", ""
                else
                    let r =
                        result.Concept.DataObjects
                        |> List.filter (fun o -> o.MimeType = "image/jpeg")
                        |> List.tryFind (fun o -> o.VettedStatus = "Trusted")
                    match r with
                    | Some i -> i.MediaUrl, i.RightsHolder, i.License
                    | None -> "","",""
            let desc,descAttribution,descLicence =
                if result.Concept.DataObjects |> box |> isNull then "", "", ""
                else
                    let r =
                        result.Concept.DataObjects
                        |> List.filter (fun o -> o.DataType = "http://purl.org/dc/dcmitype/Text")
                        |> List.tryFind (fun o -> o.VettedStatus = "Trusted" && o.Language = "en")
                    match r with
                    | Some t -> 
                        (if isNull t.Description then "" else t.Description |> stripTags), 
                        (if isNull t.RightsHolder then "" else t.RightsHolder |> stripTags),
                        t.License
                    | None -> "","",""
            { CommonEnglishName         = commonEnglishName
              PhotoUrl                  = photoUrl
              PhotoAttribution          = photoAttribution
              PhotoLicence              = photoLicense
              Description               = desc
              DescriptionAttribution    = descAttribution
              DescriptionLicence        = descLicence
              Retrieved                 = DateTime.Now }
            |> Some

[<CLIMutable>]
type NeotomaSite = {
    [<JsonProperty("location")>] Location: string
    [<JsonProperty("siteid")>] SiteID: int
    [<JsonProperty("datasettype")>] DatasetType: string
}

[<CLIMutable>]
type NeotomaAge = {
    [<JsonProperty("age")>] Age: Nullable<float>
    [<JsonProperty("ageolder")>] AgeOlder: Nullable<float>
    [<JsonProperty("ageyounger")>] AgeYounger: Nullable<float>
}

[<CLIMutable>]
type NeotomaOccurrenceData = {
    [<JsonProperty("occid")>] OccurrenceId: int
    [<JsonProperty("site")>] Site: NeotomaSite
    [<JsonProperty("age")>] Age: NeotomaAge
}

[<CLIMutable>]
type NeotomaOccurrenceApiResult = {
    [<JsonProperty("status")>] Status: string
    [<JsonProperty("data")>] Data: NeotomaOccurrenceData list
}

let geoJsonToApproxCentrePoint (str:string) =
    let regex = "{\"type\":\"(Point|Polygon)\",\"crs\":{\"type\":\"name\",\"properties\":{\"name\":\"(.*)\"}},\"coordinates\":(.*)}"
    if Text.RegularExpressions.Regex.IsMatch(str, regex) |> not
    then None
    else
        let m = Text.RegularExpressions.Regex.Match(str, regex)
        if m.Groups.[2].Value <> "EPSG:4326" then None
        else 
            let points =
                (m.Groups.[3].Value).Replace("[","").Replace("]","").Split(",")
                |> Array.map float
                |> Array.chunkBySize 2
                |> Array.map(fun a -> a.[0], a.[1])
            let lon = points |> Array.map fst |> Array.average
            let lat = points |> Array.map snd |> Array.average
            (lon, lat) |> Some

/// Caches occurrences from 50kyBP for a neotoma taxon id.
let getNeotomaCacheData neotomaId =
    printfn "Attempting to cache data from Neotoma for neotoma id: %i" neotomaId
    let neotomaUri = sprintf "occurrences?taxonid=%i&ageold=%i&ageyoung=%i&limit=10000" neotomaId 50000 1000
    let response = tryGetRequest "https://api.neotomadb.org/v2.0/data/" neotomaUri |> Async.RunSynchronously
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
            if result.Status <> "success" then
                printfn "Neotoma API reported an error"
                None
            else
                result.Data
                |> List.choose(fun ds -> 
                    match geoJsonToApproxCentrePoint ds.Site.Location with
                    | None -> None
                    | Some centroid ->
                        let ageOld, ageYoung =
                            if ds.Age.AgeYounger.HasValue && ds.Age.AgeOlder.HasValue
                            then ds.Age.AgeOlder.Value, ds.Age.AgeYounger.Value
                            else if ds.Age.Age.HasValue then ds.Age.Age.Value, ds.Age.Age.Value
                            else 0, 0
                        { AgeOldest = int ageOld
                          AgeYoungest = int ageYoung
                          Latitude = snd centroid
                          Longitude = fst centroid
                          Proxy = ds.Site.DatasetType
                          SiteId = ds.Site.SiteID } |> Some ) 
                |> List.filter(fun d -> d.AgeOldest <> 0 && d.AgeYoungest <> 0)
                |> fun occ -> {
                    RefreshTime = DateTime.Now
                    Occurrences = occ
                } |> Some
