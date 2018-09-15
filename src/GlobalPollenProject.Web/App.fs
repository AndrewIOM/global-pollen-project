module GlobalPollenProject.Web.App

open System
open System.IO
open Microsoft.AspNetCore.Http

open Account
open Docs
open Giraffe
open GlobalPollenProject.App.UseCases
open GlobalPollenProject.Core.Composition
open Handlers
open ReadModels
open Handlers.LoadProfile

/////////////////////////
/// Helpers
/////////////////////////

let accessDenied = setStatusCode 401 >=> htmlView HtmlViews.StatusPages.denied
let mustBeLoggedIn : HttpHandler = requiresAuthentication (redirectTo false Urls.Account.login)
let mustBeAdmin ctx = requiresRole "Admin" accessDenied ctx

let notFoundResult ctx =
    ctx |> (clearResponse >=> setStatusCode 400 >=> htmlView HtmlViews.StatusPages.notFound)

let maintainanceResult ctx =
    ctx |> (clearResponse >=> setStatusCode 503 >=> htmlView HtmlViews.StatusPages.maintainance)

let notInMaintainanceMode next ctx : HttpFuncResult =
    match inMaintainanceMode with
    | true -> maintainanceResult next ctx
    | false -> next ctx

let prettyJson = Serialisation.serialise

let queryRequestToApiResponse<'a,'b> (appService:'a->Result<'b,ServiceError>) : HttpHandler =
    fun next ctx ->
        ctx
        |> bindQueryString<'a>
        // |> bind validateModel
        |> bind appService
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
        let split = id.Split '/'
        match split.Length with
        | 2 -> 
            let col,slide = split.[0], split.[1] |> Net.WebUtility.UrlDecode
            Taxonomy.getSlide col slide
            |> renderViewResult HtmlViews.ReferenceCollections.slideView next ctx
        | 3 ->
            let col,slide = split.[0], split.[2] |> Net.WebUtility.UrlDecode
            Taxonomy.getSlide col slide
            |> renderViewResult HtmlViews.ReferenceCollections.slideView next ctx
        | _ -> notFoundResult next ctx

let taxonDetail (taxon:string) next ctx =
    let (f,g,s) =
        let split = taxon.Split '/'
        match split.Length with
        | 1 -> split.[0],None,None
        | 2 -> split.[0],Some split.[1],None
        | 3 -> split.[0],Some split.[1],Some split.[2]
        | _ -> "",None,None
    Taxonomy.getByName f g s
    |> renderViewResult HtmlViews.Taxon.view next ctx

let taxonDetailById (id:string) next ctx =
    match Guid.TryParse id with
    | (true,g) ->
        g
        |> Taxonomy.getById
        |> renderViewResult HtmlViews.Taxon.view next ctx
    | (false,_) -> notFoundResult next ctx

let individualCollectionIndex next ctx =
    IndividualReference.list {Page = 1; PageSize = 20}
    |> renderViewResult HtmlViews.ReferenceCollections.listView next ctx

let individualCollection (colId:string) version next ctx =
    IndividualReference.getDetail colId version
    |> renderViewResult HtmlViews.ReferenceCollections.tableView next ctx

let individualCollectionLatest (colId:string) next ctx =
    let latestVer = IndividualReference.getLatestVersion colId
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
    ctx.BindQueryString<TaxonPageRequest>()
    |> defaultIfNull
    |> Taxonomy.list
    |> renderViewResult HtmlViews.MRC.index next ctx

let listCollectionsHandler next ctx =
    Digitise.myCollections (currentUserId ctx)
    |> toApiResult next ctx

let startCollectionHandler next (ctx:HttpContext) =
    bindJson<StartCollectionRequest> ctx
    // |> bind validateModel
    |> Result.bind (Digitise.startNewCollection (currentUserId ctx))
    |> toApiResult next ctx

let publishCollectionHandler next (ctx:HttpContext) =
    let id = ctx.BindQueryString<IdQuery>().Id
    Digitise.publish (currentUserId ctx) id
    text "Ok" next ctx

let addSlideHandler next (ctx:HttpContext) =
    bindJson<SlideRecordRequest> ctx
    // |> bind validateModel
    |> bind Digitise.addSlideRecord
    |> toApiResult next ctx

let voidSlideHandler next ctx =
    bindJson<VoidSlideRequest> ctx
    |> bind Digitise.voidSlide
    |> toApiResult next ctx

let addImageHandler next (ctx:HttpContext) =
    bindJson<SlideImageRequest> ctx
    |> Result.bind Digitise.uploadSlideImage
    |> toApiResult next ctx

let getCollectionHandler next (ctx:HttpContext) =
    ctx.BindQueryString<IdQuery>().Id.ToString()
    |> Digitise.getCollection
    |> toApiResult next ctx
    
