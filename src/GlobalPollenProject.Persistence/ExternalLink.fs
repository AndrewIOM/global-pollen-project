module ExternalLink

open GlobalPollenProject.Core.Types
open System
open System.IO
open System.Net.Http
open System.Net.Http.Headers
open Newtonsoft.Json

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

let getRequest baseUri (query:string) =
    use client = new HttpClient()
    client.BaseAddress <- Uri(baseUri)
    client.DefaultRequestHeaders.Accept.Clear ()
    client.DefaultRequestHeaders.Accept.Add(MediaTypeWithQualityHeaderValue("application/json"))
    client.GetAsync(query) |> Async.AwaitTask |> Async.RunSynchronously

let unwrap (LatinName l) = l
let unwrapS (SpecificEphitet s) = s

let getGbifId (req:LinkRequest) : int option =
    let query =
        let qbase = sprintf "species/match?status=accepted&strict=true&kingdom=Plantae&family=%s" req.Family
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
        | "Exact" -> Some gbifResult.GbifId
        | _ -> None

let getNeotomaId (req:LinkRequest) =
    let query = match req.Identity with
                | Family f -> unwrap f
                | Genus g -> unwrap g
                | Species (g,s,_) -> sprintf "%s %s" (unwrap g) (unwrapS s)
    let response = getRequest "http://api.neotomadb.org/v1/" query
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
            | 1 -> None
            | _ -> Some neoResult.Result.Head.TaxonId
