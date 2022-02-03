module GlobalPollenProject.Lab.Client.Views

open System
open Bolero.Html
open Elmish
open GlobalPollenProject.Lab.Client.Types
open ReadModels

// Some helpers for making code cross-comparable to GiraffeViewEngine
let _class = attr.``class``
let _data k v = sprintf "data_%s" k => v
let _aria k v = sprintf "aria_%s" k => v
let _type = attr.``type``
let _role v = "role" => v

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


module Components =
    
    let modal name content footer =
        div [ _class "modal is-active"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _class "modal-background" ] []
            div [ _class "modal-card" ] [
                header [ _class "modal-card-head" ] [
                        h5 [ _class "modal-card-title" ] [ text name ]
                        button [ _type "button"; _class "delete"; _aria "label" "Close" ] [ span [ _aria "hidden" "true"] [ text "&times" ] ]
                ]
                section [ _class "modal-card-body" ] content
                section [ _class "modal-card-foot" ] footer
            ]
        ]

    let formGroupRow = div [ _class "form-group-row" ]
    let formGroup = div [ _class "form-group-row" ]
    let formField = div [ _class "field" ]
    let formHelpText s = small [ _class "form-text text-muted" ] [ text s ]
    let formLabel s = label [ _class "label" ] [ text s ]


module Viewer =

    open Bolero
    open Microsoft.JSInterop
    open Microsoft.AspNetCore.Components

    type ImageStackType =
        | Focus
        | Static
    
    type MicroscopeViewerModel = {
        StackType: ImageStackType
        ImageUris: string[]
    }

    // Images should be read in as bse64s before being passed to the viewer
    
    let createImageViewer =
        
        // PROCESS Reset microscope and magnification
        // VIEW Make a datepicker on the input "digitsedYearFocus"
        // VIEW If a viewer exists, dispose previous and load a new one.
        // VIEW if less than 2 images for focus image:
        // VIEW : alert("You must upload at least 2 images (shift-click/ctrl-click files to select multiple)")
        // PROCESS Load files into base64
        // VIEW nd pass to new viewer component            
        2    
    
    /// Renders a viewer for microscopic images, which only refreshes when
    /// the underlying loaded images change.
    type MicroscopeViewerCanvas() =
        inherit ElmishComponent<MicroscopeViewerModel, float>()

        let viewerRef = ElementReferenceBinder()
        
        [<Inject>]
        member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

        /// Only reload viewer when the images change
        override this.ShouldRender(oldModel, newModel) =
            oldModel.ImageUris <> newModel.ImageUris
        
        member this.makeViewer model =
            this.JSRuntime.InvokeVoidAsync("Viewer", viewerRef.Ref, viewerRef.Ref.Id + "-viewer-canvas",
                                           500, 500, model.ImageUris).AsTask() |> Async.AwaitTask
        
        /// Loads the viewer component in a single div, which will contain the image canvas.
        /// Also renders scale bar and focus slider if appropriate.
        /// TODO Set width of container to width available
        override this.View model dispatch =
            if model.ImageUris.Length > 0 then
                div [ attr.bindRef viewerRef
                      on.event "loadedImages" (fun e -> printfn "loaded images: %A" e)
                      on.event "zoomed" (fun e -> printfn "zoomed: %A" e)
                      on.event "imagesMismatchedSize" (fun e -> printfn "images were mismatched: %A" e)
                      
                      on.async.load (fun _ ->
                          if model.ImageUris.Length = 1
                          then this.makeViewer model
                          else
                              async.Combine(
                                  this.makeViewer model,
                                  this.JSRuntime.InvokeVoidAsync("FocusSlider", viewerRef.Ref,
                                                                  viewerRef.Ref.Id + "-viewer-slider").AsTask() |> Async.AwaitTask)
                              )] []
            else concat []


module Partials =

