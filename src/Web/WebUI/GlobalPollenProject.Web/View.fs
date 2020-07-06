module GlobalPollenProject.Web.HtmlViews

open System
open Giraffe.GiraffeViewEngine 
open ReadModels

type Script = string
type ActiveComponent = { View: XmlNode; Scripts: Script list }
type PageLink = { Name: string; Url: string }

let _data attr value = KeyValue("data-" + attr, value)
let _aria attr value = KeyValue("aria-" + attr, value)
let _integrity v = KeyValue("integrity", v)
let _role value = KeyValue("role",value)
let _on e value = KeyValue("on" + e,value)

let jsBundle = "/scripts/main.bundle.js"

module Settings =
    
    open Microsoft.Extensions.Configuration
    
    let appSettings =
        ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build()

    let getAppSetting name =
        match String.IsNullOrEmpty appSettings.[name] with
        | true -> invalidOp "Appsetting is missing: " + name
        | false -> appSettings.[name]
    
    let googleApiKey = getAppSetting "GoogleApiKey"

[<AutoOpen>]
module Grid =

    type ColumnBreak =
    | Small
    | Medium
    | Large

    let container = div [ _class "container" ]

    let row = div [ _class "row" ]

    let column size width = 
        match size with
        | Small -> div [ _class (sprintf "col-sm-%i" width) ]
        | Medium -> div [ _class (sprintf "col-md-%i" width) ]
        | Large -> div [ _class (sprintf "col-lg-%i" width) ]


module Icons =

    let fontawesome icon = i [ sprintf "fa fa-%s" icon |> _class ] []


module MvcAttributeValidation =

    open Microsoft.AspNetCore.Mvc.DataAnnotations
    open Microsoft.AspNetCore.Mvc.ModelBinding
    open Microsoft.AspNetCore.Mvc.ModelBinding.Validation
    open Microsoft.AspNetCore.Mvc.ViewFeatures
    open Microsoft.AspNetCore.Http
    open Giraffe
    open System.ComponentModel.DataAnnotations
    open System.Collections.Generic

    let clientSideInputValidationTags' (p:Type) pName attr (ctx:HttpContext) =
        let provider = ctx.GetService<IModelMetadataProvider>();
        let metadata = provider.GetMetadataForProperty(p, pName)
        let actionContext = Microsoft.AspNetCore.Mvc.ActionContext()
        let context = ClientModelValidationContext(actionContext, metadata, provider, AttributeDictionary())
        let adapter = ValidationAttributeAdapterProvider()
        let a = adapter.GetAttributeAdapter(attr,null)
        match isNull a with
        | true -> ()
        | false -> a.AddValidation context
        context.Attributes

    let clientSideInputValidationTags p pName ctx attr =
        clientSideInputValidationTags' p pName attr ctx
        :> seq<_>
        |> Seq.map (fun i -> KeyValue(i.Key,i.Value))

    let validateModel u (ctx:HttpContext) =
        let provider = ctx.GetService<IModelMetadataProvider>()
        let context = ValidationContext(provider, null)
        let validationResults = List<ValidationResult>()
        Validator.TryValidateObject(u, context, validationResults, true) |> ignore
        validationResults
        |> Seq.map(fun e -> { Property = e.MemberNames |> Seq.head; Errors = [ e.ErrorMessage ] } )
        |> Seq.toList

module TagHelpers =

    open Microsoft.FSharp.Quotations.Patterns
    open System.ComponentModel.DataAnnotations

    let propertyName expr =
        match expr with 
        | PropertyGet(_,pi,_) -> 
            let nameAttribute = pi.CustomAttributes |> Seq.tryFind (fun p -> p.AttributeType = typeof<DisplayAttribute>)
            match nameAttribute with
            | Some a -> 
                match a.NamedArguments |> Seq.tryFind(fun x -> x.MemberName = "Name") with
                | Some n -> n.TypedValue.Value.ToString()
                | None -> pi.Name
            | None -> pi.Name
        | _ -> ""

    let validationFor expr ctx =
        match expr with 
        | PropertyGet(_,pi,_) -> 
            pi.GetCustomAttributes(typeof<ValidationAttribute>,false)
            |> Seq.choose (fun t -> match t with | :? ValidationAttribute as v -> Some v | _ -> None)
            |> Seq.collect (MvcAttributeValidation.clientSideInputValidationTags pi.DeclaringType pi.Name ctx)
            |> Seq.toList
        | _ -> []

module Forms =

    open TagHelpers
    open Microsoft.FSharp.Quotations

    let formField' fieldName validationAttributes = 
        div [ _class "form-group row" ] [
            label [ _class "col-sm-2 col-form-label"; _for fieldName ] [ encodedText fieldName ]
            Grid.column Small 10 [
                input (List.concat [[ _name fieldName; _class "form-control"; ]; validationAttributes ])
                span [ _class "text-danger field-validation-valid"; _data "valmsg-for" fieldName; _data "valmsg-replace" "true" ] []
            ]
        ]

    let formField (e:Expr) ctx =
        let name = propertyName e
        let validationAttributes = validationFor e ctx
        formField' name validationAttributes

    let formGroup (e:Expr) helpText ctx =
        let name = propertyName e
        let validationAttributes = validationFor e ctx
        div [ _class "form-group" ] [
            label [] [ encodedText name ]
            input (List.concat [[ _id name; _name name; _class "form-control"; ]; validationAttributes ])
            small [ _id "name-help" ] [ encodedText helpText ] 
        ]

    let validationSummary (additionalErrors: ValidationError list) vm ctx =
        let errorHtml =
            MvcAttributeValidation.validateModel vm ctx
            |> List.append additionalErrors
            |> List.collect (fun e -> e.Errors)
            |> List.map encodedText
        div [] errorHtml

    let submit =
        div [ _class "form-group" ] [
            div [ _class "col-md-offset-2 col-md-10" ] [
                button [ _type "submit"; _class "btn btn-default" ] [ encodedText "Submit" ]
            ]
        ]


