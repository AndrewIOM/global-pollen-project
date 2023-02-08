module GlobalPollenProject.Web.App

open System
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Authentication
open Giraffe
open ReadModels
open Handlers
open Connections
    
let accessDenied = setStatusCode 401 >=> htmlView HtmlViews.StatusPages.denied
let notFound ctx = ctx |> (clearResponse >=> setStatusCode 400 >=> htmlView HtmlViews.StatusPages.notFound)
let mustBeLoggedIn : HttpHandler = requiresAuthentication (redirectTo false Urls.Account.login)
let mustBeAdmin ctx = requiresRole "Admin" accessDenied ctx


/////////////////////////
/// Route HTTP Handlers
/////////////////////////

module Actions =
        
    let login : HttpHandler =
        requiresAuthentication (challenge "OpenIdConnect") >=>
        fun next ctx ->
            let token = ctx.GetTokenAsync("access_token") |> Async.AwaitTask |> Async.RunSynchronously
            ctx.Items.Add("access_token",token)
            redirectTo false Urls.home next ctx

    let logout = signOut "Cookies" >=> redirectTo false "/"
           
    module Docs =

        open Docs
        
        /// Index of markdown files included in Docs folder
        let docIndex : HttpHandler =
            fun next ctx ->
                guideDocuments 
                |> Array.map (fun (meta,html) -> {Html = html; Metadata = meta |> dict; Headings = getSidebarHeadings html}) 
                |> Seq.toList
                |> HtmlViews.Guide.contentsView
                |> renderView next ctx

        let docSection docSection =
            fun next ctx ->
                let r = guideDocuments |> Array.tryFind (fun (n,_) -> n |> List.find (fun (k,_) -> k = "ShortTitle") |> snd = docSection)
                match r with
                | Some (meta,html) -> 
                    {Html = html; Metadata = meta |> dict; Headings = getSidebarHeadings html} 
                    |> HtmlViews.Guide.sectionView
                    |> renderView next ctx
                | None -> notFound next ctx

    module MasterCollection =

        let defaultIfNull (req:TaxonPageRequest) =
            match String.IsNullOrEmpty req.Rank with
            | true -> { Page = 1; PageSize = 50; Rank = "Genus"; Lex = "" }
            | false ->
                if req.PageSize = 0 then { req with PageSize = 50}
                else req

        let pagedTaxonomy : HttpHandler =
            fun next ctx ->
                task {
                    let req = ctx.BindQueryString<TaxonPageRequest>() |> defaultIfNull
                    return!
                        req
                        |> CoreActions.MRC.list
                        |> fun act -> coreAction act (HtmlViews.MRC.index req.Lex req.Rank) next ctx
                }
    
        let slideView (col:Guid) slide : HttpHandler =
            fun next ctx ->
            task {
                let core = ctx.GetService<CoreMicroservice>()
                let! result = CoreActions.MRC.getSlide (col.ToString()) slide |> core.Apply
                return! result |> renderViewResult HtmlViews.Slide.content next ctx
             }

        let taxonDetail family genus species : HttpHandler =
            fun next ctx ->
                let cmd =
                    match genus with
                    | None -> CoreActions.MRC.getFamily family
                    | Some g ->
                        match species with
                        | None -> CoreActions.MRC.getGenus family g
                        | Some sp -> CoreActions.MRC.getSpecies family g sp
                coreAction cmd HtmlViews.Taxon.view next ctx

        let taxonDetailLegacyId id =
            let old = LegacyTaxonomy.taxonLookup |> Seq.tryFind(fun t -> t.OriginalId = id)
            match old with
            | Some t ->
                match t.Rank with
                | "Family" -> redirectTo true (sprintf "/Taxon/%s" t.Family)
                | "Genus" -> redirectTo true (sprintf "/Taxon/%s/%s" t.Family t.Genus)
                | "Species" -> redirectTo true (sprintf "/Taxon/%s/%s/%s" t.Family t.Genus t.Species)
                | _ -> notFound
            | None -> notFound
        
        let taxonDetailById (id:string) : HttpHandler =
            fun next ctx ->
                match Guid.TryParse id with
                | (true,g) ->
                    task {
                        return! coreAction (CoreActions.MRC.getById g) HtmlViews.Taxon.view next ctx
                    }
                | (false,_) -> notFound next ctx

    module Collection =

        let individualCollectionIndex =
            coreAction (CoreActions.IndividualCollections.list {Page = 1; PageSize = 20}) HtmlViews.ReferenceCollections.listView

        /// Display the contents of a specific version of a reference collection 
        let individualCollection (colId:Guid) version =
            coreAction (CoreActions.IndividualCollections.collectionDetail (colId.ToString()) version) HtmlViews.ReferenceCollections.tableView

        /// Display the latest version of a specific reference collection
        let individualCollectionLatest (colId:Guid) next (ctx:HttpContext) =
            let core = ctx.GetService<CoreMicroservice>()
            let latestVer = core.Apply(CoreActions.IndividualCollections.collectionDetailLatest (colId.ToString())) |> Async.RunSynchronously
            match latestVer with
            | Ok v -> redirectTo false (sprintf "/Reference/%s/%i" (colId.ToString()) v) next ctx
            | Error _ -> notFound next ctx

    module Identify =

        let listGrains = coreAction (CoreActions.UnknownMaterial.list()) HtmlViews.Identify.index

        let showGrainDetail id =
            fun next ctx ->
                task {
                    let! user = Profile.getAuthenticatedUser ctx
                    let userId = user |> Option.map(fun u -> u.Id)
                    let baseUrl = sprintf "%s://%s" ctx.Request.Scheme ctx.Request.Host.Value
                    return! (coreAction (CoreActions.UnknownMaterial.itemDetail id) (HtmlViews.Identify.view userId baseUrl ctx.Request.Path.Value)) next ctx
                }

        // TODO Remove RunSynchronously
        let submitGrain : HttpHandler =
             fun next ctx ->
                tryBindJson<AddUnknownGrainRequest> ctx
                |> Result.map(CoreActions.UnknownMaterial.submit)
                |> Result.map(fun r -> Core.coreAction' r ctx |> Async.AwaitTask |> Async.RunSynchronously)
                |> toApiResult next ctx

        let submitIdentification : HttpHandler =
            fun next ctx ->
            task {
                let! model = ctx.BindFormAsync<IdentifyGrainRequest>()
                let! result = Core.coreAction' (CoreActions.UnknownMaterial.identify model) ctx
                return! redirectTo true (sprintf "/Identify/%A" model.GrainId) next ctx
            }

    module Account =
        
        let private optionToStr s =
            match s with
            | Some s -> s
            | None -> ""
        
        // TODO Profile page and changing of public profile
        let profile : HttpHandler =
            fun next ctx ->
                task {
                    let! user = Profile.getAuthenticatedUser ctx
                    let profile = user |> Option.bind(fun u -> u.Profile)
                    match profile with
                    | Some _ ->
                        return! Giraffe.Core.htmlView (HtmlViews.Profile.summary profile ctx profile) next ctx
                    | None ->
                        match user with
                        | Some u ->
                            let req = {
                                Title = u.Title |> optionToStr
                                FirstName = u.Firstname |> optionToStr
                                LastName = u.Lastname |> optionToStr
                                Organisation = u.Organisation |> optionToStr }
                            let! result = Core.coreAction' (CoreActions.User.register req) ctx
                            match result with
                            | Ok _ -> return! redirectTo false Urls.Account.profile next ctx
                            | Error _ ->
                                printfn "Error when processing user profile. Signing out."
                                return! (signOut "Cookies" >=> redirectTo false "/") next ctx
                        | None -> return! (signOut "Cookies" >=> redirectTo false "/") next ctx
                }
    
    module Admin =

        let rebuildReadModel next (ctx:HttpContext) =
            let core = ctx.GetService<CoreMicroservice>()
            core.Apply(CoreActions.System.rebuildReadModel ()) 
            |> Async.RunSynchronously
            |> toApiResult next ctx

        let userAdmin = coreAction (CoreActions.System.listUsers()) HtmlViews.Admin.users
        let curate = coreAction (CoreActions.Curate.listPending()) HtmlViews.Admin.curate
        let grantCuration = formAction CoreActions.Curate.grantCurationRights (fun e -> text (e.ToString())) (fun _ -> userAdmin)
        let curateDecision : HttpHandler = formAction CoreActions.Curate.decide (fun e -> text (e.ToString())) (fun _ -> curate)


    module Stats =

        let systemStats = coreAction(CoreActions.Statistics.system()) HtmlViews.Statistics.view


/////////////////////////
/// Routes
/////////////////////////

let webApp : HttpHandler = 

    let account =
        GET >=> choose [
            route  Urls.Account.login                >=> Actions.login
            route  Urls.Account.register             >=> Actions.login
            route  Urls.Account.logout               >=> mustBeLoggedIn >=> Actions.logout
            route  Urls.Account.profile              >=> mustBeLoggedIn >=> Actions.Account.profile
        ]

    let digitiseApi =
        mustBeLoggedIn >=>
        choose [
            GET  >=> routef  "/collection/%O"            (fun id -> coreApiAction (CoreActions.Digitise.getCollection id))
            GET  >=> route   "/collection/list"          >=> coreApiAction (CoreActions.Digitise.myCollections())
            POST >=> route   "/collection/start"         >=> apiResultFromBody CoreActions.Digitise.startCollection
            GET  >=> routef  "/collection/publish/%O"    (fun id -> coreApiAction (CoreActions.Digitise.publishCollection id))
            POST >=> route   "/collection/slide/add"     >=> apiResultFromBody CoreActions.Digitise.recordSlide
            POST >=> route   "/collection/slide/void"    >=> apiResultFromBody CoreActions.Digitise.voidSlide
            POST >=> route   "/collection/slide/addimage">=> apiResultFromBody CoreActions.Digitise.uploadImage
            GET  >=> route   "/calibration/list"         >=> coreApiAction (CoreActions.User.myCalibrations())
            POST >=> route   "/calibration/use"          >=> apiResultFromBody CoreActions.User.setupMicroscope
            POST >=> route   "/calibration/use/mag"      >=> apiResultFromBody CoreActions.User.calibrateMicroscope
        ]

    let api =
        choose [
            GET >=> route   "/backbone/match"           >=> apiResultFromQuery CoreActions.Backbone.tryMatch
            GET >=> route   "/backbone/trace"           >=> apiResultFromQuery<BackboneSearchRequest,BackboneTaxon list> CoreActions.Backbone.tryTrace
            GET >=> route   "/backbone/search"          >=> apiResultFromQuery<BackboneSearchRequest,string list> CoreActions.Backbone.search
            GET >=> route   "/taxon/search"             >=> apiResultFromQuery<TaxonAutocompleteRequest,TaxonAutocompleteItem list> CoreActions.MRC.autocompleteTaxon
            GET >=> route   "/grain/location"           >=> coreApiAction (CoreActions.UnknownMaterial.mostWanted())
            GET >=> routef  "/neotoma-cache/%i"         (fun i -> coreApiAction (CoreActions.Cache.neotoma i) )
            subRoute        "/digitise"                 digitiseApi
        ]

    let masterReferenceCollection =
        GET >=> 
        choose [   
            route   Urls.MasterReference.root   >=> Actions.MasterCollection.pagedTaxonomy
            routef  "/Taxon/View/%i"            Actions.MasterCollection.taxonDetailLegacyId
            routef  "/Taxon/ID/%s"              Actions.MasterCollection.taxonDetailById
            routef  "/Taxon/%s/%s/%s"           (fun (f,g,s) -> Actions.MasterCollection.taxonDetail f (Some g) (Some s))
            routef  "/Taxon/%s/%s"              (fun (f,g) -> Actions.MasterCollection.taxonDetail f (Some g) None)
            routef  "/Taxon/%s"                 (fun f -> Actions.MasterCollection.taxonDetail f None None)
        ]

    let individualRefCollections =
        GET >=> choose [
            route   ""                          >=> Actions.Collection.individualCollectionIndex
            routef  "/%O"                       Actions.Collection.individualCollectionLatest
            routef  "/%O/%i"                    (fun (id,v) -> Actions.Collection.individualCollection id v)
            routef  "/%O/%s"                    (fun (c,s) -> Actions.MasterCollection.slideView c s)
            routef  "/Grain/%i"                 (fun _ -> setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound)
        ]

    let identify =
        choose [
            POST >=> route  "/Upload"           >=> mustBeLoggedIn >=> Actions.Identify.submitGrain
            POST >=> route  "/Identify"         >=> Actions.Identify.submitIdentification
            GET  >=> route  ""                  >=> Actions.Identify.listGrains
            GET  >=> route  "/Upload"           >=> mustBeLoggedIn >=> htmlView (HtmlViews.Identify.add 0.)
            GET  >=> routef "/%s"               Actions.Identify.showGrainDetail
        ]

    let admin =
        mustBeAdmin >=>
        choose [
            POST >=> route Urls.Admin.curate               >=> Actions.Admin.curateDecision
            POST >=> route Urls.Admin.grantCuration        >=> Actions.Admin.grantCuration
            GET  >=> route Urls.Admin.users                >=> Actions.Admin.userAdmin
            GET  >=> route Urls.Admin.rebuildReadModel     >=> Actions.Admin.rebuildReadModel
            GET  >=> route Urls.Admin.curate               >=> Actions.Admin.curate
        ]

    choose [
        subRoute            "/api/v1"                   api
        routeStartsWith     Urls.Account.root           >=> account
        routeStartsWith     Urls.MasterReference.root   >=> masterReferenceCollection
        subRoute            Urls.Collections.root       individualRefCollections
        subRoute            Urls.Identify.root          identify
        subRoute            Urls.Admin.root             admin
        GET >=> choose [
            route   Urls.home                   >=> coreAction (CoreActions.Statistics.home()) HtmlViews.Home.view
            route   Urls.guide                  >=> Actions.Docs.docIndex
            routef  "/Guide/%s"                 Actions.Docs.docSection
            route   Urls.statistics             >=> Actions.Stats.systemStats
            route   Urls.api                    >=> Actions.Docs.docSection "API"
            route   Urls.tools                  >=> htmlView HtmlViews.Tools.main
            route   Urls.cite                   >=> Actions.Docs.docSection "Cite"
            route   Urls.terms                  >=> Actions.Docs.docSection "Terms"
            route   Urls.digitise               >=> mustBeLoggedIn >=> htmlView DigitiseDashboard.appView
        ]
        setStatusCode 404 >=> htmlView HtmlViews.StatusPages.notFound
    ]
