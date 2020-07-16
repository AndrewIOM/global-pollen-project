namespace GlobalPollenProject.Web.API

open Connections
open Microsoft.AspNetCore.Mvc

module ActionResult =
    let ofAsync (res: Async<IActionResult>) =
        res |> Async.StartAsTask    

[<ApiController>]
type HomeController() =
    inherit ControllerBase()
    
    [<HttpGet>]
    [<Route("/")>]
    member __.Index() =  
        RedirectResult "~/swagger"


[<Route("api/v1/[controller]")>]
[<ApiController>]
type TaxonomyController(core: Connections.CoreMicroservice) =
    inherit ControllerBase()

    [<HttpGet>]
    [<Route("/search")>]
    member this.Search(req:Requests.BackboneSearchRequest) =
        ActionResult.ofAsync <| async {
            if not this.ModelState.IsValid
            then return BadRequestResult() :> IActionResult
            else
                let! result =
                    req
                    |> CoreActions.Backbone.search
                    |> core.Apply
                match result with
                | Ok r -> return JsonResult(r) :> IActionResult
                | Error _ -> return StatusCodeResult(500) :> IActionResult
        }
 
    [<HttpGet>]
    [<Route("/trace")>]
    member this.Trace(req:BackboneSearchRequest) =
        ActionResult.ofAsync <| async {
            if not this.ModelState.IsValid
            then return BadRequestResult() :> IActionResult
            else
                let! result =
                    req
                    |> CoreActions.Backbone.search
                    |> core.Apply
                match result with
                | Ok r -> return JsonResult(r) :> IActionResult
                | Error _ -> return StatusCodeResult(500) :> IActionResult
        }

[<Route("api/v1/[controller]")>]
[<ApiController>]
type ReferenceController(core: Connections.CoreMicroservice) =
    inherit ControllerBase()

    [<HttpGet>]
    member this.Index(id:System.Guid) =
        ActionResult.ofAsync <| async {
            if not this.ModelState.IsValid
            then return BadRequestResult() :> IActionResult
            else
                let! result =
                    id
                    |> CoreActions.MRC.getById
                    |> core.Apply
                match result with
                | Ok r -> return JsonResult(r) :> IActionResult
                | Error _ -> return StatusCodeResult(500) :> IActionResult
        }


// GPP Public API
// Read-only in the first instance

// Commands:
// - Taxonomy
// ---- Search by Botanical Name
// ---- Get by ID
// - Reference Collections
// ---- Summary by ID
// ---- Contents by ID
// ---- Search by properties

// let publicApi =
//     GET >=>
//     choose [
//         // route   "/backbone/match"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryMatch
//         route   "/backbone/trace"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryTrace
//         route   "/backbone/search"          >=> queryRequestToApiResponse<BackboneSearchRequest,string list> Backbone.searchNames
//         route   "/taxon/search"             >=> queryRequestToApiResponse<TaxonAutocompleteRequest,TaxonAutocompleteItem list> Taxonomy.autocomplete
//     ]