module Layout = 

    let loginMenu (profile: ReadModels.PublicProfile option) =
        match profile with
        | Some p ->
            ul [ _class "navbar-nav" ] [
                li [ _class "navbar-dropdown" ] [
                    a [ _class "nav-link dropdown-toggle"; _href "#"; _id "navbarDropdownMenuLink" ] [
                        span [] [ encodedText (sprintf "%s %s" p.FirstName p.LastName) ]
                    ]
                    div [ _class "dropdown-menu" ] [
                        a [ _href "/Profile" ] [ str "Your profile" ]
                        a [ _href "/Account/Logout" ] [ str "Log out" ]
                    ]
                ]
            ]
        | None ->
            ul [ _class "nav navbar-nav" ] [
                li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.Account.login ] [ encodedText "Register or Login" ] ]
            ]

    let navigationBar profile =
        nav [ _class "navbar navbar-expand-lg navbar-toggleable-md navbar-light bg-faded fixed-top"] [
            Grid.container [
                button [ _class "navbar-toggler navbar-toggler-right" ] [
                    span [ _class "navbar-toggler-icon" ] []
                ]
                a [ _class "navbar-brand"; _href "/" ] [
                    span [ _class "top" ] [ encodedText "The Global" ]
                    span [ _class "bottom" ] [ encodedText "Pollen Project" ]
                ]
                div [ _class "collapse navbar-collapse"; _id "master-navbar" ] [
                    ul [ _class "navbar-nav mr-auto" ] [
                        li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.guide ] [ encodedText "Guide" ] ]
                        li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.referenceCollection ] [ encodedText "Reference Collection" ] ]
                        li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.identify ] [ encodedText "Identify" ] ]
                        li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.individualCollections ] [ encodedText "Individual Collections" ] ]
                        li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.statistics ] [ encodedText "Stats" ] ]
                    ]
                    loginMenu profile
                ]
            ] 
        ]

    let footer =
        footer [] [
            Grid.container [
                div [ _class "row" ] [
                    div [ _class "col-md-3" ] [
                        h4 [] [ encodedText "Information" ]
                        ul [] [
                            li [] [ a [ _href Urls.guide ] [ encodedText "About"] ]
                            li [] [ a [ _href Urls.api ] [ encodedText "Public API"] ]
                            li [] [ a [ _href Urls.terms ] [ encodedText "Terms and Licensing"] ]
                            li [] [ a [ _href Urls.cite ] [ encodedText "How to Cite"] ]
                        ]
                    ]
                    div [ _class "col-md-5"; _style "padding-right:4em" ] [
                        h4 [] [ encodedText "Data" ]
                        ul [] [
                            li [] [ a [ _href Urls.referenceCollection ] [ encodedText "Master Reference Collection"] ]
                            li [] [ a [ _href Urls.individualCollections ] [ encodedText "Individual Reference Collections"] ]
                            li [] [ a [ _href Urls.identify ] [ encodedText "Unidentified Specimens"] ]
                        ]
                        h4 [] [ encodedText "Tools" ]
                        ul [] [
                            li [] [ a [ _href Urls.digitise ] [ encodedText "Online Digitisation Tools"; span [ _style "font-weight: normal;margin-left: 0.5em;"; _class "badge badge-info" ] [ encodedText "Preview" ] ] ]
                            li [] [ a [ _href Urls.tools ] [ encodedText "Botanical Name Tracer"] ]
                        ]
                    ]
                    div [ _class "col-md-4 footer-images"] [
                        h4 [] [ encodedText "Funding" ]
                        p [] [ encodedText "The Global Pollen Project is funded via the Natural Environment Research Council of the United Kingdom, and the Oxford Long-Term Ecology Laboratory."]
                        a [ _href "https://oxlel.zoo.ox.ac.uk"; _target "_blank"] [
                            img [ _src "/images/oxlellogo.png"; _alt "Long Term Ecology Laboratory" ]
                        ]
                        img [ _src "/images/oxford-logo.png"; _alt "University of Oxford" ]
                    ]
                ]
                div [ _class "row" ] [
                    hr []
                    div [ _class "col-md-12" ] [
                        p [ _style "text-align:center;" ] [ 
                            encodedText "The Global Pollen Project 2.0"
                            span [ _class "hide-xs" ] [ encodedText " · " ]
                            encodedText "Code available at "
                            a [ _href "https://github.com/AndrewIOM/gpp-cqrs" ] [ encodedText "GitHub" ] ]
                    ]
                ]
            ]
        ]

    let headSection pageTitle =
        head [] [
            meta [_charset "utf-8"]
            meta [_name "viewport"; _content "width=device-width, initial-scale=1.0" ]
            title [] [ pageTitle |> sprintf "%s - Global Pollen Project" |> encodedText ]
            link [ _rel "stylesheet"; _href "https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0/css/bootstrap.min.css" ]
            link [ _rel "stylesheet"; _href "https://use.fontawesome.com/releases/v5.0.10/css/all.css" ]
            link [ _rel "stylesheet"; _href "/css/styles.css" ]
        ]

    let toScriptTag (s:Script) =
        script [ _src s ] []

    let toScriptTags (scripts:Script list) =
        scripts |> List.distinct |> List.map toScriptTag

    let headerBar title subtitle =
        header [] [
            Grid.container [
                h1 [] [ encodedText title ]
                p [] [ encodedText subtitle ]
            ]
        ]

    let baseScripts = [
        "https://code.jquery.com/jquery-3.2.1.min.js"
        jsBundle ]

    let master (scripts: Script list) content profile =
        html [] [
            headSection "Global Pollen Project"
            body [] (
                List.concat [
                    [ navigationBar profile
                      div [ _class "main-content" ] content
                      footer ]
                    ((List.concat [baseScripts; scripts]) |> toScriptTags) ] )
        ]

    let standard scripts title subtitle content =
        master scripts ( headerBar title subtitle :: [ Grid.container content ])


module Components =

    let breadcrumb (parentPages:PageLink list) currentPageName =
        let breadcrumbItem name link = li [ _class "breadcrumb-item" ] [ a [ _href link ] [ encodedText name ] ]
        let home = breadcrumbItem "Home" Urls.home
        let parents = parentPages |> List.map (fun p -> breadcrumbItem p.Name p.Url)
        let current = li [ _class "breadcrumb-item" ] [ a [] [ encodedText currentPageName ] ]
        Grid.row [
            Grid.column Medium 12 [
                ol [ _class "breadcrumb" ] (List.concat [[home]; parents; [current]] )
            ]
        ]

    let itemGrid f items =
        div [ _class "panel panel-white" ] [
            div [ _class "panel-heading" ] [
                i [ _class "fa fa-film" ] []
                encodedText "Individual Grains"
            ]
            div [ _class "panel-body" ] [
                ul [ _class "grain-grid columns-8" ] (items |> List.map f)
            ]
        ]

    let pagination currentPage itemsPerPage totalItems totalPages linkBase =
        nav [] [
            ul [ _class "pagination" ] (
                // First button
                [ 1 .. totalPages ] |> List.map (fun i ->
                    li [ _class "page-item" ] [ 
                        a [ _class "page-link"; _href (sprintf "%s=%i" linkBase i) ] [encodedText (sprintf "%i" i) ]
                    ] )
                // Last button
            )
        ]

    let percentCircle (percent:float) =
        div [ _class <| sprintf "c100 %i" (int percent) ] [
            span [] [ encodedText <| percent.ToString("0.00") ]
            div [ _class "slice" ] [
                div [ _class "bar" ] []
                div [ _class "fill" ] []
            ]
        ]


