module GlobalPollenProject.Web.HtmlViews

open System
open Giraffe.GiraffeViewEngine 
open ReadModels

type Script = string
type ActiveComponent = { View: XmlNode; Scripts: Script list }

let _data attr value = KeyValue("data-" + attr, value)

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
            |> Seq.choose (fun t -> match t with | :? ValidationAttribute as v -> printfn "%A" v; Some v | _ -> None)
            |> Seq.collect (MvcAttributeValidation.clientSideInputValidationTags pi.DeclaringType pi.Name)
            |> Seq.toList
        | _ -> []


module Forms =

    open TagHelpers
    open Microsoft.FSharp.Quotations

    let formField' fieldName validationAttributes = 
        div [ _class "form-group-row" ] [
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

    let validationSummary vm =
        div [] []

module Layout = 

    let loginMenu =
        // ul [ _class "navbar-nav" ] [
        //     li [ _class "navbar-dropdown" ] [
        //         a [ _class "nav-link dropdown-toggle"; _href "#"; _id "navbarDropdownMenuLink" ] [
        //             match loadProfile user.Id with
        //             | Some p -> span [] [ encodedText (sprintf "%s %s" p.FirstName p.LastName) ]
        //             | None -> getUserName user
        //         ]
        //     ]
        // ]
        ul [ _class "nav navbar-nav" ] [
            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "/Account/Register" ] [ encodedText "Register" ] ]
            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "/Account/Login" ] [ encodedText "Log in" ] ]
        ]

    let navigationBar =
        nav [ _class "navbar navbar-toggleable-md navbar-light bg-faded fixed-top"] [
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
                    loginMenu
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
                            li [] [ a [ _href "/Guide" ] [ encodedText "About"] ]
                            li [] [ a [ _href "/Api" ] [ encodedText "Public API"] ]
                            li [] [ a [ _href "/Terms" ] [ encodedText "Terms and Licensing"] ]
                        ]
                    ]
                    div [ _class "col-md-5"; _style "padding-right:4em" ] [
                        h4 [] [ encodedText "Data" ]
                        ul [] [
                            li [] [ a [ _href "/Taxon" ] [ encodedText "Master Reference Collection"] ]
                            li [] [ a [ _href "/Reference" ] [ encodedText "Individual Reference Collections"] ]
                            li [] [ a [ _href "/Identify" ] [ encodedText "Unidentified Specimens"] ]
                        ]
                        h4 [] [ encodedText "Tools" ]
                        ul [] [
                            li [] [ a [ _href "/Digitise" ] [ encodedText "Online Digitisation Tools"] ]
                            li [] [ a [ _href "/Tools" ] [ encodedText "Botanical Name Tracer"] ]
                        ]
                    ]
                    div [ _class "col-md-4 footer-images"] [
                        h4 [] [ encodedText "Funding" ]
                        p [] [ encodedText "The Global Pollen Project is funded via the Natural Environment Research Council of the United Kingdom, and the Oxford Long-Term Ecology Laboratory."]
                        a [ _href "https://oxlel.zoo.ox.ac.uk"; _target "_blank"] [
                            img [ _src "/images/oxlellogo.png"; _alt "Long Term Ecology Laboratory" ]
                            img [ _src "/images/oxford-logo.png"; _alt "University of Oxford" ]
                        ]
                    ]
                ]
                div [ _class "row" ] [
                    hr []
                    div [ _class "col-md-12" ] [
                        p [ _style "text-align:center;" ] [ encodedText "The Global Pollen Project 1.5" ]
                    ]
                ]
            ]
        ]

    let headSection pageTitle =
        head [] [
            meta [_charset "utf-8"]
            meta [_name "viewport"; _content "width=device-width, initial-scale=1.0" ]
            title [] [ pageTitle |> sprintf "%s - Global Pollen Project" |> encodedText ]

            // Fonts
            link [ _rel "stylesheet"; _href "https://fonts.googleapis.com/css?family=Montserrat:400,700" ]
            link [ _rel "stylesheet"; _href "https://fonts.googleapis.com/css?family=Hind"]
            link [ _rel "stylesheet"; _href "/lib/font-awesome/css/font-awesome.min.css"]

            // Styles
            link [ _rel "stylesheet"; _href "/lib/bootstrap/dist/css/bootstrap.min.css" ]
            link [ _rel "stylesheet"; _href "/lib/Jcrop/css/Jcrop.min.css" ]
            link [ _rel "stylesheet"; _href "/css/styles.css" ]
        ]

    let baseScripts = [
        "/lib/jquery/dist/jquery.js"
        "/lib/tether/dist/js/tether.js"
        "/lib/bootstrap/dist/js/bootstrap.js"
    ]

    let toScriptTag (s:Script) =
        script [ _src s ] []

    let toScriptTags (scripts:Script list) =
        scripts |> List.map toScriptTag

    let headerBar title subtitle =
        header [] [
            Grid.container [
                h1 [] [ encodedText title ]
                p [] [ encodedText subtitle ]
            ]
        ]

    let master (scripts: Script list) content =
        html [] [
            headSection "Global Pollen Project"
            body [] (
                List.concat [
                    [ navigationBar
                      div [ _class "main-content" ] content
                      footer ]
                    (baseScripts |> toScriptTags)
                    (scripts |> toScriptTags) ] )
        ]


    let standard scripts title subtitle content =
        master scripts ( headerBar title subtitle :: [ Grid.container content ])


