namespace GlobalPollenProject.Web.API

open Microsoft.AspNetCore.Mvc

[<ApiController>]
type HomeController() =
    inherit ControllerBase()

    [<HttpGet>]
    member __.Index() =
        RedirectResult "~/swagger"


[<Route("api/v1/[controller]")>]
[<ApiController>]
type TaxonomyController() =
    inherit ControllerBase()

    [<HttpGet>]
    member __.Search() =
        let values = [|"value1"; "value2"|]
        ActionResult<string[]>(values)



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
