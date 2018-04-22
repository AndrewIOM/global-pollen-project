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

let jsBundle = "/Scripts/main.bundle.js"

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
    open Microsoft.AspNetCore.Mvc.DataAnnotations.Internal
    open Microsoft.AspNetCore.Mvc.Internal
    open Microsoft.AspNetCore.Mvc.ModelBinding
    open Microsoft.AspNetCore.Mvc.ModelBinding.Metadata
    open Microsoft.AspNetCore.Mvc.ModelBinding.Validation
    open Microsoft.AspNetCore.Mvc.ViewFeatures
    open Microsoft.Extensions.Options

    type ModelMetadataProvider =
        inherit DefaultModelMetadataProvider
        static member CreateDefaultProvider() =
            let detailsProviders = [
                new DefaultBindingMetadataProvider() :> IMetadataDetailsProvider
                new DefaultValidationMetadataProvider() :> IMetadataDetailsProvider
                new DataAnnotationsMetadataProvider(Options.Create(new MvcDataAnnotationsLocalizationOptions()), null) :> IMetadataDetailsProvider
                new DataMemberRequiredBindingMetadataProvider() :> IMetadataDetailsProvider
            ]
            let compositeDetailsProvider = new DefaultCompositeMetadataDetailsProvider(detailsProviders)
            DefaultModelMetadataProvider(compositeDetailsProvider, Options.Create(new Microsoft.AspNetCore.Mvc.MvcOptions()))

    type ModelValidatorProvider =
       inherit CompositeModelValidatorProvider
       static member CreateDefaultProvider() : CompositeModelValidatorProvider =
        let x = DefaultModelValidatorProvider()
        let y = DataAnnotationsModelValidatorProvider(new ValidationAttributeAdapterProvider(),Options.Create(new MvcDataAnnotationsLocalizationOptions()),null)
        let providers = ResizeArray [ x :> IModelValidatorProvider; y :> IModelValidatorProvider ]
        new CompositeModelValidatorProvider(providers)

    let clientSideInputValidationTags' p pName attr =
        let provider = ModelMetadataProvider.CreateDefaultProvider()
        let metadata = provider.GetMetadataForProperty(p, pName)
        let actionContext = new Microsoft.AspNetCore.Mvc.ActionContext()
        let context = new ClientModelValidationContext(actionContext, metadata, provider, new AttributeDictionary())
        let adapter = ValidationAttributeAdapterProvider()
        let a = adapter.GetAttributeAdapter(attr,null)
        match isNull a with
        | true -> ()
        | false -> a.AddValidation context
        context.Attributes

    let clientSideInputValidationTags p pName attr =
        clientSideInputValidationTags' p pName attr
        :> seq<_>
        |> Seq.map (fun i -> KeyValue(i.Key,i.Value))


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

    let validationFor expr =
        match expr with 
        | PropertyGet(_,pi,_) -> 
            pi.GetCustomAttributes(typeof<ValidationAttribute>,false)
            |> Seq.choose (fun t -> match t with | :? ValidationAttribute as v -> Some v | _ -> None)
            |> Seq.collect (MvcAttributeValidation.clientSideInputValidationTags pi.DeclaringType pi.Name)
            |> Seq.toList
        | _ -> []