module Home =

    let autocomplete =
        let view =
            form [ _action "/Taxon"; _method "get"; _class "form-inline search-big" ] [
                input [ _hidden; _name "rank"; _value "Genus" ]
                input [ _name "lex"; _title "Search by latin name"; _id "ref-collection-search"; _class "form-control form-control-lg"; _type "search"; _placeholder "Search by Latin Name"; _autocomplete "off" ]
                div [ _class "dropdown-menu"; _id "suggestList"; _style "display:none" ] []
                button [ _type "submit"; _title "Search"; _class "btn btn-primary btn-lg" ] [ encodedText "Go" ] 
            ]
        { View = view
          Scripts = [ jsBundle ]}

    let heading searchField =
        header [ _class "homepage-header" ] [
            div [ _class "container" ] [
                h1 [] [ encodedText "The Global Pollen Project" ]
                p [] [ encodedText "The Open Platform for Pollen Identification" ]
                searchField
            ]
        ]

    let jumboIcon link imageUrl imageName title summary =
        Grid.column Small 4 [
            img [ _class "img-circle img-responsive home-tri-image"; _src imageUrl; _alt imageName ]
            h3 [] [ 
                a [ _href link ] [ encodedText title ] ]
            p [] [ 
                encodedText summary
                a [ _href link ] [ encodedText "Learn more" ]
            ]
        ]

    let tripleIcons =
        section [ _class "homepage-section" ] [
            Grid.container [
                div [ _class "row justify-content-center" ] [
                    jumboIcon Urls.referenceCollection "/images/pollen2.jpg" "Compositae pollen" "A Global Pollen and Spore Reference Set" "Browse the dynamic and ever expanding reference collection of the Global Pollen Project. Individual reference collections and identified material are combined into an always up-to-date botanical taxonomy."
                    jumboIcon Urls.identify "/images/pollen1.jpg" "Pollen grain" "Crowdsourced Taxonomic Identification" "If you are having trouble identifying a pollen grain to family, genus or species level, submit your grain to the Global Pollen Project. Help others by providing identifications and earn points on the leader board for your lab group or Institution."
                    jumboIcon Urls.guide "/images/pollen3.jpg" "Pollen grain" "Robust Digitisiation Tools" "Our web-based digitisation tool enables anyone to digitise and share reference material, following a scientifically-robust protocol."
                ]
            ]
        ]

    let unidentifiedGrainMap =
        let view =
            section [ _class "homepage-section map-section" ] [
                Grid.container [
                    div [ _class "row justify-content-center" ] [
                        div [ _class "col-md-4" ] [
                            p [] [
                                span [ _id "unidentified-count" ] [ encodedText "?" ]
                                encodedText "unidentified grains"
                            ]
                        ]
                        div [ _class "col-md-6" ] [
                            div [ _id "locations-map" ] []
                        ]
                    ]
                ]
            ]
        { View = view; 
          Scripts = [ jsBundle ] }

    let stats (vm:HomeStatsViewModel) =
        section [ _class "homepage-section section-bottomline" ] [
            Grid.container [
                div [ _class "row justify-content-center" ] [
                    div [ _class "col-md-3 bottom-margin" ] [ 
                        span [ _class "big-number" ] [ encodedText <| vm.Species.ToString("#,##0") ]
                        encodedText "Species" ]
                    div [ _class "col-md-3 bottom-margin" ] [ 
                        span [ _class "big-number" ] [ encodedText <| vm.DigitisedSlides.ToString("#,##0") ]
                        encodedText "Digitised Reference Slides" ]
                    div [ _class "col-md-3 bottom-margin" ] [ 
                        span [ _class "big-number" ] [ encodedText <| vm.Species.ToString("#,##0") ]
                        encodedText "Individual Grains and Spores" ]
                ]
            ]
        ]

    let view model = 
        [
            heading autocomplete.View
            stats model
            tripleIcons
            unidentifiedGrainMap.View
        ] |> Layout.master (List.append autocomplete.Scripts unidentifiedGrainMap.Scripts)


module Slide =

    let imageViewer =
        div [ _id "viewer-container" ] []

    let citation firstName lastName =
        let compiledCitation = sprintf "%s %s" firstName lastName
        div [ _class "card card-inverse card-primary crop-panel mb-3" ] [
            div [ _class "card-block" ] [
                h4 [ _class "card-title" ] [ encodedText "Citation" ]
                p [] [ encodedText compiledCitation ]
            ]
        ]

    let origin (model:Responses.SlidePageViewModel) =
        let name, description =
            match model.Slide.IdMethod with
            | "Field" -> "Cool", "Cool"
            | "LivingCollection" -> "Cool", "Cool"
            | "Voucher" -> "Cool", "Cool"
            | _ -> "Cool", "Cool"
       
        div [ _class "card" ] [
            div [ _class "card-header" ] [ encodedText "Origin" ]
            div [ _class "card-block" ] [
                h4 [] [ encodedText name ]
                small [ _class "text-muted" ] [ encodedText description ]
            ]
        ]

    let content model =
        [ 
            Components.breadcrumb [ { Name = "Cool1" ; Url = "Cool1" } ] "Slide Name"
            Grid.row [
                Grid.column Medium 6 [
                    imageViewer
                    citation model.Slide.CollectorName model.Slide.CollectorName
                ]
                Grid.column Medium 6 [
                    origin model
                ]
            ]
        ]