let getCalibrationsHandler next (ctx:HttpContext) =
    Calibrations.getMyCalibrations (currentUserId ctx)
    |> toApiResult next ctx

let setupMicroscopeHandler next (ctx:HttpContext) =
    bindJson<AddMicroscopeRequest> ctx
    |> bind (Calibrations.setupMicroscope (currentUserId ctx))
    |> toApiResult next ctx

let calibrateHandler next (ctx:HttpContext) =
    bindJson<CalibrateRequest> ctx
    |> bind Calibrations.calibrateMagnification
    |> toApiResult next ctx

let listGrains next ctx =
    UnknownGrains.listUnknownGrains()
    |> renderViewResult HtmlViews.Identify.index next ctx

let showGrainDetail id next ctx =
    UnknownGrains.getDetail id
    |> renderViewResult HtmlViews.Identify.view next ctx

let submitGrainHandler next (ctx:HttpContext) =
    bindJson<AddUnknownGrainRequest> ctx
    >>= UnknownGrains.submitUnknownGrain (currentUserId ctx)
    |> toApiResult next ctx

let submitIdentificationHandler next (ctx:HttpContext) =
    let formData =
        ctx.BindFormAsync<IdentifyGrainRequest>()
        |> Async.AwaitTask
        |> Async.RunSynchronously
    UnknownGrains.identifyUnknownGrain (currentUserId ctx) formData |> ignore
    redirectTo true (sprintf "/Identify/%A" formData.GrainId) next ctx


let homeHandler next ctx =
    Statistic.getHomeStatistics()
    |> renderViewResult HtmlViews.Home.view next ctx

let topUnknownGrainsHandler next (ctx:HttpContext) =
    UnknownGrains.getTopScoringUnknownGrains()
    |> toApiResult next ctx

let rebuildReadModelHandler next ctx =
    Admin.rebuildReadModel()
    text "Done" next ctx

let systemStatsHandler next ctx =
    Statistic.getSystemStats()
    |> renderViewResult HtmlViews.Statistics.view next ctx

let userAdminHandler next ctx =
    Admin.listUsers()
    |> renderViewResult HtmlViews.Admin.users next ctx

let curateIndexHandler next ctx =
    Curation.listPending()
    |> renderViewResult HtmlViews.Admin.curate next ctx

let curateHandler next (ctx:HttpContext) =
    ctx.BindFormAsync<CurateCollectionRequest>()
    |> Async.AwaitTask
    |> Async.RunSynchronously
    // |> validateModel
    |> Curation.issueDecision (currentUserId ctx)
    |> ignore
    redirectTo true "/Admin/Curate" next ctx


/////////////////////////
/// Routes
/////////////////////////