module Components =

    let breadcrumb =
        div [ _class "row" ] [
            div [ _class "col-md-12" ] [
                ol [ _class "breadcrumb" ] [
                    li [ _class "breadcrumb-item" ] [ a [ _href "/" ] [ encodedText "Home" ] ]
                    li [ _class "breadcrumb-item" ] [ a [ _href "/Taxon" ] [ encodedText "Master Reference Collection" ] ]
                    li [ _class "breadcrumb-item" ] [ a [] [ encodedText "Cool" ] ]
                ]
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
                input [ _name "lex"; _title "Search by latin name"; _id "ref-collection-search" ]
                div [ _class "dropdown-menu"; _id "suggestList"; _style "display:none" ] []
                button [ _type "submit"; _title "Search"; _class "btn btn-primary btn-lg" ] [ encodedText "Go" ] 
            ]
        { View = view
          Scripts = [ "/js/home/suggest.js" ]}

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
        let searchField = autocomplete
        [
            heading autocomplete.View
            stats model
            tripleIcons
            unidentifiedGrainMap
        ] |> Layout.master searchField.Scripts


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
            Components.breadcrumb
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
            [ "/lib/leaflet/dist/leaflet.js"
              "/lib/nouislider/distribute/nouislider.min.js"
              "/js/links/gbif-map.js"
              "//d3js.org/d3.v3.min.js" 
              "//d3js.org/topojson.v0.min.js"
              "/lib/wnumb/wNumb.js"
              "/js/links/neotoma-map.js" ]
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
            Components.breadcrumb
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
                encodedText "Guide Contents"
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
                span [ _class "hide-xs divider" ] []
                span [] [
                    Icons.fontawesome "calendar"
                    encodedText metadata.["Date"]
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
            Components.breadcrumb
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
            Components.breadcrumb
            Grid.row [
                Grid.column Medium 9 (vm |> List.map collectionCard)
                Grid.column Medium 3 [ infoCard ]
            ]
        ]
        |> Layout.standard [] "Individual Reference Collections" "Digitised and undigitised reference material from individual collections and institutions."

    let tableView (vm:ReferenceCollectionDetail) =
        [
            Components.breadcrumb
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
            Components.breadcrumb
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

    let login (model:Requests.LoginRequest option) =
        let vm = 
            match model with
            | None -> { Email = ""; Password = ""; RememberMe = false }
            | Some vm -> vm
        [
            Grid.row [
                Grid.column Medium 8 [
                    form [ _action "/Account/Login"; _method "POST"; _class "form-horizontal" ] [
                        validationSummary vm
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
            ]
        ] |> Layout.standard [ 
            "/lib/jquery-validation/jquery.validate.js"
            "/lib/jquery-validation-unobtrusive/jquery.validate.unobtrusive.js" ] "Log in" "Use your existing Global Pollen Project account, Facebook or Twitter"