module Taxon =

    let distributionMap gbifId neotomaId =
        let view = 
            div [ _class "panel panel-black" ] [
                div [ _class "panel-heading" ] [
                    Icons.fontawesome "globe"
                    encodedText "Distribution"
                    div [ _class "btn-group"; _data "toggle" "buttons"; _style "float:right" ] [
                        label [ _class "btn btn-primary btn-sm active" ] [
                            input [ _type "radio"; _name "distribution"; _value "recent"; _autocomplete "off"; _checked ]
                            encodedText "Recent"
                        ]
                        label [ _class "btn btn-primary btn-sm" ] [
                            input [ _type "radio"; _name "distribution"; _value "palaeo"; _autocomplete "off"]
                            encodedText "Palaeo"
                        ]
                    ]
                ]
                div [ _class "panel-body" ] [
                    input [ _hidden; _id "NeotomaId"; _value (sprintf "%i" neotomaId) ]
                    input [ _hidden; _id "NeotomaId"; _value (sprintf "%i" gbifId) ]

                    div [ _id "modern" ] [
                        div [ _id "map"; _style "height:300px" ] []
                    ]

                    div [ _id "palaeo" ] [
                        div [ _id "palaeo-loading" ] [ encodedText "Fetching from Neotoma..." ]
                        div [ _id "neotoma-map-unavailable"; _style "display:none" ] [ encodedText "Past distributions unavailable from Neotoma." ]
                        span [ _class "timespan" ] [ encodedText "Showing NeotomaDB occurrences from x to x years before present."] 
                        div [ _id "neotoma-map" ] []
                        div [ _id "range" ] []
                    ]
                ]
            ]
        let scripts = 
            [ "/Scripts" ]
        { View = view; Scripts = scripts }


    let descriptionCard (eolCache:EncyclopediaOfLifeCache) =
        div [ _class "card" ] [
            div [ _class "card-fixed-height-image" ] [
                img [ _src eolCache.PhotoUrl; _alt (sprintf "") ] 
            ]
        ]

    let slideListPanel (slides: ReadModels.SlideSummary list) =
        div [ _class "panel panel-white" ] [
            div [ _class "panel-heading" ] [
                Icons.fontawesome "film"
                encodedText "Digitised Reference Slides"
            ]
            div [ _class "panel-body" ] [
                    encodedText (sprintf "We currently have %i digitsed slides." (slides |> List.length))
                    ul [ _class "grain-grid columns-8" ] (slides |> List.map (fun s -> 
                        li [] [
                            a [ _href (sprintf "/Reference/%A/%s" s.ColId s.SlideId) ] [ 
                                div [ _class "img-container" ] [ img [ _src s.Thumbnail ] ] 
                            ] ] ))
            ]
        ]

    let view (vm:TaxonDetail) =
        let title = sprintf "%s (%s) - Master Reference Collection" vm.LatinName vm.Rank
        let distributionMap = distributionMap vm.GbifId vm.NeotomaId
        [ 
            link [ _rel "stylesheet"; _href "/lib/nouislider/distribute/nouislider.min.css"]
            link [ _rel "stylesheet"; _href "/lib/leaflet/dist/leaflet.css"]
            Components.breadcrumb [] "Cool Page"
            Grid.row [
                Grid.column Medium 6 [
                    slideListPanel vm.Slides
                ]
                Grid.column Medium 6 [
                    distributionMap.View
                    descriptionCard vm.EolCache
                ]
            ]
        ] |> Layout.standard distributionMap.Scripts title ""


module Guide =

    open Docs
    open System.Collections.Generic

    let sectionCard (section:DocViewModel) =
        Grid.column Medium 4 [
            div [ _class "card" ] [
                a [ _class "card-block"; _href (sprintf "/Guide/%s" section.Metadata.["ShortTitle"]) ] [
                    Icons.fontawesome section.Metadata.["Icon"]
                    h4 [ _class "card-title" ] [ encodedText section.Metadata.["Title"] ]
                    p [ _class "card-text" ] [ small [ _class "text-muted" ] [ encodedText section.Metadata.["Intro"] ] ]
                ]
            ]
        ]

    let sidebar vm =
        div [ _class "sticky-sidebar" ] [
            a [ _href "/Guide"; _class "btn btn-primary btn-block" ] [
                Icons.fontawesome "book"
                encodedText " Guide Contents"
            ]
            hr []
            label [] [ encodedText vm.Metadata.["Title"] ]
            nav [ _id "sidebar"; _class "nav flex-column" ] (
                vm.Headings |> List.map (fun h -> a [ _class "nav-link"; _href ("#" + h.LinkId) ] [ encodedText h.Name ]))
        ]

    let metadata (metadata:IDictionary<string,string>) =
        let authorUrl = metadata.["Author"].Replace(" ", "-").ToLower() + ".jpg"
        div [ _class "guide-text";(*  _dataSpy "scroll"; _dataTarget "#sidebar"; _dataOffset "20" *) ] [
            h2 [] [ encodedText metadata.["Title"] ]
            div [ _class "metadata" ] [
                img [ _src ("/images/guide/authors/" + authorUrl) ]
                span [] [ encodedText (sprintf "By %s, %s" metadata.["Author"] metadata.["Affiliation"])]
                span [ _class "hide-xs divider" ] [ encodedText "·" ]
                span [] [
                    Icons.fontawesome "calendar"
                    encodedText (" " + metadata.["Date"])
                ]
            ]
        ]

    let contentsView sections = 
        [
            div [ _class "row justify-content-md-center" ] [
                Grid.column Medium 9 [
                    div [ _class "row guide-sections-card-deck" ] (sections |> List.map sectionCard)
                ]
            ]
        ] |> Layout.standard [] "Guide" "Find out about the project and how it works"

    let sectionView content =
        [
            Grid.row [
                Grid.column Medium 3 [ sidebar content ]
                Grid.column Medium 9 [ metadata content.Metadata; rawText content.Html  ]
            ]
        ] |> Layout.standard [] "Guide" ""


module MRC =

    open System

    let taxonCard (summary:TaxonSummary) =
        let taxonLink =
            match summary.Rank with
            | "Family" -> "/Taxon/" + summary.Family
            | "Genus" -> "/Taxon/" + summary.Family + "/" + summary.Genus
            | "Species" -> "/Taxon/" + summary.Family + "/" + summary.Genus + "/" + summary.Species
            | _ -> "/NotFound"

        div [ _class "taxon-list-item" ] [
            div [ _class "img-container" ] [
                match String.IsNullOrEmpty summary.ThumbnailUrl with
                | false -> yield img [ _src summary.ThumbnailUrl; _alt (sprintf "%s pollen" summary.LatinName) ]
                | true -> ()
            ]
            div [ _class "taxon-details" ] [
                a [ _href taxonLink;  _class "taxon-name" ] [ encodedText summary.LatinName ]
                ul [ _class "list-inline" ] [
                    li [ _class "list-inline-item" ] [ Icons.fontawesome "object-ungroup"; encodedText (sprintf "%i" summary.GrainCount) ]
                    li [ _class "list-inline-item" ] [ Icons.fontawesome "object-group"; encodedText (sprintf "%i" summary.SlideCount) ]
                ]
            ]
            // (match summary.DirectChildren.Length with
            //  | 0 -> ()
            //  | _ -> div [ _class "taxon-toggle" ] [
            //             div [] []
            //         ])
        ]

    let alphabetIndex =
        [
            a [ _class "header-toggle"; _href "/Taxon?rank=Family" ] [ encodedText "Family" ]
            a [ _class "header-toggle"; _href "/Taxon?rank=Genus" ] [ encodedText "Genus" ]
            a [ _class "header-toggle"; _href "/Taxon?rank=Species" ] [ encodedText "Species" ]
            div [ _class "alphabet-index" ] (['A'..'Z'] |> List.map (fun l -> a [] [ encodedText (sprintf "%c" l) ] ))
        ]

    let index (vm:PagedResult<TaxonSummary>) =
        [
            Components.breadcrumb [] "Cool Page"
            Grid.row [ Grid.column Medium 12 alphabetIndex ]
            Grid.row (vm.Items |> List.map (fun t -> Grid.column Medium 6 [ taxonCard t ] ))
            Components.pagination vm.CurrentPage vm.ItemsPerPage vm.ItemTotal vm.TotalPages "/Taxon?rank=Genus"
        ] |> Layout.standard [] "Master Reference Collection" "The Global Pollen Project collates information from independent reference collections into this global reference collection. We use the Global Pollen Project's taxonomic backbone to define botanical names."