let webApp : HttpHandler =
    let publicApi =
        GET >=>
        choose [
            // route   "/backbone/match"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryMatch
            route   "/backbone/trace"           >=> queryRequestToApiResponse<BackboneSearchRequest,BackboneTaxon list> Backbone.tryTrace
            route   "/backbone/search"          >=> queryRequestToApiResponse<BackboneSearchRequest,string list> Backbone.searchNames
            route   "/taxon/search"             >=> queryRequestToApiResponse<TaxonAutocompleteRequest,TaxonAutocompleteItem list> Taxonomy.autocomplete
            route   "/grain/location"           >=> topUnknownGrainsHandler
        ]

    let digitiseApi =
        mustBeLoggedIn >=>
        choose [
            route   "/collection"               >=> getCollectionHandler
            route   "/collection/list"          >=> listCollectionsHandler
            route   "/collection/start"         >=> startCollectionHandler
            route   "/collection/publish"       >=> publishCollectionHandler
            route   "/collection/slide/add"     >=> addSlideHandler
            route   "/collection/slide/void"    >=> voidSlideHandler
            route   "/collection/slide/addimage">=> addImageHandler
            route   "/calibration/list"         >=> getCalibrationsHandler
            route   "/calibration/use"          >=> setupMicroscopeHandler
            route   "/calibration/use/mag"      >=> calibrateHandler
        ]

    let accountManagement =
        choose [
            POST >=> route  Urls.Account.login                >=> loginHandler
            POST >=> route  Urls.Account.externalLogin        >=> externalLoginHandler
            POST >=> route  Urls.Account.externalLoginConf    >=> externalLoginConfirmation
            POST >=> route  Urls.Account.register             >=> registerHandler
            POST >=> route  Urls.Account.logout               >=> mustBeLoggedIn >=> logoutHandler
            POST >=> route  Urls.Account.forgotPassword       >=> mustBeLoggedIn >=> forgotPasswordHandler
            POST >=> route  Urls.Account.resetPassword        >=> mustBeLoggedIn >=> resetPasswordHandler
            GET  >=> route  Urls.Account.login                >=> htmlView (HtmlViews.Account.login [] Requests.Empty.login)
            GET  >=> route  Urls.Account.register             >=> htmlView (HtmlViews.Account.register [] Requests.Empty.newAppUserRequest) 
            GET  >=> route  Urls.Account.resetPassword        >=> resetPasswordView
            GET  >=> route  Urls.Account.resetPasswordConf    >=> htmlView (HtmlViews.Account.resetPasswordConfirmation)
            GET  >=> route  Urls.Account.forgotPassword       >=> htmlView (HtmlViews.Account.forgotPassword Requests.Empty.forgotPassword)
            GET  >=> route  Urls.Account.confirmEmail         >=> confirmEmailHandler
            GET  >=> route  Urls.Account.externalLoginCallbk  >=> externalLoginCallback Urls.home

            GET  >=> route  "/Manage"                       >=> Manage.index
            // POST >=> route  "/Manage/Profile"               >=> Manage.profile
            // GET  >=> route  "/Manage/Profile"               >=> htmlView HtmlViews.Manage.changePassword //renderView "Manage/ChangePublicProfile" None
            POST >=> route  "/Manage/LinkLogin"             >=> Manage.linkLogin
            GET  >=> route  "/Manage/LinkLoginCallback"     >=> Manage.linkLoginCallback
            GET  >=> route  "/Manage/ManageLogins"          >=> Manage.manageLogins
            POST >=> route  "/Manage/SetPassword"           >=> Manage.setPassword
            GET  >=> route  "/Manage/SetPassword"           >=> htmlView (HtmlViews.Manage.setPassword [] 2.)
            POST >=> route  "/Manage/ChangePassword"        >=> Manage.changePassword
            GET  >=> route  "/Manage/ChangePassword"        >=> htmlView (HtmlViews.Manage.changePassword [] 2.)
            POST >=> route  "/Manage/RemoveLogin"           >=> Manage.removeLogin
            GET  >=> route  "/Manage/RemoveLogin"           >=> Manage.removeLoginView
        ]

    let masterReferenceCollection =
        GET >=> 
        choose [   
            route   Urls.MRC.root               >=> pagedTaxonomyHandler
            routef  "/Taxon/View/%i"            lookupNameFromOldTaxonId
            routef  "/Taxon/ID/%s"              taxonDetailById
            routef  "/Taxon/%s"                 taxonDetail
        ]

    let individualRefCollections =
        GET >=>
        choose [
            route   ""                          >=> individualCollectionIndex
            routef   "/Grain/%i"                (fun _ -> setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound)
            routef  "/%s/%i"                    (fun (id,v) -> individualCollection id v)
            routef  "/%s"                       slideViewHandler
        ]

    let identify =
        choose [
            POST >=> route  "/Upload"           >=> submitGrainHandler
            POST >=> route  "/Identify"         >=> submitIdentificationHandler
            GET  >=> route  ""                  >=> listGrains
            GET  >=> route  "/Upload"           >=> mustBeLoggedIn >=> htmlView (HtmlViews.Identify.add 0.)
            GET  >=> routef "/%s"               showGrainDetail
        ]

    let admin =
        choose [
            GET  >=> route "/Curate"            >=> curateIndexHandler
            POST >=> route "/Curate"            >=> curateHandler
            GET  >=> route "/Users"             >=> mustBeAdmin >=> userAdminHandler
            POST >=> routef "/GrantCuration/%s" grantCurationHandler
            GET  >=> route "/RebuildReadModel"  >=> mustBeAdmin >=> rebuildReadModelHandler
        ]

    // Main router
    notInMaintainanceMode >=>
    choose [
        subRoute            "/api/v1"            publicApi
        subRoute            "/api/v1/digitise"   digitiseApi
        routeStartsWith     Urls.Account.root    >=> accountManagement
        routeStartsWith     "/Taxon"             >=> masterReferenceCollection
        subRoute            "/Reference"         individualRefCollections
        subRoute            "/Identify"          identify
        subRoute            "/Admin"             admin
        GET >=> 
        choose [
            route   Urls.home                   >=> homeHandler
            route   Urls.guide                  >=> docIndexHandler
            routef  "/Guide/%s"                 docSectionHandler
            route   Urls.statistics             >=> systemStatsHandler
            route   Urls.digitise               >=> mustBeLoggedIn >=> htmlView DigitiseDashboard.appView
            route   Urls.api                    >=> docSectionHandler "API"
            route   Urls.tools                  >=> htmlView HtmlViews.Tools.main
            route   Urls.cite                   >=> docSectionHandler "Cite"
            route   Urls.terms                  >=> docSectionHandler "Terms"
        ]
        setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound
    ]
