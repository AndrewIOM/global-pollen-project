module GlobalPollenProject.Web.App

open System
open System.IO
open Microsoft.AspNetCore.Http
open Giraffe
open ReadModels
open Handlers
open Docs
open Connections

let notFoundResult ctx =
    ctx |> (clearResponse >=> setStatusCode 400 >=> htmlView HtmlViews.StatusPages.notFound)


/// HttpHandlers for logging in, logging out, and configuring account settings.
module Authentication =

    open Microsoft.AspNetCore.Authentication

    let accessDenied = setStatusCode 401 >=> htmlView HtmlViews.StatusPages.denied

    let mustBeLoggedIn : HttpHandler = requiresAuthentication (redirectTo false Urls.Account.login)

    let mustBeAdmin ctx = requiresRole "Admin" accessDenied ctx

    let login : HttpHandler =
        requiresAuthentication (challenge "OpenIdConnect") >=>
        fun next ctx ->
            let user = ctx.User
            let token = ctx.GetTokenAsync("access_token") |> Async.AwaitTask |> Async.RunSynchronously
            ctx.Items.Add("access_token",token)
            redirectTo true "/" next ctx

    let logout = signOut "Cookies" >=> redirectTo false "/"


let inMaintainanceMode = false

let maintainanceResult ctx =
    ctx |> (clearResponse >=> setStatusCode 503 >=> htmlView HtmlViews.StatusPages.maintainance)

let notInMaintainanceMode next ctx : HttpFuncResult =
    match inMaintainanceMode with
    | true -> maintainanceResult next ctx
    | false -> next ctx

let prettyJson = Serialisation.serialise

let apiResultFromQuery<'a,'b> (coreAction:'a->CoreFunction<'b>) : HttpHandler =
    let error str = text str
    let success model : HttpHandler =
        fun next ctx ->
            let core = ctx.GetService<CoreMicroservice>()
            coreAction model
            |> core.Apply
            |> Async.RunSynchronously
            |> toApiResult next ctx
    tryBindQuery<'a> error None success

let viewOrError view model : HttpHandler =
    fun next ctx ->
        match model with
        | Ok m -> view m next ctx
        | Error e -> serviceErrorToView e next ctx

let coreAction action view next (ctx:HttpContext) =
    let core = ctx.GetService<CoreMicroservice>()
    action
    |> core.Apply
    |> Async.RunSynchronously
    |> renderViewResult view next ctx

let coreApiAction action next (ctx:HttpContext) =
    let core = ctx.GetService<CoreMicroservice>()
    action
    |> core.Apply
    |> Async.RunSynchronously
    |> toApiResult next ctx


////////////////////////
/// Routing lookups
////////////////////////

type TaxonLookup = { 
    OriginalId: int
    Rank: string
    Family: string
    Genus: string
    Species: string 
} with
    static member FromFile file = 
        file
        |> File.ReadAllLines
        |> Seq.skip 1
        |> Seq.map (fun s-> s.Split ',' |> fun a -> {OriginalId=int a.[0]; Rank=a.[1]; Family = a.[2]; Genus = a.[3]; Species = a.[4]})

let taxonLookup = TaxonLookup.FromFile @"Lookups/taxonlookup.csv"

let lookupNameFromOldTaxonId id =
    let old = taxonLookup |> Seq.tryFind(fun t -> t.OriginalId = id)
    match old with
    | Some t ->
        match t.Rank with
        | "Family" -> redirectTo true (sprintf "/Taxon/%s" t.Family)
        | "Genus" -> redirectTo true (sprintf "/Taxon/%s/%s" t.Family t.Genus)
        | "Species" -> redirectTo true (sprintf "/Taxon/%s/%s/%s" t.Family t.Genus t.Species)
        | _ -> notFoundResult
    | None -> notFoundResult

/////////////////////////
/// Custom HTTP Handlers
/////////////////////////

let docIndexHandler : HttpHandler =
    fun next ctx ->
        guideDocuments 
        |> Array.map (fun (meta,html) -> {Html = html; Metadata = meta |> dict; Headings = getSidebarHeadings html}) 
        |> Seq.toList
        |> HtmlViews.Guide.contentsView
        |> renderView next ctx

