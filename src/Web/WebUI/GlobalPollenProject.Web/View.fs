module GlobalPollenProject.Web.HtmlViews

open System
open System.Text.Json
open System.Text.RegularExpressions
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
    let mapboxToken = getAppSetting "MapboxToken"

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

module License =
                
        let nonCommercial =
            div [] [
                a [ _rel "license"; _href "http://creativecommons.org/licenses/by-nc/4.0/" ] [
                    img [ _alt "Creative Commons License"; _style "border-width:0"
                          _src "https://i.creativecommons.org/l/by-nc/4.0/80x15.png" ]
                ]
                br []
                str "These images are licensed under a "
                a [ _href "http://creativecommons.org/licenses/by-nc/4.0/" ] [ str "Creative Commons Attribution-NonCommercial 4.0 International License" ] ]

module Layout = 

    let loginMenu (profile: ReadModels.PublicProfile option) =
        match profile with
        | Some p ->
            ul [ _class "navbar-nav" ] [
                li [ _class "nav-item dropdown" ] [
                    a [ _class "nav-link dropdown-toggle"; _href "#"; _id "navbarDropdownMenuLink"
                        _role "button"; _data "toggle" "dropdown"; _aria "haspopup" "true" ] [
                        span [] [ encodedText (sprintf "%s %s" p.FirstName p.LastName) ]
                    ]
                    div [ _class "dropdown-menu"; _aria "labelledby" "navbarDropdownMenuLink" ] [
                        a [ _href "/Profile"; _class "dropdown-item" ] [ str "Your profile" ]
                        a [ _href "/Account/Logout"; _class "dropdown-item" ] [ str "Log out" ]
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
                button [ _class "navbar-toggler navbar-toggler-right"; _type "button"; _data "toggle" "collapse"
                         _data "target" "#master-navbar"; _aria "controls" "master-navbar"; _aria "expanded" "false"
                         _aria "label" "Toggle navigation" ] [
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
                            li [] [ a [ (*_href Urls.digitise*) ] [ encodedText "Online Digitisation Tools"; span [ _style "font-weight: normal;margin-left: 0.5em;"; _class "badge badge-info" ] [ encodedText "Under Maintenance" ] ] ]
                            li [] [ a [ _href Urls.tools ] [ encodedText "Botanical Name Tracer"] ]
                        ]
                    ]
                    div [ _class "col-md-4 footer-images"] [
                        h4 [] [ encodedText "Funding" ]
                        p [] [ encodedText "The Global Pollen Project is funded via the Natural Environment Research Council of the United Kingdom, and the Oxford Long-Term Ecology Laboratory."]
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
                            a [ _href "https://github.com/AndrewIOM/gpp-cqrs"; _target "_blank" ] [ encodedText "GitHub" ] ]
                    ]
                ]
            ]
        ]

    let headSection pageTitle =
        head [] [
            meta [_charset "utf-8"]
            meta [_name "viewport"; _content "width=device-width, initial-scale=1.0" ]
            title [] [ pageTitle |> sprintf "%s - Global Pollen Project" |> encodedText ]
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

    let master (scripts: Script list) title content profile =
        html [] [
            headSection <| sprintf "%s - Global Pollen Project" title
            body [] (
                List.concat [
                    [ navigationBar profile
                      div [ _class "main-content" ] content
                      footer ]
                    ((List.concat [baseScripts; scripts]) |> toScriptTags) ] )
        ]

    let standard scripts title subtitle content =
        master scripts title ( headerBar title subtitle :: [ Grid.container content ])


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

    let paginationLink link = 
        li [ _class "page-item" ] [
            a [ _class "page-link"; _href link ] []
        ]

    let pagination (vm:PagedResult<'a>) linkBase =
        let backLink = paginationLink <| sprintf "%s&page=%i" linkBase (vm.CurrentPage - 1)
        let forwardLink = paginationLink  <| sprintf "%s&page=%i" linkBase (vm.CurrentPage + 1)
        nav [ _aria "label" "pagination" ] [
            ul [ _class "pagination" ] (
                [ 1 .. vm.TotalPages ] |> List.map (fun i ->
                    li [ _class "page-item" ] [ 
                        a [ _class "page-link"; _href (sprintf "%s&page=%i" linkBase i) ] [encodedText (sprintf "%i" i) ]
                    ] )
                |> fun l -> if vm.CurrentPage > 1 then backLink::l else l
                |> fun l -> if vm.CurrentPage > 1 then List.concat[l; [forwardLink]] else l
            )
        ]

    let percentCircle (percent:float) =
        div [ _class <| sprintf "c100 p%i" (int percent) ] [
            span [] [ encodedText <| percent.ToString("0.00") ]
            div [ _class "slice" ] [
                div [ _class "bar" ] []
                div [ _class "fill" ] []
            ]
        ]
        
    let panel style heading body =
        div [ _class <| sprintf "panel panel-%s" style ] [
            div [ _class "panel-heading" ] heading
            div [ _class "panel-body" ] body
        ]

    let card name contents =
        div [ _class "card" ] [
            div [ _class "card-header" ] [ str name ]
            div [ _class "card-block" ] contents
        ]
    
    let galleryViewWindow =
        div [ _id "viewer-container" ] []
    
    let testGalleryItem =
       div [ _class "slide-gallery-item col-md-3 active"
             _data "frames" "[&quot;https://pollen.blob.core.windows.net/live-cache/76084935-8f48-4d7f-999b-85a125870cd2_0_web.jpg&quot;,&quot;https://pollen.blob.core.windows.net/live-cache/f75986fb-ddd5-4d73-918c-2e069d22e23d_1_web.jpg&quot;,&quot;https://pollen.blob.core.windows.net/live-cache/63d4d0aa-c59e-4298-973e-5b29550eb775_2_web.jpg&quot;,&quot;https://pollen.blob.core.windows.net/live-cache/711a4872-5ed1-4508-9247-c7096cae9f66_3_web.jpg&quot;,&quot;https://pollen.blob.core.windows.net/live-cache/322d9bd1-7845-4233-b5b0-ff7917361f9d_4_web.jpg&quot;]"
             _data "pixelwidth" "0.0734194601726228" ] [
           img [ _src "https://pollen.blob.core.windows.net/live-cache/76084935-8f48-4d7f-999b-85a125870cd2_0_web.jpg" ]
       ]
    
    let galleryViewImageList images =
        div [ _class "card" ] [
            div [ _class "card-header" ] [ str "Select image:" ]
            div [ _class "card-block" ] [
                div [ _class "row"; _id "slide-gallery" ] (images |> List.map(fun i ->
                    div [ _class "slide-gallery-item col-md-3"
                          attr "data-frames" (JsonSerializer.Serialize(i.Frames))
                          attr "data-pixelwidth" (i.PixelWidth.ToString()) ] [
                        img [ _src i.Frames.Head; _alt "Image preview" ]
                    ]
                 ) |> List.append [ testGalleryItem ] )
            ]
        ]

    let detailList items =
        items
        |> List.map(fun (name,value) ->
            [ dt [ _class "col-sm-5" ] [ str name ]
              dd [ _class "col-sm-7" ] [ value ] ] )
        |> List.concat
        |> Grid.row

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
        ] |> Layout.master (List.append autocomplete.Scripts unidentifiedGrainMap.Scripts) "Home"


module Slide =
    
    let citation vm latinName =
        let compiledCitation = sprintf "%s, %s (%i). %s (%s). Digitised palynological slide. In: %s. Retrieved from globalpollenproject.org on %s"
                                   vm.Slide.CollectorName vm.Slide.CollectorName vm.Collection.Published.Year
                                   latinName vm.Slide.CollectionSlideId vm.Collection.Name (DateTime.Now.ToString("d"))
        div [ _class "card card-inverse card-primary crop-panel mb-3" ] [
            div [ _class "card-block" ] [
                h4 [ _class "card-title" ] [ encodedText "Citation" ]
                p [] [ encodedText compiledCitation ]
            ]
        ]

    let breadcrumbLinks model =
        if model.Slide.CurrentTaxonStatus = "accepted"
        then
            let mrc = { Name = "Master Reference Collection" ; Url = Urls.MasterReference.root }
            match model.Slide.Rank with
            | "Family" ->
                [ mrc; { Name = model.Slide.CurrentFamily; Url = Urls.MasterReference.family model.Slide.CurrentFamily } ]
            | "Genus" ->
                [ mrc; { Name = model.Slide.CurrentFamily; Url = Urls.MasterReference.family model.Slide.CurrentFamily }
                  { Name = model.Slide.CurrentGenus; Url = Urls.MasterReference.genus model.Slide.CurrentFamily model.Slide.CurrentGenus } ]
            | _ ->
                [ mrc; { Name = model.Slide.CurrentFamily; Url = Urls.MasterReference.family model.Slide.CurrentFamily }
                  { Name = model.Slide.CurrentGenus; Url = Urls.MasterReference.genus model.Slide.CurrentFamily model.Slide.CurrentGenus } ]
        else
            [ { Name = "Individual Reference Collections"; Url = Urls.Collections.root }
              { Name = model.Collection.Name; Url = Urls.Collections.byId model.Collection.Id } ]
    
    let cropPanel isSignedIn =
        div [ _class "card card-inverse card-primary mb-3 text-center crop-panel"; _id "cropping-panel" ] [
            div [ _class "card-block" ] [
                span [] [ str "Help us identify individual pollen grains and spores within this slide." ]
                match isSignedIn with
                | true -> button [ _id "cropping-button"; _class "btn btn-primary btn-block" ] [ str "Start" ]
                | false -> span [] [ a [ _href Urls.Account.login ] [ str "Log in to get started." ] ]
            ]
        ]
    
    let location model =
        div [] [
            span [ _class "fa-stack fa-lg mr-2" ] [
                i [ _class "fa fa-circle fa-stack-2x" ] []
                i [ _class "fa fa-globe fa-stack-1x fa-inverse" ] []
            ]
            match model.Slide.LocationType with
            | "Unknown" -> span [] [ encodedText "Unknown location" ]
            | "Place name" ->
                let layers = Regex.Matches(model.Slide.Location, @"(?<== ).*?(?=;)") |> Seq.toList
                if layers.Length = 0 then span [] [ encodedText "Unknown" ]
                else
                    
                    div [] []
            | _ -> span [] [ encodedText "Unknown location" ]
        ]
    
    let time model =
        div [] [
            span [ _class "fa-stack fa-lg mr-2" ] [
                i [ _class "fa fa-circle fa-stack-2x" ] []
                i [ _class "fa fa-calendar fa-stack-1x fa-inverse" ] []
            ]
            if model.Slide.AgeType = "Unknown" then span [] [ str "Unknown" ]
            else span [] [ str <| model.Slide.Age.ToString() ]
        ]
    
    let identificationMethod (model:Responses.SlidePageViewModel) =
        let name, description =
            match model.Slide.IdMethod with
            | "Botanical" ->
                match model.Slide.IdMethod with
                | "Field" ->
                    "Taken from a wild plant",
                    (sprintf "The plant was identified by %s." model.Slide.PlantId.IdentifiedBySurname)
                | "LivingCollection" ->
                    "Botanic Garden (Living Collection)",
                    (sprintf "The plant reference is %s at the botanic garden %s.
                     You can lookup the botanic garden code in <a href=\"https://www.bgci.org/garden_search.php\"
                     target=\"_blank\">the BGCI online database</a> for more information." model.Slide.PlantId.InternalId model.Slide.PlantId.InstitutionCode)
                | "Voucher" ->
                    "Herbarium Voucher",
                    (sprintf "The voucher barcode is <strong>%s</strong> in the
                     <strong>%s</strong> herbarium. You can lookup this herbarium
                     in the <a href=\"http://sweetgum.nybg.org/science/ih/\" target=\"_blank\">Index Herbariorum</a> for more information." model.Slide.PlantId.InternalId model.Slide.PlantId.InstitutionCode)
                | _ -> "Unknown", "The method used to identify this specimen is not known."
            | "Environmental" -> "Contextual", ""
            | "Morphological" -> "Morphological", ""
            | _ -> "", ""
        div [] [
            h4 [] [ encodedText name ]
            small [ _class "text-muted" ] [ encodedText description ]
        ]
        
    /// View for an individual slide with image viewer
    /// TODO Add in cropping interface (also removed from typescript)
    let content model =
        let latinName =
            if model.Slide.Rank = "Family" then model.Slide.CurrentFamily
            else if model.Slide.Rank = "Genus" then model.Slide.CurrentGenus
            else model.Slide.CurrentSpecies
        let title = sprintf "%s: %s" model.Slide.CollectionSlideId latinName
        let subtitle = sprintf "%s - digitised reference slide" model.Collection.Name
        [ 
            Components.breadcrumb (breadcrumbLinks model) model.Slide.CollectionSlideId
            Grid.row [
                Grid.column Medium 8 [
                    Components.galleryViewWindow
                    Components.galleryViewImageList model.Slide.Images
                    citation model latinName
                    License.nonCommercial
                ]
                Grid.column Medium 4 [
                    cropPanel false // TODO propagate isSignedIn to here
                    Components.card "Origin" [
                        identificationMethod model
                        hr []
                        location model
                        time model
                    ]
                    Components.card "Reference Collection Details" [
                        if model.Slide.CurrentTaxonStatus = "accepted" then
                            Components.detailList [
                                "Current Taxon", div [] [
                                    a [ _href <| Urls.MasterReference.family model.Slide.CurrentFamily ] [ str model.Slide.CurrentFamily ]
                                    br []
                                    a [ _href <| Urls.MasterReference.genus model.Slide.CurrentFamily model.Slide.CurrentGenus ] [ str model.Slide.CurrentGenus ]
                                    br []
                                    a [ _href <| Urls.MasterReference.species model.Slide.CurrentFamily model.Slide.CurrentGenus model.Slide.CurrentSpecies ] [ str model.Slide.CurrentSpecies ]
                                ]
                            ]
                        Components.detailList [
                            "Taxon on Slide", div [] [
                                span [] [ str <| sprintf "Family: %s" model.Slide.FamilyOriginal ]
                                br []
                                span [] [ str <| sprintf "Genus: %s" model.Slide.GenusOriginal ]
                                br []
                                span [] [ str <| sprintf "Species: %s" model.Slide.SpeciesOriginal ]
                            ]
                            "Sample Collected By", str model.Slide.CollectorName
                        ]
                    ]
                    Components.card "Slide Preparation" [
                        Components.detailList [
                            "Prepared by", str "Unknown"
                            "Chemical Treatment", str model.Slide.PrepMethod
                            "Slide Creation Date", str model.Slide.PrepYear
                            "Mounting Medium", str model.Slide.Mount
                        ]
                    ]
                ]
            ]
        ] |> Layout.standard [] title subtitle

module Taxon =

    let distributionMap gbifId neotomaId =
        let view = 
            Components.panel "black" [
                Icons.fontawesome "globe"
                encodedText "Distribution"
                div [ _class "btn-group btn-group-toggle"; _data "toggle" "buttons"; _style "float:right" ] [
                    label [ _class "btn btn-primary btn-sm active" ] [
                        input [ _type "radio"; _name "distribution"; _value "recent"; _autocomplete "off"; _checked ]
                        encodedText "Recent"
                    ]
                    label [ _class "btn btn-primary btn-sm" ] [
                        input [ _type "radio"; _name "distribution"; _value "palaeo"; _autocomplete "off"]
                        encodedText "Palaeo"
                    ]
                ]
            ] [
                input [ _hidden; _id "NeotomaId"; _value (sprintf "%i" neotomaId) ]
                input [ _hidden; _id "GbifId"; _value (sprintf "%i" gbifId) ]
                div [ _class "row"; _id "warnings-container"; _style "display:none" ] [
                    Grid.column Medium 12 [
                        div [ _class "alert alert-warning"; _role "alert" ] [
                            p [ _style "display:none"; _id "gbif-warning" ] [
                                Icons.fontawesome "warning"
                                str "GBIF Link: Present distribution currently unavailable for this taxon."
                            ]
                            p [ _style "display:none"; _id "gbif-warning-desc" ] [
                                Icons.fontawesome "warning"
                                str "GBIF Link: No English descriptions can be retrieved for this taxon."
                            ]
                        ]
                    ]
                ]
                div [ _id "modern" ] [
                    div [ _id "map"; _style "height:300px" ] []
                ]
                div [ _id "palaeo"; _style "display:none" ] [
                    div [ _id "palaeo-loading" ] [ encodedText "Fetching from Neotoma..." ]
                    div [ _id "neotoma-map-unavailable"; _style "display:none" ] [ encodedText "Past distributions unavailable from Neotoma." ]
                    span [ _class "timespan" ] [
                        str "Showing "
                        a [ _href "http://neotomadb.org" ] [ str "NeotomaDB" ]
                        str " occurrences from "
                        span [ _id "palaeo-range-low" ] []
                        str " to "
                        span [ _id "palaeo-range-high" ] []
                        str " years before present."
                        span [ _id "palaeo-refresh-time" ] []
                    ]
                    div [ _id "neotoma-map" ] []
                    div [ _id "range" ] []
                ]
            ]
        { View = section [ _id "distribution-map-component" ] [ view ]; Scripts = [] }


    let descriptionCard latinName eolId (eolCache:EncyclopediaOfLifeCache) =
        div [ _class "card" ] [
            div [ _class "card-fixed-height-image" ] [
                img [ _src eolCache.PhotoUrl; _alt <| sprintf "%s (rights holder: %s)"latinName eolCache.PhotoAttribution ]
                if not <| String.IsNullOrEmpty eolCache.PhotoAttribution then
                    span [ _class "image-attribution" ] [ str <| "&copy " + eolCache.PhotoAttribution ]
            ]
            div [ _class "card-block" ] [
                if String.IsNullOrEmpty eolCache.CommonEnglishName
                then h4 [ _class "card-title" ] [ str latinName ]
                else h4 [ _class "card-title" ] [ str eolCache.CommonEnglishName ]
                if String.IsNullOrEmpty eolCache.Description |> not
                then p [ _class "card-text" ] [ rawText (if eolCache.Description.Length > 400
                                                         then eolCache.Description.Substring(0,400)
                                                         else eolCache.Description) ]
                a [ _class "card-link"; _href <| sprintf "http://eol.org/pages/%i/overview" eolId; _target "blank" ]
                    [ str "See more in the Encyclopedia of Life..." ]
            ]
        ]
    
    let taxonomyDefinitionCard (vm:TaxonDetail) =
        let subName = if vm.Rank = "Family" then "Genera" else "Species"
        let completionPercentage =
            if vm.BackboneChildren > 0
            then((double)vm.Children.Length / (double)vm.BackboneChildren) * 100.00
            else 100.00
        Components.panel "white" [
            Icons.fontawesome "book"
            str " Definition"
        ] [
            if vm.Rank <> "Family" then
                dl [ _class "row" ] [
                    dt [ _class "col-sm-3" ] [ str "Parent Taxon" ]
                    dd [ _class "col-sm-9" ] [
                        if vm.Rank = "Genus" then a [ _href <| "/Taxon/" + vm.Family ] [ str vm.Family ]
                        else a [ _href <| "/Taxon/" + vm.Family + "/" + vm.Genus ] [ str vm.Genus ]
                    ]
                ]
            if vm.Rank <> "Species" then
                dl [ _class "row" ] [
                    dt [ _class "col-sm-3" ] [ str subName ]
                    dl [ _class "col-sm-9" ] [
                        ul [ _class "list-inline" ] (vm.Children |> List.sortBy(fun c -> c.Name) |> List.map(fun c ->
                            li [ _class "list-inline-item" ] [ a [ _href <| Urls.MasterReference.taxonById c.Id ] [ str c.Name ] ]
                        ))
                    ]
                ]
                dl [ _class "row" ] [
                    dt [ _class "col-sm-3" ] [ str "Taxonomic Completion" ]
                    dl [ _class "col-sm-9" ] [
                        Components.percentCircle completionPercentage
                        span [] [ str <| sprintf "%i of %i accepted %s" vm.Children.Length vm.BackboneChildren subName ]
                    ]
                ]
            dl [ _class "row" ] [
                dt [ _class "col-sm-3" ] [ str "Global Pollen Project UUID" ]
                dd [ _class "col-sm-9" ] [ str <| vm.Id.ToString() ]
            ]
            dl [ _class "row" ] [
                dt [ _class "col-sm-3" ] [ str "Botanical Reference" ]
                dd [ _class "col-sm-9" ] [
                    if not <| String.IsNullOrEmpty vm.ReferenceName then
                        p [] [
                            strong [] [ str "Reference: " ]
                            if String.IsNullOrEmpty vm.ReferenceUrl
                            then a [ _href vm.ReferenceUrl; _target "blank" ] [ str vm.ReferenceName ]
                            else str vm.ReferenceName
                        ]
                    else span [] [ str "None available." ]
                ]
            ]
        ]

    let connectedDataCard latinName gbifId =
        Components.panel "green" [
            Icons.fontawesome "external-link-alt"
            str " Connected data sources"
        ] [
            p [] [ str "This taxon is currently linked to the following locations." ]
            a [ _class "btn btn-primary"; _target "blank"
                _href <| sprintf "http://www.theplantlist.org/tpl1.1/search?q=%s" latinName ] [ str "The Plant List" ]
            a [ _class "btn btn-primary"; _target "blank"
                _href <| sprintf "http://gbif.org/species/%i" gbifId ] [ str "Global Biodiversity Information Facility" ]
        ]

    let slideListPanel (slides: ReadModels.SlideSummary list) =   
        Components.panel "white" [
            Icons.fontawesome "film"
            encodedText "Digitised Reference Slides"
        ] [
            encodedText (sprintf "We currently hold %i digitised slides." (slides |> List.length))
            ul [ _class "grain-grid columns-8" ] (slides |> List.map (fun s -> 
                li [] [
                    a [ _href (sprintf "/Reference/%A/%s" s.ColId s.SlideId) ] [ 
                        div [ _class "img-container" ] [ img [ _src s.Thumbnail ] ] 
                    ] ] ))
        ]

    let grainListPanel (grains: ReadModels.GrainSummary list) =
        Components.panel "white" [
            Icons.fontawesome "film"
            encodedText "Identified Specimens"
        ] [
            encodedText (sprintf "%i individual grains have been identified." (grains |> List.length))
            ul [ _class "grain-grid columns-8" ] (grains |> List.map (fun s -> 
                li [] [
                    a [ _href <| sprintf "%s/%s" Urls.Identify.identify (s.Id.ToString()) ] [ 
                        div [ _class "img-container" ] [ img [ _src s.Thumbnail ] ] 
                    ] ] ))
        ]
    
    let view (vm:TaxonDetail) =
        let subtitle = sprintf "%s in the Global Pollen Project's Master Reference Collection" vm.Rank
        let distributionMap = distributionMap vm.GbifId vm.NeotomaId
        let breadcrumbs =
            if vm.Rank = "Family" then []
            else if vm.Rank = "Genus" then [
                {Name = vm.Family; Url = Urls.MasterReference.family vm.Family } ]
            else [
                {Name = vm.Family; Url = Urls.MasterReference.family vm.Family }
                {Name = vm.Genus; Url = Urls.MasterReference.genus vm.Family vm.Genus } ]
        [
            Components.breadcrumb ( {Name = "Master Reference Collection"; Url = Urls.MasterReference.root }::breadcrumbs) vm.LatinName
            Grid.row [
                Grid.column Medium 6 [
                    slideListPanel vm.Slides
                    if not vm.Grains.IsEmpty then grainListPanel vm.Grains
                ]
                Grid.column Medium 6 [
                    distributionMap.View
                    descriptionCard vm.LatinName vm.EolId vm.EolCache
                    taxonomyDefinitionCard vm
                    connectedDataCard vm.LatinName vm.GbifId
                ]
            ]
        ] |> Layout.standard distributionMap.Scripts vm.LatinName subtitle


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

    let specificEphitet (latinName:string) =
        latinName.Split(' ') |> Array.last
    
    let taxonCard (summary:TaxonSummary) =
        let taxonLink =
            match summary.Rank with
            | "Family" -> Urls.MasterReference.family summary.Family
            | "Genus" -> Urls.MasterReference.genus summary.Family summary.Genus
            | "Species" -> Urls.MasterReference.species summary.Family summary.Genus (specificEphitet summary.Species)
            | _ -> Urls.notFound

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
            if summary.DirectChildren.Length > 0 then
                let taxonId = summary.Id.ToString()
                div [ _class "taxon-toggle" ] [
                    a [ _class "subtaxa-button"; _role "button"
                        _data "toggle" "collapse"; _data "target" (sprintf "#taxon-%s" taxonId)] [
                        Icons.fontawesome "list"
                    ]
                ]
                ul [ _id <| sprintf "taxon-%s" taxonId; _class "panel-collapse collapse"
                     _role "tabpanel" ] (summary.DirectChildren |> List.sortBy(fun t -> t.Name) |> List.map(fun t ->
                    li [] [ a [ _href <| Urls.MasterReference.taxonById t.Id ] [ str t.Name ] ] ))
        ]
    
    let rankToggle isActive rank letter =
        let styling = if isActive then "header-toggle header-toggle-active" else "header-toggle"
        a [ _class styling; _href <| Urls.MasterReference.rootBy rank letter ] [ encodedText rank ]
    
    let alphabetIndex activeLetter activeRank =
        [
            rankToggle (activeRank = "Family") "Family" activeLetter
            rankToggle (activeRank = "Genus") "Genus" activeLetter
            rankToggle (activeRank = "Species") "Species" activeLetter
            div [ _class "alphabet-index" ] (['A'..'Z'] |> List.map (fun l ->
                let isActive = if activeLetter = l.ToString() then "header-toggle-active" else ""
                a [ _class isActive; _href <| Urls.MasterReference.rootBy activeRank (l.ToString()) ]
                    [ encodedText (sprintf "%c" l) ] )
            |> List.append [ a [ _class (if activeLetter = "" then "header-toggle-active" else "")
                                 _href <| Urls.MasterReference.rootBy "" activeRank ] [ str "All" ] ])
        ]

    let index activeLetter activeRank (vm:PagedResult<TaxonSummary>) =
        [
            Components.breadcrumb [] "Master Reference Collection"
            Grid.row [ Grid.column Medium 12 (alphabetIndex activeLetter activeRank) ]
            Grid.row (vm.Items |> List.map (fun t -> Grid.column Medium 6 [ taxonCard t ] ))
            Components.pagination vm (Urls.MasterReference.rootBy activeRank activeLetter)
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
                h4 [ _class "card-title" ] [ a [ _href <| sprintf "/Reference/%s/%i" (refCol.Id.ToString()) refCol.Version ] [ encodedText refCol.Name ] ]
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
        |> Layout.standard [] "Individual Reference Collections" "Digitised and un-digitised reference material from individual collections and institutions."

    let tableView (vm:ReferenceCollectionDetail) =
        [
            link [ _rel "stylesheet"; _href "https://cdn.datatables.net/1.10.21/css/dataTables.bootstrap4.min.css" ]
            Components.breadcrumb [{Name = "Individual Reference Collections"; Url = Urls.individualCollections} ] vm.Name
            p [] [ encodedText vm.Description ]
            div [ _class "card-group mb-4" ] [
                div [ _class "card" ] [
                    div [ _class "card-block" ] [
                        h4 [ _class "card-title" ] [ encodedText "Contributors" ]
                        p [] [ strong [] [ encodedText "Curator:" ]; encodedText <| sprintf "%s %s" vm.CuratorFirstNames vm.CuratorSurname ]
                        p [] ((strong [] [encodedText "Digitised by:"])::(vm.Digitisers |> List.map(fun d -> span [] [ str d ])))
                        p [] ((strong [] [encodedText "Material contributed by:"])::(vm.Collectors |> List.map(fun d -> span [] [ str d ])))
                    ]
                ]
                div [ _class "card" ] [
                    div [ _class "card-block" ] [
                        h4 [ _class "card-title" ] [ encodedText "Access to Material" ]
                        p [ _class "card-text" ] [
                            match vm.AccessMethod with
                            | "digital" -> span [] [ str "This collection is only available to view in digitally. The curator does not have access to the original physical reference slides." ]
                            | "institution" -> span [] [
                                str "The physical reference slides are located within an institution: "
                                a [ _href vm.InstitutionUrl ] [ str vm.Institution ] ]
                            | _ -> span [] [ str "This is a personal collection. Access to the physical reference material may be granted on request. Please contact the curator for more information." ]
                        ]
                    ]
                ]
                div [ _class "card" ] [
                    div [ _class "card-block" ] [
                        h4 [ _class "card-title" ] [ encodedText "Citation" ]
                        p [ _class "card-text" ] [ str <| sprintf "%s, %s, %s (Version %i). Digitised palynological Reference Collection accessed via globalpollenproject.org on %s"
                                                               vm.CuratorSurname vm.CuratorFirstNames vm.Name vm.Version (DateTime.Now.ToLongDateString()) ]
                    ]
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
                    tr [ _class <| sprintf "taxon-status-%s" s.CurrentTaxonStatus ] [
                        th [ _scope "row" ] [ encodedText s.CollectionSlideId ]
                        if s.CurrentTaxonStatus = "accepted" && s.IsFullyDigitised then
                            td [] [ a [ _href <| Urls.MasterReference.family s.CurrentFamily ] [ encodedText s.CurrentFamily ]
                                    em [ _class "table-orig-name" ] [ str s.FamilyOriginal ] ]
                            td [] [ a [ _href <| Urls.MasterReference.genus s.CurrentFamily s.CurrentGenus ] [ encodedText s.CurrentGenus ]
                                    em [ _class "table-orig-name" ] [ str s.GenusOriginal ] ]
                            td [] [ a [ _href <| Urls.MasterReference.species s.CurrentFamily s.CurrentGenus s.CurrentSpecies ] [ encodedText s.CurrentSpecies ]
                                    em [ _class "table-orig-name" ] [ str s.SpeciesOriginal ] ]
                        else
                            td [] [ em [ _class "table-orig-name" ] [ encodedText s.CurrentFamily ] ]
                            td [] [ em [ _class "table-orig-name" ] [ encodedText s.CurrentGenus ] ]
                            td [] [ em [ _class "table-orig-name" ] [ encodedText s.CurrentSpecies ] ]
                        td [] [
                            if s.IsFullyDigitised
                            then span [] [ str <| sprintf "Yes: %i image(s)" s.Images.Length ]
                            else span [] [ str "No" ]
                        ]
                        td [] [
                            if s.IsFullyDigitised then
                                a [ _href <| Urls.Collections.slide vm.Id s.CollectionSlideId
                                    _class "btn btn-secondary" ] [ str "View" ]
                        ]
                    ]
                ))
            ]
        ] |> Layout.standard [ "https://cdn.datatables.net/1.10.21/js/jquery.dataTables.min.js"
                               "https://cdn.datatables.net/1.10.21/js/dataTables.bootstrap4.min.js"
                               "$(document).ready(function() { $('#reference-table').DataTable({paging: false, order: [[ 2, 'asc' ]]});})" ]
                               vm.Name "Individual Reference Collection"


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
    
    let taxonDropdownBoxes =
        Grid.row [
            Grid.column Small 3 [
                input [ attr "data-bind" "value: family, event: { blur: capitaliseFirstLetter($element),
                        keyup: suggest($element, 'Family') }"
                        _type "text"; _id "original-Family"; _class "form-control"
                        _autocomplete "off"; _placeholder "Family" ]
                ul [ _class "dropdown-menu taxon-dropdown"; _id "FamilyList"
                     _style "display:none" ] []
            ]
            Grid.column Small 3 [
                input [ attr "data-bind" "value: genus, enable: rank() != 'Family',
                        event: { blur: capitaliseFirstLetter($element),
                        keyup: suggest($element, 'Genus'),
                        blur: disable('Genus') }"
                        _type "text"; _id "original-Genus"; _class "form-control"
                        _autocomplete "off"; _placeholder "Genus" ]
                ul [ _class "dropdown-menu taxon-dropdown"; _id "GenusList"
                     _style "display:none" ] []
            ]
            Grid.column Small 3 [
                input [ attr "data-bind" "value: species, disable: rank() != 'Species',
                        event: { blur: disable('Species'), keyup: suggest($element, 'Species') }"
                        _type "text"; _id "original-Species"; _class "form-control"
                        _autocomplete "off"; _placeholder "Species" ]
                ul [ _class "dropdown-menu taxon-dropdown"; _id "SpeciesList"
                     _style "display:none" ] []
            ]
            Grid.column Small 3 [
                input [ attr "data-bind" "value: author, disable: rank() != 'Species', event: { blur: capitaliseFirstLetter($element) }"
                        _type "text"; _class "form-control"; _autocomplete "off"; _placeholder "Auth." ]
            ]
        ]
    
    let taxonValidationSpace =
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
                    li [ attr "data-bind" "text: latinName + ' ' + namedBy + ' (' + taxonomicStatus + ' name)'" ] []
                ]
            ]
            div [ attr "data-bind" "visible: newSlideTaxonStatus().length == 0" ] [
                p [] [
                    Icons.fontawesome "frown-o"
                    str " Taxon was not recognised by our taxonomic backbone."
                ]
            ]
        ]
    
    
    let identifyForm grainId = section [] [
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
            taxonDropdownBoxes
            small [] [ str "Authorship is optional. The given name will be traced within our taxonomic backbone to the currently accepted name." ]
            taxonValidationSpace
            input [ _hidden; _name "TaxonId"; _id "TaxonId"; attr "data-bind" "value: currentTaxon" ]
            input [ _hidden; _name "GrainId"; _id "GrainId"; _value <| grainId ]
            button [ _class "btn btn-primary"; _style "display:block"
                     attr "data-bind" "click: validateAndSubmit, enable: isValidSearch" ] [
                str "Identify"
            ]
        ]]
    
    let view absoluteUrl currentUserId (vm:GrainDetail) =
        let myIdentification =
            match currentUserId with
            | None -> None
            | Some userId -> vm.Identifications |> Seq.tryFind(fun i -> i.User = userId)
        [
            Components.breadcrumb [
                { Name = "Pollen of Unknown Identity"; Url = Urls.Identify.root }
            ] "Unidentified Specimen"
            Grid.row [
                Grid.column Medium 6 [
                    Components.galleryViewWindow
                    Components.galleryViewImageList vm.Images
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
                                          _src <| sprintf "https://api.mapbox.com/styles/v1/mapbox/streets-v10/static/pin-s-a+9ed4bd(%f,%f)/%f,%f,3/560x200@2x?access_token=%s" vm.Longitude vm.Latitude vm.Longitude vm.Latitude Settings.mapboxToken ]
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
                                | None -> identifyForm <| vm.Id.ToString()
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
            Components.breadcrumb [] "Statistics"
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
                    h3 [ _id "contributions" ] [ str "Contributions" ]
                    p [] [ str "Once a grain has been identified and transferred to the online reference collection all participating users will gain points
                                both for themselves and their affiliated institutions. The number of points will be determined by the current score placed
                                on an individual grain as as well as the level of taxonomic identification (i.e. more points will be awarded for a species
                                level than a genus level identification)." ]
                    Grid.row [
                        Grid.column Medium 6 [
                            div [ _class "card" ] [
                                div [ _class "card-block primary-header" ] [
                                    h4 [ _class "card-title" ] [
                                        Icons.fontawesome "users"
                                        str "Top Groups"
                                    ]
                                ]
                                ul [ _class "list-group list-group-flush" ] [
                                    li [ _class "list-group-item" ] [ str "Contribution scores are temporarily deactivated" ]
                                ]
                            ]
                            div [ _class "card" ] [
                                div [ _class "card-block primary-header" ] [
                                    h4 [ _class "card-title" ] [
                                        Icons.fontawesome "user"
                                        str "Top People"
                                    ]
                                ]
                                ul [ _class "list-group list-group-flush" ] [
                                    li [ _class "list-group-item" ] [ str "Contribution scores are temporarily deactivated" ]
                                ]
                            ]
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
                    i [ _class "far fa-meh"; _style "font-size: 6em;margin-bottom: 0.25em;" ] []
                    h4 [] [ encodedText "Sorry, we couldn't find what you were looking for." ]
                    p [] [ encodedText "We've recently revamped the Global Pollen Project. We hope we haven't broken any links, but if you find any please let us know." ]
                    a [ _href Urls.home ] [ encodedText "Get me back to the homepage" ]
                    p [] [ encodedText "- or -" ]
                    p [] [ encodedText "Lookup a taxon in our reference collection" ]
                    autocomplete.View
                ]
            ]
        ] 
        |> Layout.master autocomplete.Scripts "Not Found"

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
        ] |> Layout.master [] title

    let error = statusLayout "bomb" "Sorry, there's been a slight snag..." "Your request didn't go through. You can try again, or please come back later. If this happens often, please let us know."
    
    let maintenance = statusLayout "wrench" "Under Temporary Maintenance" "We're working on some changes, which have required us to take the Pollen Project offline. We will be back later today, so sit tight."

    let denied = statusLayout "exclamation-triangle" "Access Denied" "You're not authorised to access this content."

module Tools =

    let sidebar =
        div [ _class "sticky-sidebar" ] [
            label [] [ str "Tools" ]
            nav [ _id "sidebar"; _class "nav flex-column" ] [
                a [ _class "nav-link"; _href ("#botanical-names") ] [ str "Botanical Name Tracer" ]
            ]
        ]
    
    let main =
        [
            Grid.row [
                Grid.column Medium 3 [ sidebar ]
                Grid.column Medium 9 [
                    div [ _id "botanical-lookup-component" ] [
                        p [] [ str "The Global Pollen Project incorporates a taxonomic backbone - a checklist of global plant families, genera, and species." ]
                        p [] [
                            str "Lookup rank:"
                            select [ attr "data-bind" "value: rank"
                                     _class "form-control form-control-sm inline-dropdown" ] [
                                option [ _value "Family" ] [ str "Family" ]
                                option [ _value "Genus" ] [ str "Genus" ]
                                option [ _value "Species" ] [ str "Species" ]
                            ]
                        ]
                        Identify.taxonDropdownBoxes
                        small [] [ str "Authorship is optional. The given name will be traced within our taxonomic backbone to the currently accepted name." ]
                        button [ _class "btn btn-secondary"; _style "display:block"
                                 _data "bind" "click: requestValidation, enable: isValidSearch" ] [ str "Trace" ]
                        Identify.taxonValidationSpace
                    ]
                ]
            ]
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
            | None -> p [] [ str "Your profile has not been created" ]
            | Some profile ->
                Grid.row [
                    Grid.column Medium 3 [
                        img [ _alt "Profile image"; _src "/images/pollen3.jpg"; _class "profile-image" ]
                        h3 [] [ str <| sprintf "%s %s" profile.FirstName profile.LastName ]
                        div [] (profile.Groups |> List.map(fun s -> p [] [ str s ]))
                        div [] [ span [ _class "badge badge-primary" ] [ str <| sprintf "%f score" profile.Score ] ]
                    ]
                    Grid.column Medium 9 [
                        ul [ _class "nav nav-tabs"; _id "myTab"; _role "tablist" ] [
                            li [ _class "nav-item" ] [
                                a [ _class "nav-link active"; _id "active-tab"; _data "toggle" "tab"
                                    _href "#active"; _role "tab" ] [ str "Active" ]
                            ]
                            li [ _class "nav-item" ] [
                                a [ _class "nav-link"; _id "timeline-tab"; _data "toggle" "tab"
                                    _href "#timeline"; _role "tab" ] [ str "Timeline" ]
                            ]
                        ]
                    ]
                ]
        ] |> Layout.standard [] "Your Profile" "Your Profile"
    