module ReferenceCollections =

    let infoCard =
        div [ _class "card card-inverse card-primary mb-3 text-center" ] [
            div [ _class "card-block" ] [
                h4 [ _class "card-title" ] [ encodedText "What are these collections?" ]
                p [ _class "card-text" ] [ encodedText "Pollen and spore reference material is commonly organised in phyiscal reference collections, of drawers of individual glass slides. We hold records of such collections, important information about their manufacture, and whether they have been digitised into the Global Pollen Project." ]
            ]
        ]

    let collectionCard (refCol:ReadModels.ReferenceCollectionSummary) =
        div [ _class "card" ] [
            div [ _class "card-block" ] [
                h4 [ _class "card-title" ] [ a [ _href "/Reference/ColId/ColVersion" ] [ encodedText refCol.Name ] ]
                h6 [ _class "card-subtitle mb-2 text-muted" ] [ encodedText <| sprintf "%s, %s / %s" refCol.CuratorSurname refCol.CuratorFirstNames refCol.Institution ]
                p [ _class "card-text" ] [ encodedText refCol.Description ] //TODO Shorten to 300 chars followed by epsilon
                p [ _class "card-text" ] [ small [ _class "text-muted" ] [ encodedText <| sprintf "Version %i published on %s" refCol.Version (refCol.Published.ToLongDateString())]]
            ]
        ]

    let listView (vm:ReferenceCollectionSummary list) =
        [
            Components.breadcrumb [] "Individual Reference Collections"
            Grid.row [
                Grid.column Medium 9 (vm |> List.map collectionCard)
                Grid.column Medium 3 [ infoCard ]
            ]
        ]
        |> Layout.standard [] "Individual Reference Collections" "Digitised and undigitised reference material from individual collections and institutions."

    let tableView (vm:ReferenceCollectionDetail) =
        [
            Components.breadcrumb [] "Cool Page"
            p [] [ encodedText vm.Description ]
            div [ _class "card" ] [
                div [ _class "card-block" ] [
                    h4 [ _class "card-title" ] [ encodedText "Contributors" ]
                    p [] [ strong [] [ encodedText "Curator:" ]; encodedText <| sprintf "%s %s" vm.CuratorFirstNames vm.CuratorSurname ]
                ]
            ]

            table [ _class "table table-responsive"; _id "reference-table"; _data "page-length" "100" ] [
                thead [ _class "thead-default" ] [
                    tr [] [
                        th [] [ encodedText "#" ]
                        th [] [ encodedText "Family" ]
                        th [] [ encodedText "Genus" ]
                        th [] [ encodedText "Species" ]
                        th [] [ encodedText "Digitised?" ]
                        th [] [ encodedText "Actions" ]
                    ]
                ]
                tbody [] (vm.Slides |> List.map(fun s ->
                    tr [ _class "taxon-status-" ] [
                        th [ _scope "row" ] [ encodedText s.CollectionSlideId ]
                        td [] [ a [ _href "" ] [ encodedText s.CurrentFamily ] ]
                        td [] [ a [ _href "" ] [ encodedText s.CurrentGenus ] ]
                        td [] [ a [ _href "" ] [ encodedText s.CurrentSpecies ] ]
                    ]
                ))
            ]

        ] |> Layout.standard [] vm.Name "Individual Reference Collection"

    let slideView vm =
        [

        ] |> Layout.standard [] "Individual Slide" "Individual Slide"