let docSectionHandler docSection =
    fun next ctx ->
        let r = guideDocuments |> Array.tryFind (fun (n,_) -> n |> List.find (fun (k,_) -> k = "ShortTitle") |> snd = docSection)
        match r with
        | Some (meta,html) -> 
            {Html = html; Metadata = meta |> dict; Headings = getSidebarHeadings html} 
            |> HtmlViews.Guide.sectionView
            |> renderView next ctx
        | None -> notFoundResult next ctx

let slideViewHandler (id:string) : HttpHandler =
    fun next ctx ->
        let core = ctx.GetService<CoreMicroservice>()
        let split = id.Split '/'
        match split.Length with
        | 2 -> 
            let col,slide = split.[0], split.[1] |> Net.WebUtility.UrlDecode
            CoreActions.MRC.getSlide col slide
            |> core.Apply
            |> Async.RunSynchronously // TODO Remove!
            |> renderViewResult HtmlViews.ReferenceCollections.slideView next ctx
        | 3 ->
            let col,slide = split.[0], split.[2] |> Net.WebUtility.UrlDecode
            CoreActions.MRC.getSlide col slide
            |> core.Apply
            |> Async.RunSynchronously // TODO Remove!
            |> renderViewResult HtmlViews.ReferenceCollections.slideView next ctx
        | _ -> notFoundResult next ctx

let taxonDetail (taxon:string) : HttpHandler =
    fun next ctx ->
        let core = ctx.GetService<CoreMicroservice>()
        let (f,g,s) =
            let split = taxon.Split '/'
            match split.Length with
            | 1 -> split.[0],None,None
            | 2 -> split.[0],Some split.[1],None
            | 3 -> split.[0],Some split.[1],Some split.[2]
            | _ -> "",None,None
        CoreActions.MRC.getByName f "" "" //g s //TODO Fix this
        |> core.Apply
        |> Async.RunSynchronously
        |> renderViewResult HtmlViews.Taxon.view next ctx

let taxonDetailById (id:string) : HttpHandler =
    fun next ctx ->
        match Guid.TryParse id with
        | (true,g) ->
            let core = ctx.GetService<CoreMicroservice>()
            CoreActions.MRC.getById g
            |> core.Apply
            |> Async.RunSynchronously
            |> renderViewResult HtmlViews.Taxon.view next ctx
        | (false,_) -> notFoundResult next ctx

let individualCollectionIndex = 
    coreAction (CoreActions.IndividualCollections.list {Page = 1; PageSize = 20}) HtmlViews.ReferenceCollections.tableView

let individualCollection (colId:string) version =
    coreAction (CoreActions.IndividualCollections.collectionDetail colId version) HtmlViews.ReferenceCollections.tableView

let individualCollectionLatest (colId:string) next (ctx:HttpContext) =
    let core = ctx.GetService<CoreMicroservice>()
    let latestVer = core.Apply(CoreActions.IndividualCollections.collectionDetailLatest colId) |> Async.RunSynchronously
    match latestVer with
    | Ok v -> redirectTo false (sprintf "/Reference/%s/%i" colId v) next ctx
    | Error _ -> notFoundResult next ctx

let defaultIfNull (req:TaxonPageRequest) =
    match String.IsNullOrEmpty req.Rank with
    | true -> { Page = 1; PageSize = 50; Rank = "Genus"; Lex = "" }
    | false ->
        if req.PageSize = 0 then { req with PageSize = 50}
        else req

let pagedTaxonomyHandler next (ctx:HttpContext) =
    let core = ctx.GetService<CoreMicroservice>()
    ctx.BindQueryString<TaxonPageRequest>()
    |> defaultIfNull
    |> CoreActions.MRC.list
    |> core.Apply
    |> Async.RunSynchronously
    |> (fun x -> printfn "Response was %A" x; x)
    |> renderViewResult HtmlViews.MRC.index next ctx

// let listCollectionsHandler =
//     coreApiAction (CoreActions.Digitise.myCollections)

// let startCollectionHandler next (ctx:HttpContext) =
//     bindJson<StartCollectionRequest> ctx
//     // |> bind validateModel
//     |> Result.bind (Digitise.startNewCollection (currentUserId ctx))
//     |> toApiResult next ctx

// let publishCollectionHandler next (ctx:HttpContext) =
//     let id = ctx.BindQueryString<IdQuery>().Id
//     Digitise.publish (currentUserId ctx) id
//     text "Ok" next ctx