//(vm:StartCollectionRequest option) 
    let addCollectionModal draft dispatch =
        let vm =
            match draft with
            | None -> Requests.Empty.addCollection
            | Some d ->
                match d with
                | DraftCollection col -> col
                | _ -> Requests.Empty.addCollection
        Components.modal "Digitise a Collection" [
            p [] [ text "Please tell us about the reference collection you wish to digitise. You can edit this information later if necessary." ]
            div [] [] // TODO Validation errors
            Components.formField [
                Components.formLabel "Collection Name"
                input [ bind.input.string vm.Name (fun s -> { vm with Name = s } |> ChangeNewCollection |> dispatch); _class "input" ]
                Components.formHelpText "Use a name specific to the collection."
            ]
            Components.formField [
                Components.formLabel "Description"
                textarea [ bind.input.string vm.Description (fun s -> { vm with Description = s } |> ChangeNewCollection |> dispatch); _class "input"; attr.rows 3 ] []
                Components.formHelpText "Your collection description could include the motivation for creating the collection, geographical coverage, or the nature of the material, for example."
            ]
            Components.formGroupRow [
                Components.formLabel "Who is the curator?"
                div [ _class "col-sm-4" ] [
                    input [ bind.input.string vm.CuratorFirstNames (fun s -> { vm with CuratorFirstNames = s } |> ChangeNewCollection |> dispatch); attr.placeholder "Forenames"; _class "input" ] ]
                div [ _class "col-sm-4" ] [
                    input [ bind.input.string vm.CuratorSurname (fun s -> { vm with CuratorSurname = s } |> ChangeNewCollection |> dispatch); attr.placeholder "Surname"; _class "input" ] ]
            ]
            Components.formField [
                label [] [ text "Email Address of Curator, for Enquiries" ]
                input [ bind.input.string vm.CuratorEmail (fun s -> { vm with CuratorEmail = s } |> ChangeNewCollection |> dispatch); _type "Email"; _class "input" ]
                Components.formHelpText "Please specify a contact email so that users can find out further information about this collection."
            ]
            h4 [] [ text "Physical Access to Material" ]
            hr []
            p [] [ text "Please tell us the level of access that the curator has to the original reference material." ]
            div [ _class "form-check" ] [
                label [ _class "form-check-label" ] [
                    input [bind.change.string vm.AccessMethod (fun _ -> { vm with AccessMethod = "digital" } |> ChangeNewCollection |> dispatch)
                           _class "form-check-input"; _type "radio"; attr.name "accessMethod"; attr.id "digital"; attr.value "digital" ]
                    strong [] [ text "Digital Only." ]
                    text "This could be a meta-collection made from many dispersed sources, or reference material to which the owner of the images no longer has physical access."
                ]
                label [ _class "form-check-label" ] [
                    input [ bind.change.string vm.AccessMethod (fun _ -> { vm with AccessMethod = "institution" } |> ChangeNewCollection |> dispatch);
                                _class "form-check-input"; _type "radio"; attr.name "accessMethod"; attr.id "institution"; attr.value "institution" ]
                    strong [] [ text "Institution." ]
                    text "The reference material is housed within an institution."
                ]
                label [ _class "form-check-label" ] [
                    input [ bind.change.string vm.AccessMethod (fun _ -> { vm with AccessMethod = "private" } |> ChangeNewCollection |> dispatch)
                            _class "form-check-input"; _type "radio"; attr.name "accessMethod"; attr.id "private"; attr.value "private" ]
                    strong [] [ text "Personal / Private Collection." ]
                    text "The physical reference material forms a private collection of the curator. Access to the material may be negotiated with the curator."
                ]
            ]
            div [ if vm.AccessMethod <> "institution" then attr.hidden "hidden" ] [
                div [ _class "form-group" ] [
                    label [] [ text "Institution Where Collection is Located" ]
                    input [ bind.input.string vm.Institution (fun s -> { vm with Institution = s } |> ChangeNewCollection |> dispatch);
                            if vm.AccessMethod <> "institution" then attr.disabled "disabled"; _class "form-control" ]
                    small [ _class "form-text text-muted" ] [ text "Where are the physical slides for this collection located? Be sure to include any specific identifiers, for example a group or department name." ]
                ]
                div [ _class "form-group" ] [
                    label [] [ text "Institution Website Address" ]
                    input [ bind.input.string vm.InstitutionUrl (fun s -> { vm with InstitutionUrl = s } |> ChangeNewCollection |> dispatch);
                            if vm.AccessMethod <> "institution" then attr.disabled "disabled"; _type "url"; _class "form-control" ]
                    small [ _class "form-text text-muted" ] [ text "If there is further information on your own website, paste a link here." ]
                ]
            ]
        ] [ button [ on.click (fun _ -> SendStartCollection |> dispatch); _type "button"; _class "button is-success" ] [ text "Start" ] ]

    
    let slideDetail slide dispatch =
        let taxonName = sprintf "%s %s %s %s" slide.CurrentFamily slide.CurrentGenus slide.CurrentSpecies slide.CurrentSpAuth
        section [] [
            match slide.IsFullyDigitised with
            | true ->
                div [ _class "alert alert-info" ] [
                    Icons.fontawesome "info-circle"
                    text "This slide has not been fully digitised. Upload at least one image"
                ]
            | false ->
                div [ _class "alert alert-success" ] [
                    Icons.fontawesome "check-circle"
                    text "Fully digitised"
                ]
            Grid.container [
                ul [ _class "grain-grid" ] [
                    forEach slide.Images <| fun image ->
                    li [] [
                        div [ _class "img-container" ] [
                            a [] [ img [ attr.src image.FramesSmall.Head; attr.style "max-width: 100%; max-height: 100%;" ] ]
                        ]
                    ]
                ]
            ]
            p [] [ text <| sprintf "The original reference slide has the taxonomic identification: %s %s %s" slide.FamilyOriginal slide.GenusOriginal slide.SpeciesOriginal ]
            p [] [ text <| "The most current name for this taxon is: " + taxonName ]
            p [] [ text "If this slide contains errors, you can void it. This will remove the slide from the collection and allow re-entry of another slide with the correct information." ]
            button [ _type "button"; on.click(fun _ -> SendVoidSlide(slide.CollectionId, slide.CollectionSlideId) |> dispatch); _class "btn btn-danger" ] [
                Icons.fontawesome "trash-o"
                text "Void Slide" ]
        ]
    
    let isValidFocusRequest vm =
        true
    
    
    /// Ensures that only image files can be uploaded.
    let validateImages (files:BlazorInputFile.IFileListEntry[]) =
        // TODO Check that files are images only (e.g. image/* or .png)
        files
        |> Array.map(fun f ->
            use reader = new System.IO.StreamReader(f.Data)
            reader.ReadToEnd()) //|> Async.AwaitTask)
        //|> Async.Parallel
    
    let focusImage (vm:SlideImageRequest) (options:ImageDraftModel) (calibrations:Calibration list) validationErrors dispatch =
        let loadedFocusImages = vm.IsFocusImage && vm.FramesBase64.Length > 0
        section [] [
            ul [] [ forEach validationErrors <| fun e -> li [] [ text e ] ]
            match calibrations |> Seq.isEmpty with
            | true ->
                div [ _class "alert alert-danger" ] [
                    strong [] [ text "Error" ]
                    text " - no microscope calibrations have been configured"
                ]
            | false ->
                div [] [
                    h5 [] [ text "Upload a focusable image" ]
                    p [] [ text "Select all focus level images below" ]
                    comp<BlazorInputFile.InputFile> [ attr.accept "image/*"; attr.multiple "multiple"
                                                      attr.callback "OnChange" (fun (files:BlazorInputFile.IFileListEntry[]) ->
                        ({ vm with FramesBase64 = validateImages files |> Array.toList }, options) |> ChangeDraftSlideImage |> dispatch) ] []
                    ecomp<Viewer.MicroscopeViewerCanvas,_,_> [] {
                        ImageUris = vm.FramesBase64 |> List.toArray
                        StackType = Viewer.ImageStackType.Focus
                    } (fun f -> ({vm.}) dispatch)
                    if loadedFocusImages then
                        div [ _class "card"] [
                            div [ _class "card-header" ] [
                                text "Select your configured microscope + magnification level"
                            ]
                            div [ _class "card-block" ] [
                                if calibrations |> Seq.isEmpty |> not then
                                    Components.formGroupRow [
                                        Grid.column Small 6 [
                                            label [ attr.``for`` "microscope-down" ] [ text "Microscope" ]
                                            div [ _class "dropdown" ] [
                                                button [ _class "btn btn-secondary dropdown-toggle calibration-dropdown"; _type "button"
                                                         attr.id "microscope-dropdown"; _data "toggle" "dropdown" ] [ text <| if options.SelectedCalibration.IsSome then options.SelectedCalibration.Value.Name else "None" ]
                                                div [ _class "dropdown-menu calibration-dropdown-list" ] [
                                                    forEach calibrations <| fun cal ->
                                                    a [ attr.value cal.Name; on.click (fun _ -> (vm, { SelectedCalibration = Some cal; SelectedMagnification = None }) |> ChangeDraftSlideImage |> dispatch)
                                                        _class "dropdown-item calibration-option" ] [ text cal.Name ]
                                                ]
                                            ]
                                        ]
                                        Grid.column Small 6 [
                                            label [ attr.``for`` "magnification-dropdown" ] [ text "Magnification" ]
                                            match options.SelectedCalibration with
                                            | None -> ()
                                            | Some cal ->
                                                div [ _class "dropdown" ] [
                                                    button [ _class "btn btn-secondary dropdown-toggle calibration-dropdown"; _type "button"; attr.id "magnification-dropdown"; _data "toggle" "dropdown" ] [ text selectedMicroscopeName ]
                                                    div [_class "dropdown-menu calibration-dropdown-list" ] [
                                                        forEach cal.Magnifications <| fun mag ->
                                                        a [ attr.value mag; on.click (fun _ -> (vm, { options with SelectedMagnification = Some mag }) |> ChangeDraftSlideImage |> dispatch)
                                                            _class "dropdown-item calibration-option" ] [ text <| sprintf "%ix" mag.Level ]
                                                    ]
                                                ]
                                        ]
                                    ]
                            ]
                        ]
                ]
            if loadedFocusImages then
                let digitisedYearValue = if vm.DigitisedYear.HasValue then vm.DigitisedYear.Value else DateTime.Now.Year
                div [ _class "card" ] [
                    div [ _class "card-block" ] [
                        Components.formGroupRow [
                            label [ attr.``for`` "digitisedYearFocus"; _class "col-sm-4 col-form-label" ] [ text "Year Image Taken" ]
                            Grid.column Small 8 [
                                input [ bind.change.int digitisedYearValue (fun i -> ({ vm with DigitisedYear = Nullable(i) }, options) |> ChangeDraftSlideImage |> dispatch ); attr.id "digitisedYearFocus"; _class "form-control" ]
                                small [ _class "help" ] [ text "In which calendar year was this image taken?" ]
                            ]
                        ]
                    ]
                ]
                div [ _class "card" ] [
                    div [ _class "card-block" ] [
                        Grid.row [
                            div [ _class "col-sm-9 prgoress"; attr.id "slidedetail-focus-upload-progress" ] [
                                // TODO Upload progress bar
                                //div [ _koBind "visible: uploadPercentage, style: { width: function() { if (uploadPercentage() != null) { return uploadPercentage() + '%'; } } }"; _class "progress-bar progress-bar-striped progress-bar-animated"; _id "slidedetail-focus-upload-progressbar"; _style "width:100%" ] []
                            ]
                            Grid.column Small 3 [
                                button [ on.click (fun _ -> SendUploadImage |> dispatch); if isValidFocusRequest vm |> not then attr.disabled "disabled"
                                         _type "button"; _class "btn btn-primary" ] [ text "Upload Image" ]
                            ]
                        ]
                    ]
                ]
        ]
    
    let staticImage (vm:SlideImageRequest) validationErrors dispatch =
        let haveLoadedStaticImage = (not vm.IsFocusImage) && vm.FramesBase64.Length = 1
        section [] [
            ul [] [ forEach validationErrors <| fun e -> li [] [ text e ] ]
            h5 [] [ text "Upload a static image" ]
            p [] [ text "A static image uses a floating calibration to discern size within the image. You must complete the calibration step for every image." ]
            comp<BlazorInputFile.InputFile> [ attr.accept "image/*"; attr.multiple "multiple"
                                              attr.callback "OnChange" (fun (files:BlazorInputFile.IFileListEntry[]) ->
                { vm with FramesBase64 = validateImages files |> Array.toList } |> ChangeDraftSlideImage |> dispatch) ] []
            ecomp<Viewer.MicroscopeViewerCanvas,_,_> [] {
                ImageUris = vm.FramesBase64 |> List.toArray
                StackType = Viewer.ImageStackType.Static
            } (fun _ -> dispatch)
            if haveLoadedStaticImage then
                div [ _class "card" ] [
                    div [ _class "card-header" ] [
                        text "Draw a line on the loaded image of known length"
                    ]
                    div [ _class "card-block" ] [
                        Components.formGroupRow [
                            Grid.column Small 3 [
                                button [ on.click (fun _ -> activatesMeasuringLine()); _type "button"; _class "btn btn-primary"; attr.id "slidedetail-draw-line-button" ] [ text "Draw Line" ]
                            ]
                            label [ attr.``for`` "measuredDistance"; _class "col-sm-3 col-form-label" ] [ text "Measured Distance" ]
                            Grid.column Small 6 [
                                div [ _class "input-group" ] [
                                    input [ bind.change.float vm.MeasuredDistance.Value (fun _ -> failwith "TODO Not implemented"); attr.id "measuredDistance"; _class "form-control" ]
                                    span [ _class "input-group-addon" ] [ text "μm" ]
                                ]
                                small [ _class "help" ] [ text "Enter the length of your measurement line in micrometres (μm)" ]
                            ]
                        ]
                    ]
                ]
                div [ _class "card" ] [
                    div [ _class "card-block" ] [
                        div [ _class "form-group-row" ] [
                            label [ attr.``for`` "digitisedYearStatic"; _class "col-sm-4 col-form-label" ] [ text "Year Image Taken" ]
                            div [ _class "col-sm-8" ] [
                                input [ bind.change.int vm.DigitisedYear (fun _ -> failwith "TODO Not implemented"); attr.id "digitisedYearStatic"; _class "form-control" ]
                                small [ _class "help" ] [ text "In which calendar year was this image taken?" ]
                            ]
                        ]
                    ]
                ]s
                div [ _class "card" ] [
                    div [ _class "card-block" ] [
                        Grid.row [
                            div [ _class "col-sm-9 progress" ] [
                                // TODO Loading bar
                                //div [ _koBind "visible: uploadPercentage, style: { width: function() { if (uploadPercentage() != null) { return uploadPercentage() + '%'; } } }"; _class "progress-bar progress-bar-striped progress-bar-animated"; _id "slidedetail-static-upload-progressbar"; _style "width:100%" ] []
                            ]
                            div [ _class "col-sm-3" ] [
                                button [ on.click (fun _ -> SendUploadImage |> dispatch )
                                         if not isValidRequest then attr.disabled "disabled"; _class "btn btn-primary" ] [ text "Upload Image" ]
                            ]
                        ]
                    ]
                ]
        ]
    
    
    let slideTabbedModal slide (vm:Model) dispatch =
        let title = sprintf "Slide %s: %s %s %s" slide.CollectionSlideId slide.FamilyOriginal slide.GenusOriginal slide.SpeciesOriginal
        Components.modal title [
            ul [ _class "nav nav-tabs" ] [
                li [ _class "nav-item" ] [ a [ attr.href "#"; on.click(fun _ -> SlideDetailView(slide.CollectionId.ToString(), slide.CollectionSlideId) |> SetPage |> dispatch)
                                               match vm.page with | SlideDetailView _ -> _class "nav-link" | _ -> _class "nav-link" ] [ text "Overview" ] ]
                li [ _class "nav-item" ] [ a [ attr.href "#"; on.click(fun _ -> SlideStaticImage(slide.CollectionId.ToString(), slide.CollectionSlideId) |> SetPage |> dispatch)
                                               match vm.page with | SlideStaticImage _ -> _class "nav-link" | _ -> _class "nav-link"  ] [ text "Upload static image" ] ]
                li [ _class "nav-item" ] [ a [ attr.href "#"; on.click(fun _ -> SlideFocusImage(slide.CollectionId.ToString(), slide.CollectionSlideId) |> SetPage |> dispatch)
                                               match vm.page with | SlideFocusImage _ -> _class "nav-link" | _ -> _class "nav-link"  ] [ text "Upload focus image" ] ]
            ]
            match vm.page with
            | SlideDetailView _ -> slideDetail slide dispatch
            | SlideStaticImage _ -> staticImage vm [] dispatch
            | SlideFocusImage _ -> focusImage vm c [] dispatch
            | _ -> div [] [ text "Error" ]
        ] []

    let requiredSymbol = span [ _class "required-symbol" ] [ text "*" ]

    let capitalise (s: string) =
        s |> Seq.mapi (fun i c -> match i with | 0 -> (Char.ToUpper(c)) | _ -> c)  |> String.Concat
    
    let isValidTaxonSearch rank (family:string) (genus:string) (species:string) =
        if rank = Family && family.Length > 0 then true
        else if rank = Genus && family.Length > 0 && genus.Length > 0 then true
        else if rank = Species && family.Length > 0 && genus.Length > 0 && species.Length > 0 then true
        else false
    
    let recordSlide currentDraft dispatch =
        let vm, rank, newSlideTaxonStatus =
            match currentDraft with
            | None -> Requests.Empty.recordSlide, Family, None
            | Some draft ->
                match draft with
                | DraftSlide (v,r,n) -> v,r,n
                | _ -> Requests.Empty.recordSlide, Family, None
        Components.modal "Add a Slide: Single" [
            p [] [ 
                text "We require information on the taxonomic identity, sample origin, spatial properties, and temporal properties for every slide. Please fill these in below. For more information,"
                a [ attr.href <| "https://globalpollenproject.org/Guide"; attr.target "_blank" ] [ text "please refer to the GPP guide" ]
                text "."
            ]
            Components.formGroupRow [
                label [ attr.``for`` "inputExistingId"; _class "col-sm-2 col-form-label" ] [ text "Existing ID" ]
                Grid.column Small 10 [
                    input [ bind.input.string vm.ExistingId (fun s -> { vm with ExistingId = s } |> ChangeDraftSlide |> dispatch) ; _class "form-control"; attr.id "inputExistingId"; attr.placeholder "Identifier" ]
                    small [ _class "form-text text-muted" ] [ text "If you have already assigned IDs to your slides, you can specify this here. Your ID will be used in place of a Global Pollen Project ID within this collection." ]
                ]
            ]
            h5 [] [ text "1. Taxonomic Identity" ]
            hr []
            p [] [ text "How has the material on this slide been identified?"; requiredSymbol ]
            div [ _class "form-check" ] [
                label [ _class "form-check-label" ] [
                    input [ bind.change.string vm.PlantIdMethod.Method (fun _ -> { vm with PlantIdMethod = { vm.PlantIdMethod with Method = "private" }} |> ChangeDraftSlide |> dispatch)
                            _class "form-check-input"; _type "radio"; attr.name "sampleType"; attr.id "botanical"; attr.value "botanical" ]
                    strong [] [ text "Direct." ]
                    text "Pollen or spores sampled from plant material."
                ]
                label [ _class "form-check-label" ] [
                    input [ bind.change.string vm.PlantIdMethod.Method (fun _ -> { vm with PlantIdMethod = { vm.PlantIdMethod with Method = "morphological" }} |> ChangeDraftSlide |> dispatch)
                            _class "form-check-input"; _type "radio"; attr.name "sampleType"; attr.id "morphological"; attr.value "morphological" ]
                    strong [] [ text "Morphological." ]
                    text "A taxonomic identification attributed to the grains by morphology, for example using pollen keys."
                ]
                label [ _class "form-check-label" ] [
                    input [ bind.change.string vm.PlantIdMethod.Method (fun _ -> { vm with PlantIdMethod = { vm.PlantIdMethod with Method = "environmental" }} |> ChangeDraftSlide |> dispatch)
                            _class "form-check-input"; _type "radio"; attr.name "sampleType"; attr.id "environmental"; attr.value "environmental" ]
                    strong [] [ text "Environmental." ]
                    text "The pollen was extracted from an environmental sample, for example surface water or a pollen trap. The taxonomic identification has been constrained by species occuring known to occur in this area."
                ]
            ]
            p [] [
                text "This reference slide is of"
                // TODO Fix select binding
                select [ bind.change.string (string rank) (fun r -> Family |> ToggleRank |> dispatch ); _class "form-control input-sm inline-dropdown" ] [
                    option [ attr.value Species ] [ text "Species" ]
                    option [ attr.value Genus ] [ text "Genus" ]
                    option [ attr.value Family ] [ text "Family" ]
                ]
                text "rank."
            ]
            p [] [ text "Please enter the original taxonomic identity given to the slide."; requiredSymbol ]
            Grid.row [
                Grid.column Small 3 [
                    input [ bind.change.string vm.OriginalFamily (fun s -> { vm with OriginalFamily = s |> capitalise } |> ChangeDraftSlide |> dispatch)
                            _type "text"; attr.id "original-Family"; _class "form-control"; attr.autocomplete "off"; attr.placeholder "Family"; ]
                    div [ _class "dropdown-menu taxon-dropdown"; attr.id "FamilyList"; attr.style "display:none" ] []
                ]
                Grid.column Small 3 [
                    input [ bind.change.string vm.OriginalGenus (fun s -> { vm with OriginalGenus = s |> capitalise } |> ChangeDraftSlide |> dispatch);
                            if rank = Family then attr.disabled "disabled";
                            _type "text"; attr.id "original-Genus"; _class "form-control"; attr.autocomplete "off"; attr.placeholder "Genus" ]
                    div [ _class "dropdown-menu taxon-dropdown"; attr.id "GenusList"; attr.id "display:none" ] []
                ]
                Grid.column Small 3 [
                    input [ bind.change.string vm.OriginalSpecies (fun s -> { vm with OriginalSpecies = s } |> ChangeDraftSlide |> dispatch);
                            if rank <> Species then attr.disabled "disabled";
                            _type "text"; attr.id "original-Species"; _class "form-control"; attr.autocomplete "off"; attr.placeholder "Species" ]
                    div [ _class "dropdown-menu taxon-dropdown"; attr.id "SpeciesList"; attr.style "display:none" ] []
                ]
                Grid.column Small 3 [
                    input [ bind.change.string vm.OriginalAuthor (fun s -> { vm with OriginalAuthor = s |> capitalise } |> ChangeDraftSlide |> dispatch)
                            if rank <> Species then attr.disabled "disabled"
                            _type "text"; _class "form-control"; attr.autocomplete "off"; attr.placeholder "Auth." ]
                ]
            ]
            small [ attr.id "taxon-help"; _class "form-text text-muted" ] [ text "This identity will be validated against the taxonomic backbone. If / when taxonomic changes occur, or have occurred, these will be reflected on this slide automatically." ]
            match newSlideTaxonStatus with
            | None ->
                button [ _class "btn btn-default"; attr.style "margin-bottom:0.5em"
                         if isValidTaxonSearch rank vm.OriginalFamily vm.OriginalGenus vm.OriginalSpecies then attr.disabled true ] [ text "Validate Taxon" ]
            | Some matchedTaxa ->
                div [] [
                    match matchedTaxa.Length with
                    | 0 -> div [ _class "alert alert-error" ] [ text "There were no matches." ]
                    | _ ->
                        if matchedTaxa.Head.TaxonomicStatus = "accepted" then
                            div [ _class "alert alert-success" ] [
                                p [] [ strong [] [ text "This taxon is an accepted name." ] ]
                                p [] [
                                    text "GPP Taxon:"
                                    span [] [ text matchedTaxa.Head.Family ]
                                    span [] [ text ">" ]
                                    span [] [ text matchedTaxa.Head.Genus ]
                                    span [] [ text matchedTaxa.Head.Species ]
                                    span [] [ text matchedTaxa.Head.NamedBy ]
                                ]
                            ]
                        else
                            // TODO Implement synonym, ambiguous, unverified, invalid
                            div [] [ text "TODO Implement this" ]
                    ]
        ] []
        