module Forms =

    open TagHelpers
    open Microsoft.FSharp.Quotations

    let formField' fieldName validationAttributes = 
        div [ _class "form-group row" ] [
            label [ _class "col-sm-2 col-form-label"; _for fieldName ] [ encodedText fieldName ]
            Grid.column Small 10 [
                input (List.concat [[ _id fieldName; _class "form-control"; ]; validationAttributes ])
                span [ _class "text-danger field-validation-valid"; _data "valmsg-for" fieldName; _data "valmsg-replace" "true" ] []
            ]
        ]

    let formField (e:Expr) =
        let name = propertyName e
        let validationAttributes = validationFor e
        formField' name validationAttributes

    let formGroup (e:Expr) helpText =
        let name = propertyName e
        let validationAttributes = validationFor e
        div [ _class "form-group" ] [
            label [] [ encodedText name ]
            input (List.concat [[ _id name; _name name; _class "form-control"; ]; validationAttributes ])
            small [ _id "name-help" ] [ encodedText helpText ] 
        ]

    let validationSummary (additionalErrors: ValidationError list) vm =
        let errorHtml =
            additionalErrors
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
                ]
            ]
        | None ->
            ul [ _class "nav navbar-nav" ] [
                li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.Account.register ] [ encodedText "Register" ] ]
                li [ _class "nav-item" ] [ a [ _class "nav-link"; _href Urls.Account.login ] [ encodedText "Log in" ] ] ]

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
                            encodedText "The Global Pollen Project 1.5"
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
        "https://code.jquery.com/jquery-3.2.1.slim.min.js"
        "https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.12.9/umd/popper.min.js"
        "https://maxcdn.bootstrapcdn.com/bootstrap/4.0.0/js/bootstrap.min.js"
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
        div [ _class "row" ] [
            div [ _class "col-md-12" ] [
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
                a [ _href taxonLink ] [ span [ _class "taxon-name" ] [ encodedText summary.LatinName ] ]
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

    let view vm = 
        [

        ] |> Layout.standard [] "View" "View"

    let add vm = 
        [

        ] |> Layout.standard [] "Add" "Add"


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


module Account =

    open Forms

    let login errors (vm:Requests.LoginRequest) =
        [
            Grid.row [
                Grid.column Medium 8 [
                    form [ _action "/Account/Login"; _method "POST"; _class "form-horizontal" ] [
                        validationSummary errors vm
                        formField <@ vm.Email @>
                        formField <@ vm.Password @>
                        formField <@ vm.RememberMe @>
                        div [ _class "row form-group" ] [
                            div [ _class "offset-sm-2 col-sm-10" ] [
                                button [ _type "submit"; _class "btn btn-primary" ] [ encodedText "Sign in" ]
                                a [ _class "btn btn-secondary"; _href "/Account/ForgotPassword" ] [ encodedText "Forgotten Password" ] 
                            ]
                        ]
                    ]
                ]
                Grid.column Medium 4 [
                    section [] [
                        form [ _action "/Account/ExternalLogin"; _method "POST"; _class "form-horizontal" ] [
                            button [ _name "provider"; _class "btn btn-block btn-social btn-facebook"; _type "submit"; _value "Facebook" ] [ 
                                Icons.fontawesome "facebook"
                                encodedText "Sign in with Facebook" ]
                            button [ _name "provider"; _class "btn btn-block btn-social btn-twitter"; _type "submit"; _value "Twitter" ] [ 
                                Icons.fontawesome "twitter"
                                encodedText "Sign in with Twitter" ]
                        ]
                        br []
                        div [ _class "panel panel-primary" ] [
                            div [ _class "panel-heading" ] [
                                Icons.fontawesome "pencil"
                                encodedText "Sign up today"
                            ]
                            div [ _class "panel-body" ] [
                                p [] [ encodedText "Register to submit your pollen and exchange identifications." ]
                                a [ _class "btn btn-secondary"; _href "/Account/Register" ] [ encodedText "Register" ]
                            ]
                        ]
                    ]
                ]
            ]
        ] |> Layout.standard [] "Log in" "Use your existing Global Pollen Project account, Facebook or Twitter"

    let register errors (vm:NewAppUserRequest) =
        [
            form [ _action "/Account/Register"; _method "POST"; _class "form-horizontal" ] [
                p [] [ encodedText "An account will enable you to submit your own unknown pollen grains and identify others. You can also request access to our digitisation features." ]
                p [] [ encodedText "You can also alternatively"; a [ _href "/Account/Login" ] [ encodedText "sign in with your Facebook or Twitter account." ] ]
                hr []
                validationSummary errors vm
                h4 [] [ encodedText "About You" ]
                formField <@ vm.Title @>
                formField <@ vm.FirstName @>
                formField <@ vm.LastName @>
                formField <@ vm.Email @>
                formField <@ vm.EmailConfirmation @>
                formField <@ vm.Password @>
                formField <@ vm.ConfirmPassword @>
                hr []
                h4 [] [ encodedText "Your Organisation" ]
                p [] [ encodedText "Are you a member of a lab group, company or other organisation? Each grain you identify gives you a bounty score. By using a common group name, you can build up your score together. Can your organisation become top identifiers?" ]
                formField <@ vm.Organisation @>
                p [] [ encodedText "By registering, you agree to the Global Pollen Project"; a [ _href "/Guide/Terms" ] [ encodedText "Terms and Conditions." ] ]
                button [ _type "submit"; _class "btn btn-primary" ] [ encodedText "Register" ]
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Register" "Create a new account"

    let externalRegistration provider errors (vm:ExternalLoginConfirmationViewModel) =
        [
            form [ _action "/Account/ExternalLoginConfirmation"; _method "POST"; _class "form-horizontal" ] [
                p [] [ encodedText ("You've successfully authenticated with " + provider + ". We just need a few more personal details from you before you can log in.") ]
                validationSummary errors vm
                h4 [] [ encodedText "About You" ]
                formField <@ vm.Title @>
                formField <@ vm.FirstName @>
                formField <@ vm.LastName @>
                formField <@ vm.Email @>
                formField <@ vm.EmailConfirmation @>
                hr []
                h4 [] [ encodedText "Your Organisation" ]
                p [] [ encodedText "Are you a member of a lab group, company or other organisation? Each grain you identify gives you a bounty score. By using a common group name, you can build up your score together. Can your organisation become top identifiers?" ]
                formField <@ vm.Organisation @>
                p [] [ encodedText "By registering, you agree to the Global Pollen Project"; a [ _href "/Guide/Terms" ] [ encodedText "Terms and Conditions." ] ]
                button [ _type "submit"; _class "btn btn-primary" ] [ encodedText "Register" ]
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Nearly logged in..." ("Associate your" + provider + "account")

    let awaitingEmailConfirmation =
        [
            p [] [ encodedText "Please check your email for an activation link. You must do this before you can log in." ]
        ] |> Layout.standard [] "Confirm Email" ""

    let confirmEmail =
        [
            p [] [ 
                encodedText "Thank you for confirming your email. Please"
                a [ _href Urls.Account.login ] [ encodedText "Click here to Log in" ]
                encodedText "." ]
        ] |> Layout.standard [] "Confirm Email" ""

    let forgotPasswordConfirmation =
        [ p [] [ encodedText "Please check your email to reset your password" ]
        ] |> Layout.standard [] "Confirm Email" ""

    let externalLoginFailure =
        [ p [] [ encodedText "Unsuccessful login with service" ] ]
        |> Layout.standard [] "Login failure" ""

    let resetPassword (vm:ResetPasswordViewModel) =
        [
            form [ _action "/Account/ResetPassowrd"; _method "POST"; _class "form-horizontal" ] [
                // Validation summary
                input [ _hidden; _value vm.Code ]
                Forms.formField <@ vm.Email @>
                Forms.formField <@ vm.Password @>
                Forms.formField <@ vm.ConfirmPassword @>
                Forms.submit
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Reset Password" ""

    let resetPasswordConfirmation =
        [ p [] [ 
            encodedText "Your password has been reset."
            a [ _href "/Account/Login" ] [ encodedText "Click here to login." ] ]
        ] |> Layout.standard [] "Confirm Email" ""

    let lockout =
        [

        ] |> Layout.standard [] "" ""

    let forgotPassword (vm:ForgotPasswordViewModel) =
        [
            form [ _href "/Account/ForgotPassword"; _method "POST"; _class "form-horizontal" ] [
                h4 [] [ encodedText "Enter your email." ]
                // Validation summary here
                formField <@ vm.Email @>
                Forms.submit
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Forgot your password?" ""

module Manage =

    let index vm = 
        [

        ] |> Layout.standard [] "" ""

    let removeLogin vm = 
        [

        ] |> Layout.standard [] "" ""

    let manageLogins vm = 
        [

        ] |> Layout.standard [] "" ""

    let setPassword errors vm = 
        [

        ] |> Layout.standard [] "" ""
        
    let changePassword errors vm = 
        [

        ] |> Layout.standard [] "" ""


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
    
    let maintainance = statusLayout "wrench" "Under Temporary Maintainance" "We're working on some changes, which have required us to take the Pollen Project offline. We will be back later today, so sit tight."

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