module Identify =

    let index (vm:ReadModels.GrainSummary list) = 
        let title = "Unidentified Pollen and Spore Pool"
        [
            Components.breadcrumb [] title
            Grid.row [
                Grid.column Medium 8 [
                    ul [ _class "grain-grid columns-4" ] [
                        for g in vm do
                            yield li [] [
                                a [ _href (sprintf "/Identify/%s" (g.Id.ToString()) ) ] [
                                    div [ _class "img-container portrait" ] [
                                        img [ _src g.Thumbnail ]
                                        div [ _class "ribbon" ] [ span [] [ encodedText "?" ] ]
                                    ]
                                ]
                            ]
                    ]
                ]
                Grid.column Medium 4 [
                    a [ _class "btn btn-primary btn-lg btn-block"; _style "margin-bottom:1em;"; _href "/Identify/Upload" ] [
                        Icons.fontawesome "plus-square"
                        encodedText " Submit a Grain / Spore"
                    ]
                    div [ _class "card mb-3 text-center" ] [
                        div [ _class "card-block" ] [
                            h4 [ _class "card-title" ] [ encodedText "What are these grains and spores?" ]
                            p [ _class "card-text" ] [ encodedText "These individual pollen and spores require taxonomic identification. You can identify any of them to family, genus, or species level. Once there is argeement between at least three people, and above a threshold, the identity may become confirmed." ]
                        ]
                    ]
                ]
            ]
        ] |> Layout.standard [] title "Some specimens have been submitted, for which the botanical origin is not known. Can you help with a morphological identification?"

    let disqus url =
        []
    
    let view absoluteUrl currentUserId (vm:GrainDetail) =
        let myIdentification =
            match currentUserId with
            | None -> None
            | Some userId -> vm.Identifications |> Seq.tryFind(fun i -> i.User = userId)
        [
            // Scripts: d3 viewer focusSlider scalebar slide knockout lookup identify
            // TODO Move to typescript?: $(function() {
            // var frames = ("@Model.Images[0].Frames").slice(1, -1).split(";");
            //     createViewer(frames, @Model.Images[0].PixelWidth);
            // });
            Components.breadcrumb [
                { Name = "Home"; Url = Urls.home }
                { Name = "Pollen of Unknown Identity"; Url = Urls.Identify.root }
            ] "Unidentified Specimen"
            
            Grid.row [
                Grid.column Medium 6 [
                    div [ _id "viewer-container" ] []
                    div [ _class "card" ] [
                        div [ _class "card-header" ] [ str "Select image:" ]
                        div [ _class "card-block" ] [
                            div [ _class "row"; _id "slide-gallery" ] (vm.Images |> List.map(fun i ->
                                div [ _class "slide-gallery-item col-md-3"
                                      attr "data-frames" (i.Frames.ToString())
                                      attr "data-pixelwidth" (i.PixelWidth.ToString()) ] [
                                    img [ _src i.Frames.Head; _alt "Image preview" ]
                                ]
                             ))
                        ]
                    ]
                    div [ _class "card" ] [
                        div [ _class "card-header" ] [ str "Discussion" ]
                        div [ _class "card-block" ] (disqus "")
                    ]
                ]
                Grid.column Medium 6 [
                    div [ _class "panel panel-default" ] [
                        div [ _class "panel-heading" ] [
                            Icons.fontawesome "leaf"
                            str " Context"
                        ]
                        div [ _class "panel-body" ] [
                            Grid.row [
                                Grid.column Medium 4 [
                                    label [] [ str "Sampling Method" ]
                                ]
                                Grid.column Medium 8 [
                                    match vm.AgeType with
                                    | "Calendar" -> span [] [
                                        strong [] [ str "Environmental." ]
                                        str "This grain or spore was from the environment, for example from a pollen trap, bee, honey, or soil."
                                        ]
                                    | _ -> span [] [
                                        strong [] [ str "Fossil." ]
                                        str "This grain or spore was taken from a sediment core or other environmental archive."
                                    ]
                                ]
                            ]
                            Grid.row [
                                Grid.column Medium 4 [
                                    match vm.AgeType with
                                    | "Calendar" -> label [] [ str "Year of Sampling" ]
                                    | _ -> label [] [ str "Age" ]
                                ]
                                Grid.column Medium 8 [
                                    if String.IsNullOrEmpty vm.AgeType then span [] [ str "Unknown" ]
                                    else if vm.AgeType <> "Calendar" then span [] [ str <| sprintf "%s years before present" (vm.Age.ToString("#,###")) ]
                                    else span [] [ str <| vm.Age.ToString() ]
                                ]
                            ]
                            Grid.row [
                                Grid.column Medium 12 [
                                    hr []
                                    label [] [ str "Location" ]
                                    img [ _style "text-align: left; margin-right: auto; display: block; max-width: 100%"
                                          _alt "Pollen Location"
                                          // TODO Move access token to appSettings
                                          _src <| sprintf "https://api.mapbox.com/styles/v1/mapbox/streets-v10/static/pin-s-a+9ed4bd(%f,%f)/%f,%f,3/560x200@2x?access_token=pk.eyJ1IjoibWFyZWVwMjAwMCIsImEiOiJjaWppeGUxdm8wMDQ3dmVtNHNhcHh0cHA1In0.OrAULrL8pJaL9N5WerUUDQ" vm.Longitude vm.Latitude vm.Longitude vm.Latitude ]
                                ]
                            ]
                            Grid.row [
                                Grid.column Medium 4 [ label [] [ str "Share" ] ]
                                Grid.column Medium 8 [
                                    a [ _href "https://twitter.com/intent/tweet?button_hashtag=GlobalPollenProject&text=Help%20identify%20this%20pollen%20grain"
                                        _class "twitter-hashtag-button"
                                        attr "url" absoluteUrl ] [ str "Tweet this grain" ]
                                    script [] [ rawText "!function (d, s, id) { var js, fjs = d.getElementsByTagName(s)[0], p = /^http:/.test(d.location) ? 'http' : 'https'; if (!d.getElementById(id)) { js = d.createElement(s); js.id = id; js.src = p + '://platform.twitter.com/widgets.js'; fjs.parentNode.insertBefore(js, fjs); } }(document, 'script', 'twitter-wjs');" ]
                                ]
                            ]
                        ]
                    ]
                    
                    // Identification pane
                    div [ _class "panel panel-primary" ] [
                        div [ _class "panel-heading" ] [
                            Icons.fontawesome "search"
                            str " Identify"
                        ]
                        div [ _class "panel-body" ] [
                            match currentUserId with
                            | None ->
                                p [] [
                                    a [ _href Urls.Account.login ] [ str "Log in" ]
                                    str " to identify this specimen."
                                ]
                            | Some _ ->
                                match myIdentification with
                                | Some _ -> span [] [ str "Thank you for suggesting a taxonomic identification." ]
                                | None ->
                                    h4 [] [ str "Can you identify this grain?" ]
                                    form [ _method "POST"; _action Urls.Identify.identify; _id "identify-form" ] [
                                        p [] [
                                            str "I can identify this grain to"
                                            select [ attr "data-bind" "value: rank"
                                                     _class "form-control form-control-sm inline-dropdown" ] [
                                                option [ _value "Family" ] [ str "Family" ]
                                                option [ _value "Genus" ] [ str "Genus" ]
                                                option [ _value "Species" ] [ str "Species" ]
                                            ]
                                            str "rank."
                                        ]
                                        Grid.row [
                                            Grid.column Small 3 [
                                                input [ attr "data-bind" "value: family, event: { blur: capitaliseFirstLetter($element) }"
                                                        _type "text"; _id "original-Family"; _class "form-control"
                                                        _onkeyup "suggest(this, 'Family');"
                                                        _autocomplete "off"; _placeholder "Family" ]
                                                ul [ _class "dropdown-menu taxon-dropdown"; _id "FamilyList"
                                                     _style "display:none" ] []
                                            ]
                                            Grid.column Small 3 [
                                                input [ attr "data-bind" "value: genus, enable: rank() != 'Family', event: { blur: capitaliseFirstLetter($element) }"
                                                        _type "text"; _id "original-Genus"; _class "form-control"
                                                        _onblur "disable('Genus');"
                                                        _onkeyup "suggest(this, 'Genus');"
                                                        _autocomplete "off"; _placeholder "Genus" ]
                                                ul [ _class "dropdown-menu taxon-dropdown"; _id "GenusList"
                                                     _style "display:none" ] []
                                            ]
                                            Grid.column Small 3 [
                                                input [ attr "data-bind" "value: species, disable: rank() != 'Species'"
                                                        _type "text"; _id "original-Species"; _class "form-control"
                                                        _onblur "disable('Species');"
                                                        _onkeyup "suggest(this, 'Species');"
                                                        _autocomplete "off"; _placeholder "Species" ]
                                                ul [ _class "dropdown-menu taxon-dropdown"; _id "SpeciesList"
                                                     _style "display:none" ] []
                                            ]
                                            Grid.column Small 3 [
                                                input [ attr "data-bind" "value: author, disable: rank() != 'Species', event: { blur: capitaliseFirstLetter($element) }"
                                                        _type "text"; _class "form-control"; _autocomplete "off"; _placeholder "Auth." ]
                                            ]
                                        ]
                                        small [] [ str "Authorship is optional. The given name will be traced within our taxonomic backbone to the currently accepted name." ]
                                        div [ attr "data-bind" "visible: newSlideTaxonStatus, if: newSlideTaxonStatus" ] [
                                            div [ attr "data-bind" "visible: newSlideTaxonStatus() == 'Error'" ] [
                                                p [] [
                                                    Icons.fontawesome "frown-o"
                                                    str " There was a problem communicating with the taxonomic backbone."
                                                ]
                                            ]
                                            div [ attr "data-bind" "visible: newSlideTaxonStatus().length > 1" ] [
                                                p [] [
                                                    Icons.fontawesome "frown-o"
                                                    str " Validation unsuccessful. There are "
                                                    span [ attr "data-bind" "text: newSlideTaxonStatus().length" ] []
                                                    str " matching names."
                                                ]
                                                ul [ attr "data-bind" "foreach: newSlideTaxonStatus" ] [
                                                    li [ attr "data-bind" "text: LatinName + ' ' + NamedBy + ' (' + TaxonomicStatus + ' name)'" ] []
                                                ]
                                            ]
                                            div [ attr "data-bind" "visible: newSlideTaxonStatus().length == 0" ] [
                                                p [] [
                                                    Icons.fontawesome "frown-o"
                                                    str " Taxon was not recognised by our taxonomic backbone."
                                                ]
                                            ]
                                        ]
                                        input [ _hidden; _name "TaxonId"; _id "TaxonId"; attr "data-bind" "value: currentTaxon" ]
                                        input [ _hidden; _name "GrainId"; _id "GrainId"; _value <| vm.Id.ToString() ]
                                        button [ _class "btn btn-primary"; _style "display:block"
                                                 attr "data-bind" "click: validateAndSubmit, enable: isValidTaxonSearch" ] [
                                            str "Identify"
                                        ]
                                    ] // end of form
                                    h4 [] [ str "Current Identification" ]
                                    match vm.Identifications.Length with
                                    | 0 -> p [] [ str "No current identifications" ]
                                    | _ ->
                                        table [ _class "table" ] [
                                            thead [] [
                                                tr [] [
                                                    th [] [ str "Rank" ]
                                                    th [] [ str "Method" ]
                                                    th [] [ str "Identified as" ]
                                                ]
                                            ]
                                            tbody [] (vm.Identifications |> List.map(fun i ->
                                                tr [] [
                                                    td [] [ str i.Rank ]
                                                    td [] [ str i.IdentificationMethod ]
                                                    td [] [ str <| sprintf "%s %s %s %s" i.Family i.Genus i.Species i.SpAuth ]
                                            ]))
                                        ]
                                        
                        ]
                    ] // end identification pane
                ]
            ]
        ] |> Layout.standard []
                 "Unidentified Specimen"
                 "This individual pollen grain or spore does not have a taxonomic identification. Can you help?"

    let add vm = 
        [
            // TODO Add datepicker to bundle call - CSS and JS
            // TODO bootstrap, d3, jcrop, viewer, measuringline, datepicker, add
            
            form [ _id "add-grain-form"; _novalidate ] [
                
                // Alert box
                div [ _class "alert alert-danger"; _id "errors-box" ] [
                    div [ _class "col-md-1" ] [
                        i [ _class "fa fa-exclamation-triangle"; _aria "hidden" "true"; _style "font-size: 2em; width: 100%; text-align: center" ] []
                    ]
                    div [ _class "col-md-11" ] [
                        p [ _id "errors" ] []
                    ]
                ]
                
                // 1. Sampling Method
                div [ _class "card identify-form-section"; _id "identity-sampling-section" ] [
                    div [ _class "card-header" ] [ str "1 - Choose Sampling Method" ]
                    div [ _class "card-block" ] [
                        fieldset [ _class "form-group row"; _id "identify-sampling-method" ] [
                            div [ _class "col-sm-10" ] [
                                div [ _class "form-check" ] [
                                    label [ _class "form-check-label" ] [
                                        input [ _class "form-check-input"; _name "identify-method-radio"; _id "identify-sampling-method-fossil"; _type "radio"; _value "fossil"; _checked ]
                                        str "A fossil pollen grain, or spore, obtained from a sedimentary sequence."
                                    ]
                                ]
                                div [ _class "form-check" ] [
                                    label [ _class "form-check-label" ] [
                                        input [ _class "form-check-input"; _name "identify-method-radio"; _id "identify-sampling-method-environmental"; _type "radio"; _value "environmental"; _checked ]
                                        str "A pollen grain collected from the environment, for example from a pollen trap, bee, honey, or soil. This grain has not been fossilised."
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
                
                // 2. Upload images
                div [ _class "card identify-form-section"; _id "identify-image-section" ] [
                    div [ _class "card-header" ] [ str "2 - Upload Image(s)" ]
                    div [ _class "card-block" ] [
                        label [ _for "identify-image-upload-button" ] [ str "Select image(s) (Shift-Click/Ctrl-Click to select multiple)" ]
                        br []
                        input [ _type "file"; _multiple; _class "upload btn"; _id "identify-image-upload-button" ]
                        div [ _id "identify-image-configuration"; _style "display:none" ] [
                            ul [ _class "nav nav-tabs"; _id "identify-image-config-tabs"; _role "tablist" ] []
                            div [ _class "tab-content"; _id "identify-image-config-content" ] [] 
                        ]
                    ]
                ]
                
                // 3. Location
                div [ _class "card identify-form-section"; _id "identify-location-section" ] [
                    div [ _class "card-header" ] [ str "3 - Location" ]
                    div [ _class "card-block" ] [
                        Grid.row [
                            Grid.column Medium 3 [
                                label [] [ str "Where was the pollen grain collected from? Click on the map to drop a pin in the correct location." ]
                                hr []
                                div [ _class "input-group" ] [
                                    input [ _class "text"; _readonly; _id "latitude-input"; _class "form-control"; _placeholder "Latitude" ]
                                    span [ _class "input-group-addon" ] []
                                ]
                                div [ _class "input-group" ] [
                                    input [ _class "text"; _readonly; _id "longitude-input"; _class "form-control"; _placeholder "Longitude" ]
                                    span [ _class "input-group-addon" ] []
                                ]
                            ]
                            Grid.column Medium 9 [
                                script [ _type "text/javascript"; _src <| sprintf "http://maps.google.com/maps/api/js?key=%s" Settings.googleApiKey ] []
                                div [ _id "map" ] []
                            ]
                        ]
                    ]
                ]
                
                // 4. Time
                div [ _class "card identify-form-section"; _id "identify-temporal-section" ] [
                    div [ _class "card-header" ] [ str "4 - Temporal Context" ]
                    div [ _class "card-block" ] [
                        div [ _id "identify-temporal-fossil" ] [
                            fieldset [ _class "form-group"; _id "identify-temporal-fossil-type" ] [
                                div [ _class "form-check form-check-inline" ] [
                                    label [ _class "form-check-label" ] [
                                        input [ _class "form-check-input"; _type "radio"; _name "identify-temporal-fossil-type"; _id "identify-temporal-fossil-radiocarbon"; _value "radiocarbon" ]
                                        str "Radiocarbon (years before present)"
                                    ]
                                ]
                                div [ _class "form-check form-check-inline" ] [
                                    label [ _class "form-check-label" ] [
                                        input [ _class "form-check-input"; _type "radio"; _name "identify-temporal-fossil-type"; _id "identify-temporal-fossil-lead"; _value "lead" ]
                                        str "Lead210"
                                    ]
                                ]
                                div [ _class "form-check form-check-inline" ] [
                                    label [ _class "form-check-label" ] [
                                        input [ _class "form-check-input"; _type "radio"; _name "identify-temporal-fossil-type"; _id "identify-temporal-fossil-unknown"; _value "unknown" ]
                                        str "Unknown"
                                    ]
                                ]
                            ]
                            div [ _id "identify-temporal-fossil-value-section"; _style "display:none" ] [
                                hr []
                                label [ _for "identify-temporal-fossil-ybp" ] [ str "Years before present: " ]
                                input [ _type "number"; _name "identify-temporal-fossil-ybp"; _id "identify-temporal-fossil-ybp"; _placeholder "0" ]
                                br []
                                span [] [ i [] [ str "Baseline year is 1950" ] ]
                            ]
                        ]
                        div [ _id "identify-temporal-environmental"; _style "display:none" ] [
                            div [ _class "form-group" ] [
                                label [ _for "identify-temporal-environmental-year" ] [ str "What year was this sample collected?" ]
                                input [ _id "identify-temporal-environmental-year"; _class "form-control" ]
                            ]
                        ]
                    ]
                    div [ _id "upload-progress"; _class "progress"; _style "display:none" ] [
                        div [ _class "progress-bar progress-bar-striped progress-bar-animated"; _role "progressbar"; _aria "valuenow" "0"; _aria "valuemin" "0"; _aria "valuemax" "100"; _style "width:0%" ] []
                    ]
                    a [ _id "submit"; _class "btn btn-primary" ] [ str "Submit my grain" ]
                ]                
            ]   
        ] |> Layout.standard []
                 "Request Identification - Unknown Grain"
                 "Upload a pollen grain or spore, for crowd-sourced taxonomic identification."


module Statistics =

    let stickySidebar =
        div [ _class "sticky-sidebar" ] [
            label [] [ encodedText "Statistics" ]
            nav [ _id "sidebar"; _class "nav flex-column" ] [
                a [ _class "nav-link"; _href "#representation" ] [ encodedText "Taxonomic Representation" ]
                a [ _class "nav-link"; _href "#contributions" ] [ encodedText "Contributions" ]
            ]
        ]

    let view (model:Responses.AllStatsViewModel) =

        let familyPercent = float model.Family.Count / float model.Family.Total * 100.
        let genusPercent = float model.Genus.Count / float model.Genus.Total * 100.
        let speciesPercent = float model.Species.Count / float model.Species.Total * 100.

        [
            Components.breadcrumb [] "Cool Page"
            Grid.row [
                Grid.column Medium 3 [ stickySidebar ]
                Grid.column Medium 9 [
                    h3 [] [ encodedText "Taxonomic Representation" ]
                    hr []
                    p [] [ encodedText "The Global Pollen Project is uses a heirarchical, botanical taxonomy. Of the known plant taxa, our master reference collection covers:" ]
                    div [ _class "row justify-content-center" ] [
                        Grid.column Small 4 [ 
                            Components.percentCircle familyPercent
                            p [] [ encodedText <| sprintf "or %i of %s families." model.Family.Count (String.Format("{0:n0}", model.Family.Total)) ]
                        ]
                        Grid.column Small 4 [ 
                            Components.percentCircle genusPercent
                            p [] [ encodedText <| sprintf "or %i of %s genera." model.Genus.Count (String.Format("{0:n0}", model.Genus.Total)) ]
                        ]
                        Grid.column Small 4 [ 
                            Components.percentCircle speciesPercent
                            p [] [ encodedText <| sprintf "or %i of %s species." model.Species.Count (String.Format("{0:n0}", model.Species.Total)) ]
                        ]
                    ]
                ]
            ]
        ] |> Layout.standard [] "Statistics" ""

module StatusPages =

    let notFound = 
        let autocomplete = Home.autocomplete
        [
            div [ _class "row justify-content-center"; _style "text-align:center;margin: 12.5em 0 7.5em;" ] [
                Grid.column Medium 4 [
                    i [ _class "fa fa-meh-o"; _style "font-size: 6em;margin-bottom: 0.25em;" ] []
                    h4 [] [ encodedText "Sorry, we couldn't find what you were looking for." ]
                    p [] [ encodedText "We've recently revamped the Global Pollen Project. We hope we haven't broken any links, but if you find any please let us know." ]
                    a [ _href Urls.home ] [ encodedText "Get me back to the homepage" ]
                    p [] [ encodedText "- or -" ]
                    p [] [ encodedText "Lookup a taxon in our reference collection" ]
                    autocomplete.View
                ]
            ]
        ] 
        |> Layout.master autocomplete.Scripts

    let statusLayout icon title description =
        [
            div [ _class "row justify-content-center"; _style "text-align:center;margin: 12.5em 0 7.5em;" ] [
                Grid.column Medium 4 [
                    i [ _class ("fa fa-" + icon); _style "font-size: 6em;margin-bottom: 0.25em;" ] []
                    h4 [] [ encodedText title ]
                    p [] [ encodedText description ]
                    a [ _href Urls.home ] [ encodedText "Get me back to the homepage" ]
                ]
            ]
        ] |> Layout.master []

    let error = statusLayout "bomb" "Sorry, there's been a slight snag..." "Your request didn't go through. You can try again, or please come back later. If this happens often, please let us know."
    
    let maintenance = statusLayout "wrench" "Under Temporary Maintenance" "We're working on some changes, which have required us to take the Pollen Project offline. We will be back later today, so sit tight."

    let denied = statusLayout "exclamation-triangle" "Access Denied" "You're not authorised to access this content."

module Tools =

    let main =
        [

        ] |> Layout.standard [] "Tools" "Some tools"


module Admin =

    let users vm =
        [

        ] |> Layout.standard [] "Users" "Admin"

    let curate vm =
        [
            
        ] |> Layout.standard [] "Curate" "Curate"
       
        
module Profile =
    
    /// View to allow quick creation and editing of a basic public profile
    let summary (vm:PublicProfile option) ctx =
        [
            match vm with
            | None ->
                
                // Profile: use name or mask into 
                p [] [ str "Your profile has not been created" ]
                //form [ _action Urls.Account.createProfile; _method "POST" ] [
                //    Forms.formField <@  @>
                //]
                
                
            | Some profile ->
                p [] [ str "Please " ]
                h3 [] [ str "" ]
                
                Forms.formField <@ profile.FirstName @> ctx
                p [] [ str profile.FirstName ]
                p [] [ str profile.LastName ]
        ] |> Layout.standard [] "Your Profile" "Your Profile"
    
    