// let addSlideHandler next (ctx:HttpContext) =
//     bindJson<SlideRecordRequest> ctx
//     // |> bind validateModel
//     |> bind Digitise.addSlideRecord
//     |> toApiResult next ctx

// let voidSlideHandler next ctx =
//     bindJson<VoidSlideRequest> ctx
//     |> bind Digitise.voidSlide
//     |> toApiResult next ctx

// let addImageHandler next (ctx:HttpContext) =
//     bindJson<SlideImageRequest> ctx
//     |> Result.bind Digitise.uploadSlideImage
//     |> toApiResult next ctx

// let getCollectionHandler next (ctx:HttpContext) =
//     ctx.BindQueryString<IdQuery>().Id.ToString()
//     |> Digitise.getCollection
//     |> toApiResult next ctx
    
// let getCalibrationsHandler next (ctx:HttpContext) =
//     Calibrations.getMyCalibrations (currentUserId ctx)
//     |> toApiResult next ctx

// let setupMicroscopeHandler next (ctx:HttpContext) =
//     bindJson<AddMicroscopeRequest> ctx
//     |> Result.bind (Calibrations.setupMicroscope (currentUserId ctx))
//     |> toApiResult next ctx

// let calibrateHandler next (ctx:HttpContext) =
//     bindJson<CalibrateRequest> ctx
//     |> Result.bind Calibrations.calibrateMagnification
//     |> toApiResult next ctx

let listGrains = coreAction (CoreActions.UnknownMaterial.list()) HtmlViews.Identify.index

let showGrainDetail id = coreAction (CoreActions.UnknownMaterial.itemDetail id) HtmlViews.Identify.view

// let submitGrainHandler next (ctx:HttpContext) =
//     let core = ctx.GetService<CoreMicroservice>()
//     bindJson<AddUnknownGrainRequest> ctx
//     |> Result.bind (core.Apply(CoreActions.UnknownMaterial.submit (currentUserId ctx)))
//     |> toApiResult next ctx

// let submitIdentificationHandler next (ctx:HttpContext) =
//     let formData =
//         ctx.BindFormAsync<IdentifyGrainRequest>()
//         |> Async.AwaitTask
//         |> Async.RunSynchronously
//     UnknownGrains.identifyUnknownGrain (currentUserId ctx) formData |> ignore
//     redirectTo true (sprintf "/Identify/%A" formData.GrainId) next ctx


let topUnknownGrainsHandler next (ctx:HttpContext) =
    let core = ctx.GetService<CoreMicroservice>()
    core.Apply(CoreActions.UnknownMaterial.mostWanted()) 
    |> Async.RunSynchronously
    |> toApiResult next ctx

let rebuildReadModelHandler next (ctx:HttpContext) =
    let core = ctx.GetService<CoreMicroservice>()
    core.Apply(CoreActions.System.rebuildReadModel ()) 
    |> Async.RunSynchronously
    |> toApiResult next ctx

// let rebuildReadModelHandler next ctx =
//     Admin.rebuildReadModel()
//     text "Done" next ctx

let systemStatsHandler = coreAction(CoreActions.Statistics.system()) HtmlViews.Statistics.view

// let userAdminHandler next ctx =
//     Admin.listUsers()
//     |> renderViewResult HtmlViews.Admin.users next ctx

// let curateIndexHandler next ctx =
//     Curation.listPending()
//     |> renderViewResult HtmlViews.Admin.curate next ctx

// let curateHandler next (ctx:HttpContext) =
//     ctx.BindFormAsync<CurateCollectionRequest>()
//     |> Async.AwaitTask
//     |> Async.RunSynchronously
//     // |> validateModel
//     |> Curation.issueDecision (currentUserId ctx)
//     |> ignore
//     redirectTo true "/Admin/Curate" next ctx

/////////////////////////
/// Routes
/////////////////////////

        // GET >=> route   "/Digitise/Collection"              >=> api (getCurrentUser |> U.Digitise.myCollections)
        // GET >=> routef  "/Digitise/Collection/%s"           (fun col n c -> col |> U.Digitise.getCollection |> apiResult n c)
        // POST >=> route  "/Digitise/Collection/Start"        >=> api (U.Digitise.startNewCollection getCurrentUser)
        // POST >=> routef "/Digitise/Collection/%s/Publish"   (fun col n c -> U.Digitise.publish getCurrentUser col |> apiResult n c)
        // POST >=> routef "/Digitise/Collection/%s/Slide/Add" (fun col n c -> U.Digitise.addSlideRecord c)
        // POST >=> routef "/Digitise/Collection/%s/Slide/%s/Void"     (fun (col,s) n c -> U.Digitise.voidSlide c)
        // POST >=> routef "/Digitise/Collection/%s/Slide/%s/AddImage" (fun (col,s) n c -> U.Digitise.uploadSlideImage)

