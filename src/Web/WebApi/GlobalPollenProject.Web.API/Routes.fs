namespace GlobalPollenProject.Web.API

open System.Collections.Generic
open Connections
open Microsoft.AspNetCore.Mvc
open GlobalPollenProject.Web.API.Extensions
open ReadModels

[<ApiController>]
type HomeController() =
    inherit ControllerBase()
    
    [<HttpGet>]
    [<Route("/")>]
    [<ApiExplorerSettings(IgnoreApi = true)>]
    member __.Index() =  
        RedirectResult "~/swagger"

[<ApiController>]
type ErrorController() =
    inherit ControllerBase()
    
    [<HttpGet>]
    [<Route("/error")>]
    [<ApiExplorerSettings(IgnoreApi = true)>]
    member this.Error() = this.Problem()


[<Route("api/v1/[controller]")>]
[<ApiController>]
type BackboneController(core: Connections.CoreMicroservice) =
    inherit ControllerBase()

    [<HttpGet>]
    [<Route("search")>]
    [<ProducesResponseType(typeof<BackboneTaxon>, 200)>]
    [<ProducesResponseType(typeof<Dictionary<string,string[]>>, 400)>]
    member this.Search req =
        this.apiAction(req, CoreActions.Backbone.search, core)
 
    /// <summary>
    /// Traces any botanical name to the currently-used name.
    /// </summary>
    /// <remarks>
    /// This function traces the history of the taxon in the Global Pollen Project
    /// backbone to determine if there is a currently accepted name. Names may be split,
    /// clumped, or renamed through time.
    /// </remarks>
    /// <returns></returns>
    [<HttpGet>]
    [<Route("trace")>]
    [<ProducesResponseType(typeof<BackboneTaxon>, 200)>]
    [<ProducesResponseType(typeof<Dictionary<string,string[]>>, 400)>]
    member this.Trace req =
        this.apiAction(req, CoreActions.Backbone.tryTrace, core)


[<Route("api/v1/[controller]")>]
[<ApiController>]
type TaxonController(core: Connections.CoreMicroservice) =
    inherit ControllerBase()
    
    [<HttpGet>]
    member this.Index(id:System.Guid) =
        this.apiAction(id, CoreActions.MRC.getById, core)

    [<HttpGet>]
    [<Route("{family}")>]
    [<ProducesResponseType(typeof<TaxonDetail>, 200)>]
    [<ProducesResponseType(typeof<Dictionary<string,string[]>>, 400)>]
    member this.Family(family:string) =
        this.apiAction(family, CoreActions.MRC.getFamily, core)

    [<HttpGet>]
    [<Route("{family}/{genus}")>]
    [<ProducesResponseType(typeof<TaxonDetail>, 200)>]
    [<ProducesResponseType(typeof<Dictionary<string,string[]>>, 400)>]
    member this.Genus(family:string, genus:string) =
        let action (f,g) = CoreActions.MRC.getGenus f g
        this.apiAction((family, genus), action, core)

    [<HttpGet>]
    [<Route("{family}/{genus}/{species}")>]
    [<ProducesResponseType(typeof<TaxonDetail>, 200)>]
    [<ProducesResponseType(typeof<Dictionary<string,string[]>>, 400)>]
    member this.Species(family:string, genus:string, species:string) =
        let action (f,g,s) = CoreActions.MRC.getSpecies f g s
        this.apiAction((family, genus, species), action, core)


[<Route("api/v1/[controller]")>]
[<ApiController>]
type CollectionController(core: Connections.CoreMicroservice) =
    inherit ControllerBase()

    [<HttpGet>]
    [<ProducesResponseType(typeof<ReferenceCollectionDetail>, 200)>]
    [<ProducesResponseType(typeof<Dictionary<string,string[]>>, 400)>]
    member this.Index(id:System.Guid) =
        this.apiAction(id, CoreActions.MRC.getById, core)