let activeCollection (activeCollection:EditableRefCollection) dispatch =
    div [ _class "card"; attr.id "collection-detail-card" ] [
        div [ _class "card-header" ] [ text activeCollection.Name ]
        div [ _class "card-block" ] [
            div [ _class "card-title row" ] [
                Grid.column Medium 5 [ p [] [ text activeCollection.Description ] ]
                Grid.column Medium 7 [
                    Grid.row [
                        Grid.column Medium 5 [
                            button [ on.click (fun _ -> AddSlideForm (activeCollection.Id.ToString()) |> SetPage |> dispatch); _class "btn btn-primary" ] [
                                Icons.fontawesome "plus-square"
                                text "Add new slide"
                            ]
                        ]
                        Grid.column Medium 7 [
                            button [ on.click (fun _ -> activeCollection.Id |> RequestPublication |> dispatch)
                                     if activeCollection.AwaitingReview then attr.hidden "hidden"
                                     _class "btn btn-primary" ] [
                                Icons.fontawesome "beer"
                                text "Request Publication"
                            ]
                            p [ if not activeCollection.AwaitingReview then attr.hidden "hidden" ] [ text "Your collection has been submitted for review. The outcome will appear here." ]
                        ]
                    ]
                ]
            ]
        ]
        table [ _class "table"; attr.id "slides-data-table" ] [
            thead [] [
                tr [] [
                    th [] [ text "ID" ]
                    th [] [ text "Family" ]
                    th [] [ text "Genus" ]
                    th [] [ text "Species" ]
                    th [] [ text "Current Taxon" ]
                    th [] [ text "Image Count" ]
                    th [] [ text "Actions" ]
                ]
            ]
            tbody [] [
                forEach activeCollection.Slides <| fun slide ->
                tr [ if slide.Voided then _class "table-danger" ] [
                    td [] [ text slide.CollectionSlideId ]
                    td [] [ text slide.FamilyOriginal ]
                    td [] [ text slide.GenusOriginal ]
                    td [] [ text slide.SpeciesOriginal ]
                    td [] [ text <| sprintf "%s %s %s %s" slide.CurrentFamily slide.CurrentGenus slide.CurrentSpecies slide.CurrentSpAuth ]
                    td [] [ text <| sprintf "%i images" slide.Images.Length ]
                    td [] [
                        button [ on.click (fun _ -> SlideDetailView(slide.CollectionId.ToString(), slide.CollectionSlideId) |> SetPage |> dispatch)
                                 if slide.Voided then attr.disabled "disabled"; _class "btn btn-secondary collection-slide-upload-image-button" ] [ text "Details" ]
                    ]
                ]
            ]
        ]
    ]
    
