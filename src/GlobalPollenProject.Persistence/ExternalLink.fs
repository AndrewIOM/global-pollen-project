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
    [<JsonProperty("TaxonId")>] TaxonId:int
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

let getRequest baseUri (query:string) =
    use client = new HttpClient()
    client.BaseAddress <- Uri(baseUri)
    client.DefaultRequestHeaders.Accept.Clear ()
    client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
    client.GetAsync(query) |> Async.AwaitTask |> Async.RunSynchronously

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
        let qbase = sprintf "species/match?status=accepted&kingdom=Plantae&family=%s" req.Family
        let q1 = match req.Genus with
                 | Some g -> sprintf "%s&genus=%s" qbase g
                 | None -> qbase
        let q2 = match req.Species with
                 | Some s -> sprintf "%s&species=%s" q1 s
                 | None -> q1
        match req.Identity with
        | Family f -> sprintf "%s&rank=family&name=%s" q2 (unwrap f)
        | Genus g -> sprintf "%s&rank=genus&name=%s" q2 (unwrap g)
        | Species (s,_,_) -> sprintf "%s&rank=species&name=%s" q2 (unwrap s)

    let response = getRequest "http://api.gbif.org/v1/" query
    match response.IsSuccessStatusCode with
    | false -> None
    | true ->
        use responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask |> Async.RunSynchronously
        let jsonMessage = (new StreamReader(responseStream)).ReadToEnd()
        let gbifResult : GbifTaxonResult = JsonConvert.DeserializeObject<GbifTaxonResult>(jsonMessage)
        match gbifResult.MatchType with
        | "EXACT" -> Some gbifResult.GbifId
        | _ -> None

let getNeotomaId (req:LinkRequest) =
    let query = match req.Identity with
                | Family f -> unwrap f
                | Genus g -> unwrap g
                | Species (g,s,_) -> unwrapS s
    let response = getRequest "http://api.neotomadb.org/v1/data/" ("taxa?taxonname=" + query)
    match response.IsSuccessStatusCode with
    | false -> None
    | true ->
        use responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask |> Async.RunSynchronously
        let jsonMessage = (new StreamReader(responseStream)).ReadToEnd()
        let neoResult : NeotomaResult = JsonConvert.DeserializeObject<NeotomaResult>(jsonMessage)
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
                | Species (g,s,auth) -> (unwrapS s) + " " + (unwrapAuth auth)
    let response = getRequest "http://eol.org/api/search/" ("1.0.json?q=" + query + "&page=1&exact=true")
    match response.IsSuccessStatusCode with
    | false -> None
    | true ->
        use responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask |> Async.RunSynchronously
        let jsonMessage = (new StreamReader(responseStream)).ReadToEnd()
        let result : EolSearchResult = JsonConvert.DeserializeObject<EolSearchResult>(jsonMessage)
        match result.TotalResults with
        | 0 -> None
        | _ -> Some result.Results.Head.Id

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

let stripTags html =
    System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "")

let capitaliseFirstLetters s =
    System.Text.RegularExpressions.Regex.Replace(s, @"(^\w)|(\s\w)", fun (m:System.Text.RegularExpressions.Match) -> m.Value.ToUpper())

let getEncyclopediaOfLifeCacheData taxonId =
    let req = taxonId |> sprintf "1.0.json?batch=false&id=%i&images_per_page=1&images_page=1&videos_per_page=0&videos_page=0&sounds_per_page=0&sounds_page=0&maps_per_page=0&maps_page=0&texts_per_page=2&texts_page=1&subjects=overview&licenses=all&details=true&common_names=true&synonyms=false&references=true&taxonomy=false&vetted=0&cache_ttl=&language=en"
    let response = getRequest "http://eol.org/api/pages/" req
    match response.IsSuccessStatusCode with
    | false -> None
    | true ->
        use responseStream = response.Content.ReadAsStreamAsync() |> Async.AwaitTask |> Async.RunSynchronously
        let jsonMessage = (new StreamReader(responseStream)).ReadToEnd()
        let result : EolPageResult = JsonConvert.DeserializeObject<EolPageResult>(jsonMessage)

        let commonEnglishName =
            let n =
                result.VernacularNames
                |> List.filter (fun n -> n.Preferred)
                |> List.tryFind (fun n -> n.Language = "en")
            match n with
            | Some name -> name.Name |> capitaliseFirstLetters
            | None -> "" 

        let photoUrl,photoAttribution =
            let r =
                result.DataObjects
                |> List.filter (fun o -> o.MimeType = "image/jpeg")
                |> List.tryFind (fun o -> o.VettedStatus = "Trusted")
            match r with
            | Some i -> i.MediaUrl, i.RightsHolder
            | None -> "",""

        let desc,descAttribution =
            let r =
                result.DataObjects
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