let webApp : HttpHandler = 

    let account =
        GET >=> choose [
            route  Urls.Account.login                >=> Authentication.login
            route  Urls.Account.register             >=> Authentication.login
            route  Urls.Account.logout               >=> Authentication.logout
        ]

    let api =
        GET >=> choose [
            route   "/backbone/match"           >=> apiResultFromQuery CoreActions.Backbone.tryMatch
            route   "/backbone/trace"           >=> apiResultFromQuery<BackboneSearchRequest,BackboneTaxon list> CoreActions.Backbone.tryTrace
            route   "/backbone/search"          >=> apiResultFromQuery<BackboneSearchRequest,string list> CoreActions.Backbone.search
            route   "/taxon/search"             >=> apiResultFromQuery<TaxonAutocompleteRequest,TaxonAutocompleteItem list> CoreActions.MRC.autocompleteTaxon
            route   "/grain/location"           >=> topUnknownGrainsHandler
        ]

    let masterReferenceCollection =
        GET >=> 
        choose [   
            route   Urls.MasterReference.root   >=> pagedTaxonomyHandler
            routef  "/Taxon/View/%i"            lookupNameFromOldTaxonId
            routef  "/Taxon/ID/%s"              taxonDetailById
            routef  "/Taxon/%s"                 taxonDetail
        ]

    let individualRefCollections =
        GET >=> choose [
            route   ""                          >=> individualCollectionIndex
            routef   "/Grain/%i"                (fun _ -> setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound)
            routef  "/%s/%i"                    (fun (id,v) -> individualCollection id v)
            routef  "/%s"                       slideViewHandler
        ]

    let identify =
        choose [
            // POST >=> route  "/Upload"           >=> Authentication.mustBeLoggedIn >=> submitGrainHandler
            // POST >=> route  "/Identify"         >=> submitIdentificationHandler
            GET  >=> route  ""                  >=> listGrains
            GET  >=> route  "/Upload"           >=> Authentication.mustBeLoggedIn >=> htmlView (HtmlViews.Identify.add 0.)
            GET  >=> routef "/%s"               showGrainDetail
        ]

    let admin =
        choose [
            // GET  >=> route "/Curate"            >=> curateIndexHandler
            // POST >=> route "/Curate"            >=> curateHandler
            // GET  >=> route "/Users"             >=> mustBeAdmin >=> userAdminHandler
            // POST >=> routef "/GrantCuration/%s" grantCurationHandler
            GET  >=> route "/RebuildReadModel"  (*>=> Authentication.mustBeAdmin*) >=> rebuildReadModelHandler
        ]

    // TODO Move maintainance mode into the core service.
    // Make it a ServiceError, which can then be rendered here.

    notInMaintainanceMode >=>
    choose [
        subRoute            "/api/v1"                   api
        // subRoute           "/api/v1/digitise"         digitiseApi
        routeStartsWith     Urls.Account.root           >=> account
        routeStartsWith     Urls.MasterReference.root   >=> masterReferenceCollection
        subRoute            Urls.Collections.root       individualRefCollections
        subRoute            Urls.Identify.root          identify
        subRoute            "/Admin"                    admin
        GET >=> choose [
            route   Urls.home                   >=> coreAction (CoreActions.Statistics.home()) HtmlViews.Home.view
            route   Urls.guide                  >=> docIndexHandler
            routef  "/Guide/%s"                 docSectionHandler
            route   Urls.statistics             >=> systemStatsHandler
            route   Urls.digitise               >=> Authentication.mustBeLoggedIn >=> htmlView DigitiseDashboard.appView
            route   Urls.api                    >=> docSectionHandler "API"
            route   Urls.tools                  >=> htmlView HtmlViews.Tools.main
            route   Urls.cite                   >=> docSectionHandler "Cite"
            route   Urls.terms                  >=> docSectionHandler "Terms"
        ]
        setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound
    ]