module Calibration =
    
    let addMicroscope (req:AddMicroscopeRequest) dispatch =
        section [ attr.id "add-microscope" ] [
            h2 [] [ text "Set up a new microscope" ]
            Components.formGroup [
                label [] [ text "Calibration Friendly Name" ]
                input [ bind.input.string req.Name (fun n -> { req with Name = n } |> ChangeDraftMicroscope |> dispatch); _class "form-control" ]
                small [ _class "form-text text-muted" ] [ text "Use a name that is specific to this microscope, for example its make and model." ]
            ]
            label [] [ text "Microscope type" ]
            select [ bind.change.string req.Type (fun n -> { req with Type = n } |> ChangeDraftMicroscope |> dispatch); _class "form-control input-sm inline-dropdown" ] [
                option [ attr.value "Compound" ] [ text "Compound" ]
                option [ attr.value "Single" ] [ text "Single" ]
                option [ attr.value "Digital" ] [ text "Digital" ]
            ]
            forEach req.Objectives <| fun mag ->
                concat [
                    input [ _class "required"; bind.input.int mag (fun ocular ->
                        { req with Objectives = (ocular::req.Objectives) |> List.distinct } |> ChangeDraftMicroscope |> dispatch) ]
                    a [ attr.href "#"; on.click (fun _ -> { req with Objectives = req.Objectives |> List.except [mag] } |> ChangeDraftMicroscope |> dispatch) ] [ text "Remove" ] ]
            button [ on.click (fun _ -> SendAddMicroscope |> dispatch); _class "btn btn-primary" ] [ text "Submit" ]
        ]

    let editMicroscope (microscope:Calibration) (req:CalibrateRequest) dispatch =
        let loadedImage = String.IsNullOrEmpty req.ImageBase64 |> not
        let canSubmit = false // TODO
        section [ attr.id "edit-microscope" ] [
            h3 [] [ text microscope.Name ]
            label [] [ text "Already calibrated:" ]
            if microscope.Magnifications.Length = 0 then span [ attr.style "font-style: italic;" ] [ text "None" ]
            ul [] [
                forEach microscope.Magnifications <| fun mag ->
                    li [] [ text <| sprintf "%ix" mag.Level ]
            ]
            if microscope.UncalibratedMags.Length > 0 then
                label [] [ text "Magnification Level" ]
                select [ bind.change.int req.Magnification (fun n -> { req with Magnification = n } |> ChangeDraftObjective |> dispatch) ] [
                    forEach microscope.UncalibratedMags <| fun m ->
                        option [ attr.value m ] [ text <| sprintf "%i" m ]
                ]
            br []
            comp<BlazorInputFile.InputFile> [ attr.accept "image/*"; attr.multiple "multiple"
                                              attr.callback "OnChange" (fun (files:BlazorInputFile.IFileListEntry[]) ->
                { vm with FramesBase64 = validateImages files |> Array.toList } |> ChangeDraftSlideImage |> dispatch) ] []
            ecomp<Viewer.MicroscopeViewerCanvas,_,_> [] {
                ImageUris = vm.FramesBase64 |> List.toArray
                StackType = Viewer.ImageStackType.Focus
            } (fun e -> e |> dispatch)
            if loadedImage then
                div [ _class "card" ] [
                    div [ _class "card-header" ] [ text "Draw a line on the loaded image of known length" ]
                    div [ _class "card-block" ] [
                        div [ _class "form-group row" ] [
                            Grid.column Small 3 [
                                button [ on.click (fun _ -> activateMeasuringLine); _type "button"; _class "btn btn-primary" ] [ text "Draw Line" ]
                            ]
                            label [ attr.``for`` "measuredDistance"; _class "col-sm-3 col-form-label" ] [ text "Measured Distance" ]
                            Grid.column Small 6 [
                                div [ _class "input-group" ] [
                                    input [ attr.value measuredDistance; _class "form-control" ]
                                    span [ _class "input-group-addon" ] [ text "μm" ]
                                ]
                                small [ _class "help" ] [ text "Enter the length of your measurement line in micrometres (μm)" ]
                            ]
                        ]
                    ]
                ]
            if canSubmit then button [ on.click (fun _ -> submit); _class "btn btn-primary" ] [ text "Submit" ]
        ]

    let calibrationModal vm dispatch =     
        Components.modal "Calibrations" [
            Grid.row [
                Grid.column Medium 4 [
                    div [ _class "card"; attr.id "calibration-list-card" ] [
                        div [ _class "card-header" ] [ text "My Microscopes" ]
                    ]
                    div [ _class "card-block" ] [
                        ul [ _class "list-group backbone-list"; attr.id "calibration-list" ] [
                            match vm.calibrations with
                            | None -> text "No calibrations have been loaded"
                            | Some calibrations ->
                                forEach calibrations <| function cal ->
                                    li [ _class "list-group-item"; on.click (fun _ -> CalibrationDetail (cal.Id.ToString()) |> SetPage |> dispatch) ] [ text cal.Name ]
                        ]
                        button [ _class "btn"; on.click (fun _ -> SetPage AddCalibration |> dispatch) ] [ text "Add new" ]
                    ]
                ]
                Grid.column Medium 8 [
                    addMicroscope vm.draft dispatch
                    editMicroscope vm.draft dispatch
                ]
            ]
        ] [ button [ on.click (fun _ -> SetPage Page.Home |> dispatch); _class "btn btn-default" ] [ text "Close" ] ]
    
