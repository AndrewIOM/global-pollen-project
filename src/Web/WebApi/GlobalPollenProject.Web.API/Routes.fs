module Routes

open Giraffe
open Microsoft.AspNetCore.Http

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


// let notLoggedIn =
//     RequestErrors.UNAUTHORIZED
//         "Basic"
//         "Some Realm"
//         "You must be logged in."

//let mustBeLoggedIn = requiresAuthentication notLoggedIn

// let publicApi =
//     GET >=>
//     choose [
//         // route   "/backbone/match"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryMatch
//         route   "/backbone/trace"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryTrace
//         route   "/backbone/search"          >=> queryRequestToApiResponse<BackboneSearchRequest,string list> Backbone.searchNames
//         route   "/taxon/search"             >=> queryRequestToApiResponse<TaxonAutocompleteRequest,TaxonAutocompleteItem list> Taxonomy.autocomplete
//         route   "/grain/location"           >=> topUnknownGrainsHandler
//     ]

let webApp : HttpFunc -> HttpContext -> HttpFuncResult =
    //mustBeLoggedIn >=>
        choose [
            route "/ping"   >=> text "pong"
            route "/"       >=> htmlFile "/pages/index.html" ]