//
//let appView dispatch =
//    [
//        link [ attr.rel "stylesheet"; attr.href "/lib/bootstrap-datepicker/dist/css/bootstrap-datepicker.min.css" ]
//        link [ attr.rel "stylesheet"; attr.href "https://cdn.datatables.net/1.10.15/css/dataTables.bootstrap4.min.css"]
//        Partials.addCollectionModal Requests.Empty.addCollection dispatch
//        Partials.recordSlide
//        Partials.slideDetail
//        Partials.calibrationModal
//
//        div [ _class "btn-toolbar mb-3"; _role "toolbar" ] [
//            button [ _koBind "click: function() { switchView(CurrentView.ADD_COLLECTION) }"; _class "btn btn-secondary" ] [ text "Create new collection" ]
//            button [ _koBind "click: function() { switchView(CurrentView.CALIBRATE) }"; _class "btn btn-secondary" ] [ text "Setup Microscope Calibrations" ]            
//        ]
//
//        Grid.row [
//            Grid.column Medium 3 [
//                div [ _class "card"; _id "collection-list-card" ] [
//                    div [ _class "card-header" ] [ text "My Collections" ]
//                    div [ _class "card-block" ] [
//                        span [ _id "collection-loading-spinner" ] [ text "Loading..." ]
//                        ul [ _class "list-group"; _id "collection-list"; _koBind "foreach: myCollections"; _style "display:none" ] [
//                            li [ _class "list-group-item"; _koBind "text: name, click: function() { $parent.switchView(CurrentView.DETAIL, $data); $parent.setActiveCollectionTab($element); }" ] []
//                        ]
//                    ]
//                ]
//            ]
//        ]
//    ]
