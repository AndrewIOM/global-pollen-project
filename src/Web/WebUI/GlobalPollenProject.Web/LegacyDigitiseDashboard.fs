module GlobalPollenProject.Web.DigitiseDashboard

open Giraffe.ViewEngine 
open GlobalPollenProject.Web.HtmlViews

// let digitiseApi =
//     mustBeLoggedIn >=>
//     choose [
//         route   "/collection"               >=> getCollectionHandler
//         route   "/collection/list"          >=> listCollectionsHandler
//         route   "/collection/start"         >=> startCollectionHandler
//         route   "/collection/publish"       >=> publishCollectionHandler
//         route   "/collection/slide/add"     >=> addSlideHandler
//         route   "/collection/slide/void"    >=> voidSlideHandler
//         route   "/collection/slide/addimage">=> addImageHandler
//         route   "/calibration/list"         >=> getCalibrationsHandler
//         route   "/calibration/use"          >=> setupMicroscopeHandler
//         route   "/calibration/use/mag"      >=> calibrateHandler
//     ]


[<AutoOpen>]
module Knockout =
    let _koBind value = KeyValue("data-bind", value)

module Partials =

    let addCollectionModal (vm:StartCollectionRequest) =
        div [ _koBind "BSModal: currentView() == CurrentView.ADD_COLLECTION, if: currentView() == CurrentView.ADD_COLLECTION"; _class "modal bd-example-modal-lg"; _aria "hidden" "true"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: newCollectionVM"; _class "modal-dialog modal-lg" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title" ] [ encodedText "Digitise a Collection" ]
                        button [ _type "button"; _class "close"; _aria "label" "Close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ span [ _aria "hidden" "true"] [ rawText "&times" ] ]
                    ]
                    div [ _class "modal-body" ] [
                        p [] [ encodedText "Please tell us about the reference collection you wish to digitise. You can edit this information later if necessary." ]
                        ul [ _koBind "foreach: validationErrors" ] [
                            li [ _koBind "text: $data.Errors[0]" ] []
                        ]

                        

                        // Curator Information
                        div [ _class "form-group row" ] [
                            label [ _class "col-sm-2 col-form-label" ] [ encodedText "Who is the curator?" ]
                            div [ _class "col-sm-4" ] [
                                input [ _koBind "value: curatorFirstNames"; _placeholder "Forenames"; _class "form-control" ] ]
                            div [ _class "col-sm-4" ] [
                                input [ _koBind "value: curatorSurname"; _placeholder "Surname"; _class "form-control" ] ]
                        ]

                        div [ _class "form-group" ] [
                            label [] [ encodedText "Email Address of Curator, for Enquiries" ]
                            input [ _koBind "textInput: email"; _type "Email"; _class "form-control" ]
                            small [ _class "form-text text-muted" ] [ encodedText "Please specify a contact email so that users can find out further information about this collection."]
                        ]

                        // Access Information
                        h4 [] [ encodedText "Physical Access to Material" ]
                        hr []
                        p [] [ encodedText "Please tell us the level of access that the curator has to the original reference material." ]
                        div [ _class "form-check" ] [
                            label [ _class "form-check-label" ] [
                                input [ _class "form-check-input"; _type "radio"; _name "accessMethod"; _id "digital"; _value "digital"; _koBind "checked: accessMethod" ]
                                strong [] [ encodedText "Digital Only." ]
                                encodedText "This could be a meta-collection made from many dispursed sources, or reference material to which the owner of the images no longer has physical access."
                            ]
                            label [ _class "form-check-label" ] [
                                input [ _class "form-check-input"; _type "radio"; _name "accessMethod"; _id "institution"; _value "institution"; _koBind "checked: accessMethod" ]
                                strong [] [ encodedText "Institution." ]
                                encodedText "The reference material is housed within an institution."
                            ]
                            label [ _class "form-check-label" ] [
                                input [ _class "form-check-input"; _type "radio"; _name "accessMethod"; _id "private"; _value "private"; _koBind "checked: accessMethod" ]
                                strong [] [ encodedText "Personal / Private Collection." ]
                                encodedText "The physical reference material forms a private collection of the curator. Access to the material may be negotiated with the curator."
                            ]
                        ]

                        div [ _class "form-group" ] [
                            label [] [ encodedText "Institution Where Collection is Located" ]
                            input [ _koBind "textInput: institutionName, enable: accessMethod() == 'institution'"; _class "form-control" ]
                            small [ _class "form-text text-muted" ] [ encodedText "Where are the physical slides for this collection located? Be sure to include any specific identifiers, for example a group or department name." ]
                        ]
                        div [ _class "form-group" ] [
                            label [] [ encodedText "Institution Website Address" ]
                            input [ _koBind "textInput: institutionUrl, enable: accessMethod() == 'institution'"; _type "url"; _class "form-control" ]
                            small [ _class "form-text text-muted" ] [ encodedText "If there is further information on your own website, paste a link here." ]
                        ]
                    ]
                    div [ _class "modal-footer" ] [
                        button [ _koBind "click: function() { submit($root) }, disable: isProcessing"; _type "button"; _class "btn btn-primary" ] [ encodedText "Create" ]
                    ]
                ]
            ]
        ]

    let slideDetail =
        div [ _koBind "BSModal: currentView() == CurrentView.SLIDE_DETAIL, if: currentView() == CurrentView.SLIDE_DETAIL"; _class "modal bd-example-modal-lg"; _aria "hidden" "true"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: slideDetailVM"; _class "modal-dialog model-lg" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title"; _koBind "text: 'Slide: ' + slideDetail().CollectionSlideId + ' - ' + slideDetail().FamilyOriginal + ' ' + slideDetail().GenusOriginal + ' ' + slideDetail().SpeciesOriginal" ] []
                        button [ _type "button"; _class "close"; _aria "label" "Close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ span [ _aria "hidden" "true" ] [ rawText "&times;" ] ]
                    ]
                    div [ _class "modal-body" ] [
                        ul [ _class "nav nav-tabs" ] [
                            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "#"; _koBind "click: function() { switchTab(SlideDetailTab.OVERVIEW) }, css: { active: currentTab() == SlideDetailTab.OVERVIEW }" ] [ encodedText "Overview" ] ]
                            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "#"; _koBind "click: function() { switchTab(SlideDetailTab.UPLOAD_STATIC) }, css: { active: currentTab() == SlideDetailTab.UPLOAD_STATIC }" ] [ encodedText "Upload static image" ] ]
                            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "#"; _koBind "click: function() { switchTab(SlideDetailTab.UPLOAD_FOCUSABLE) }, css: { active: currentTab() == SlideDetailTab.UPLOAD_FOCUSABLE }" ] [ encodedText "Upload focusable image" ] ]
                        ]
                    ]

                    div [ _id "slidedetail-overview-tab"; _koBind "visible: currentTab() == SlideDetailTab.OVERVIEW" ] [
                        div [ _class "alert alert-info"; _koBind "visible: !slideDetail().IsFullyDigitised" ] [
                            Icons.fontawesome "info-circle"
                            encodedText "This slide has not been fully digitised. Upload at least one image"
                        ]
                        div [ _class "alert alert-success"; _koBind "visible: slideDetail().IsFullyDigitised" ] [
                            Icons.fontawesome "check-circle"
                            encodedText "Fully digitised"
                        ]
                        Grid.container [
                            ul [ _class "grain-grid"; _koBind "foreach: slideDetail().Images" ] [
                                li [] [
                                    div [ _class "img-container" ] [
                                        a [] [ img [ _koBind "attr: {src: $data.FramesSmall[0]}"; _style "max-width: 100%; max-height: 100%;" ] ]
                                    ]
                                ]
                            ]
                        ]
                        p [] [ 
                            encodedText "The original reference slide has the taxonomic identification:"
                            span [ _koBind "text: slideDetail().FamilyOriginal" ] []
                            span [ _koBind "text: slideDetail().GenusOriginal" ] []
                            span [ _koBind "text: slideDetail().SpeciesOriginal" ] [] ]
                        p [] [
                            encodedText "The most current name for this taxon is: "
                            span [ _koBind "text: slideDetail().CurrentFamily + ' ' + slideDetail().CurrentGenus + ' ' + slideDetail().CurrentSpecies + ' ' + slideDetail().CurrentSpAuth" ] [] 
                        ]
                        p [] [ encodedText "If this slide contains errors, you can void it. This will remove the slide from the collection and allow re-entry of another slide with the correct information." ]
                        button [ _type "button"; _koBind "click: function() { voidSlide(); }"; _class "btn btn-danger" ] [
                            Icons.fontawesome "trash-o"
                            encodedText "Void Slide"
                        ]
                    ]

                    div [ _id "slidedetail-static-tab"; _koBind "visible: currentTab() == SlideDetailTab.UPLOAD_STATIC" ] [
                        ul [ _koBind "foreach: validationErrors" ] [
                            li [ _koBind "text: $data" ] []
                        ]
                        h5 [] [ encodedText "Upload a static image" ]
                        p [] [ encodedText "A static image uses a floating calibration to discren size within the image. You must complete the calibration step for every image." ]
                        input [ _type "file"; _class "upload btn"; _koBind "event: { change: function() { createStaticImageViewer($element); } }" ]
                        div [ _id "static-image-previewer-container" ] []
                        div [ _class "card"; _id "slidedetail-static-measurement-section"; _koBind "visible: loadedStaticImage" ] [
                            div [ _class "card-header" ] [
                                encodedText "Draw a line on the loaded image of known length"
                            ]
                            div [ _class "card-block" ] [
                                div [ _class "form-group-row" ] [
                                    Grid.column Small 3 [
                                        button [ _koBind "click: function() { activateMeasuringLine(); }"; _type "button"; _class "btn btn-primary"; _id "slidedetail-draw-line-button" ] [ encodedText "Draw Line" ]
                                    ]
                                    label [ _for "measuredDistance"; _class "col-sm-3 col-form-label" ] [ encodedText "Measured Distance" ]
                                    Grid.column Small 6 [
                                        div [ _class "input-group" ] [
                                            input [ _koBind "value: measuredDistance"; _id "measuredDistance"; _class "form-control" ]
                                            span [ _class "input-group-addon" ] [ encodedText "μm" ]
                                        ]
                                        small [ _class "help" ] [ encodedText "Enter the length of your measurement line in micrometres (μm)" ]
                                    ]
                                ]
                            ]
                        ]
                        div [ _class "card"; _id "slidedetail-static-year-section"; _koBind "visible: loadedStaticImage" ] [
                            div [ _class "card-block" ] [
                                div [ _class "form-group-row" ] [
                                    label [ _for "digitisedYearStatic"; _class "col-sm-4 col-form-label" ] [ encodedText "Year Image Taken" ]
                                    div [ _class "col-sm-8" ] [
                                        input [ _koBind "value: digitisedYear"; _id "digitisedYearStatic"; _class "form-control" ]
                                        small [ _class "help" ] [ encodedText "In which calendar year was this image taken?" ]
                                    ]
                                ]
                            ]
                        ]
                        div [ _class "card"; _koBind "visible: loadedStaticImage" ] [
                            div [ _class "card-block" ] [
                                Grid.row [
                                    div [ _class "col-sm-9 progress"; _id "slidedetail-static-upload-progress" ] [
                                        div [ _koBind "visible: uploadPercentage, style: { width: function() { if (uploadPercentage() != null) { return uploadPercentage() + '%'; } } }"; _class "progress-bar progress-bar-striped progress-bar-animated"; _id "slidedetail-static-upload-progressbar"; _style "width:100%" ] []
                                    ]
                                    div [ _class "col-sm-3" ] [
                                        button [ _koBind "click: function() { submitStatic($root); }, enable: isValidStaticRequest"; _class "btn btn-primary" ] [ encodedText "Upload Image" ]
                                    ]
                                ]
                            ]
                        ]
                    ]

                    div [ _id "slidedetail-focusable-tab"; _koBind "visible: currentTab() == SlideDetailTab.UPLOAD_FOCUSABLE" ] [
                        div [ _koBind "visible: calibrations().length == 0"; _class "alert alert-danger"; _id "slidedetail-no-calibrations-alert" ] [
                            strong [] [ encodedText "Error" ]
                            encodedText " - no microscope calibrations have been configured"
                        ]
                        ul [ _koBind "foreach: validationErrors" ] [
                            li [ _koBind "text: $data" ] []
                        ]
                        div [ _koBind "visible: calibrations().length > 0" ] [
                            h5 [] [ encodedText "Upload a focusable image" ]
                            p [] [ encodedText "Select all focus level images below" ]
                            input [ _type "file"; _multiple; _class "upload btn"; _koBind "event: { change: function() { createFocusImageViewer($element); } }" ]
                            div [ _id "focus-image-previewer-container" ] []
                            div [ _class "card"; _id "slidedetail-microscope-section"; _koBind "visible: loadedFocusImages" ] [
                                div [ _class "card-header" ] [
                                    encodedText "Select your configured microscope + magnification level"
                                ]
                                div [ _class "card-block" ] [
                                    div [ _koBind "visible: calibrations().length > 0"; _class "form-group row" ] [
                                        Grid.column Small 6 [
                                            label [ _for "microscope-down" ] [ encodedText "Microscope" ]
                                            div [ _class "dropdown" ] [
                                                button [ _koBind "text: selectedMicroscopeName()"; _class "btn btn-secondary dropdown-toggle calibration-dropdown"; _type "button"; _id "microscope-dropdown"; _data "toggle" "dropdown" ] []
                                                div [ _koBind "foreach: calibrations"; _class "dropdown-menu calibration-dropdown-list" ] [
                                                    a [ _koBind "value: $data.Name, text: $data.Name, click: $parent.selectMicroscope"; _class "dropdown-item calibration-option" ] []
                                                ]
                                            ]
                                        ]
                                        Grid.column Small 6 [
                                            label [ _for "magnification-dropdown" ] [ encodedText "Magnification" ]
                                            div [ _class "dropdown"; _koBind "visible: selectedMicroscope() != null" ] [
                                                button [ _koBind "text: selectedMagnificationName()"; _class "btn btn-secondary dropdown-toggle calibration-dropdown"; _type "button"; _id "magnification-dropdown"; _data "toggle" "dropdown" ] []
                                                div [ _koBind "foreach: selectedMicroscopeMagnifications()"; _class "dropdown-menu calibration-dropdown-list" ] [
                                                    a [ _koBind "value: $data, text: $data.Level, click: $parent.selectMagnification"; _class "dropdown-item calibration-option" ] []
                                                ]
                                            ] 
                                        ]
                                    ]
                                ]
                            ]

                            div [ _class "card"; _id "slidedetail-focus-year-section"; _koBind "visible: loadedFocusImages" ] [
                                div [ _class "card-block" ] [
                                    div [ _class "form-group row" ] [
                                        label [ _for "digitisedYearFocus"; _class "col-sm-4 col-form-label" ] [ encodedText "Year Image Taken" ]
                                        Grid.column Small 8 [
                                            input [ _koBind "value: digitisedYear"; _id "digitisedYearFocus"; _class "form-control" ]
                                            small [ _class "help" ] [ encodedText "In which calendar year was this image taken?" ]
                                        ]
                                    ]
                                ]
                            ]
                            div [ _class "card"; _koBind "visible: loadedFocusImages" ] [
                                div [ _class "card-block" ] [
                                    Grid.row [
                                        div [ _class "col-sm-9 prgoress"; _id "slidedetail-focus-upload-progress" ] [
                                            div [ _koBind "visible: uploadPercentage, style: { width: function() { if (uploadPercentage() != null) { return uploadPercentage() + '%'; } } }"; _class "progress-bar progress-bar-striped progress-bar-animated"; _id "slidedetail-focus-upload-progressbar"; _style "width:100%" ] []
                                        ]
                                        Grid.column Small 3 [
                                            button [ _koBind "click: function() { submitFocus($root); }, enable: isValidFocusRequest"; _type "button"; _class "btn btn-primary" ] [ encodedText "Upload Image" ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    let addMicroscope =
        div [ _koBind "if: newMicroscope" ] [
            div [ _koBind "with: newMicroscope" ] [
                h2 [] [ encodedText "Setting up new microscope" ]
                div [ _class "form-group" ] [
                    label [] [ encodedText "Calibration Friendly Name" ]
                    input [ _koBind "textInput: friendlyName"; _class "form-control"; _id "friendlyname" ]
                    small [ _id "friendlyName-help"; _class "form-text text-muted" ] [ encodedText "Use a name that is specific to this microscope, for example its make and model." ]
                ]
                label [] [ encodedText "Microscope type" ]
                select [ _koBind "value: microscopeType"; _class "form-control input-sm inline-dropdown" ] [
                    option [ _value "Compound" ] [ encodedText "Compound" ]
                    option [ _value "Single" ] [ encodedText "Single" ]
                    option [ _value "Digital" ] [ encodedText "Digital" ]
                ]
                div [ _koBind "foreach: magnifications" ] [
                    input [ _class "required"; _koBind "value: $data, uniqueName: true" ]
                    a [ _href "#"; _koBind "click: $root.removeMag" ] [ encodedText "Remove" ]
                ]
                button [ _koBind "click: function() { submit($parent) }"; _class "btn btn-primary" ] [ encodedText "Submit" ]
            ]
        ]

    let editMicroscope =
        div [ _koBind "if: microscopeDetail" ] [
            div [ _koBind "with: microscopeDetail" ] [
                h3 [ _koBind "text: microscope().Name" ] []
                label [] [ encodedText "Already calibrated:" ]
                span [ _style "font-style: italic;"; _koBind "visible: microscope().Magnifications.length == 0" ] [ encodedText "None" ]
                ul [ _koBind "foreach: microscope().Magnifications" ] [
                    li [ _koBind "text: $parent.magName($data)" ] []
                ]
                div [ _koBind "visible: microscope().UncalibratedMags.length > 0" ] [
                    label [] [ encodedText "Magnification Level" ]
                    select [ _koBind "value: magnification, foreach: microscope().UncalibratedMags" ] [
                        option [ _koBind "value: $data, text: $data" ] []
                    ]
                ]
                br []
                input [ _type "file"; _class "upload"; _koBind "event: { change: function() { createViewer($element) } }" ]
                div [ _id "calibration-viewer-container" ] []
                div [ _class "card"; _id "calibration-static-measurement-section"; _koBind "visible: loadedImage" ] [
                    div [ _class "card-header" ] [ encodedText "Draw a line on the loaded image of known length" ]
                    div [ _class "card-block" ] [
                        div [ _class "form-group row" ] [
                            Grid.column Small 3 [
                                button [ _koBind "click: function() { activateMeasuringLine(); }"; _type "button"; _class "btn btn-primary"; _id "calibration-draw-line-button" ] [ encodedText "Draw Line" ]
                            ]
                            label [ _for "measuredDistance"; _class "col-sm-3 col-form-label" ] [ encodedText "Measured Distance" ]
                            Grid.column Small 6 [
                                div [ _class "input-group" ] [
                                    input [ _koBind "value: measuredDistance"; _id "measuredDistance"; _class "form-control" ]
                                    span [ _class "input-group-addon" ] [ encodedText "μm" ]
                                ]
                                small [ _class "help" ] [ encodedText "Enter the length of your measurement line in micrometres (μm)" ]
                            ] 
                        ]
                    ]
                ]
                button [ _koBind "click: function() { submit($parent) }, visible: canSubmit"; _class "btn btn-primary" ] [ encodedText "Submit" ]
            ]
        ]

    let calibrationModal =
        div [ _koBind "BSModal: currentView() == CurrentView.CALIBRATE, if: currentView() == CurrentView.CALIBRATE"; _class "modal bd-example-modal-lg"; _role "dialog"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: calibrateVM"; _class "modal-dialog modal-lg" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title" ] [ encodedText "Calibrations" ]
                        button [ _type "button"; _class "close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ span [] [ rawText "&times;" ] ]
                    ]
                    div [ _class "modal-body" ] [
                        Grid.row [
                            Grid.column Medium 4 [
                                div [ _class "card"; _id "calibration-list-card" ] [
                                    div [ _class "card-header" ] [ encodedText "My Microscopes" ]
                                ]
                                div [ _class "card-block" ] [
                                    ul [ _class "list-group backbone-list"; _id "calibration-list"; _koBind "foreach: myMicroscopes" ] [
                                        li [ _class "list-group-item"; _koBind "text: Name, click: function() { $parent.changeView(CalibrateView.DETAIL, $data); $parent.setActiveCalibrationTab($element); }" ] []
                                    ]
                                    button [ _class "btn"; _id "calibration-add-new-button"; _koBind "click: function() { changeView(CalibrateView.ADD_MICROSCOPE); setActiveCalibrationTab(null); }" ] [ encodedText "Add new" ]
                                ]
                            ]
                            Grid.column Medium 8 [
                                addMicroscope
                                editMicroscope
                            ]
                        ]
                    ]
                    div [ _class "modal-footer" ] [
                        button [ _koBind "click: function() { $root.switchView(CurrentView.MASTER) }"; _class "btn btn-default" ] [ encodedText "Close" ]
                    ]
                ]
            ]
        ]

    let requiredSymbol = span [ _class "required-symbol" ] [ encodedText "*" ]

    let recordSlide =
        div [ _koBind "BSModal: currentView() == CurrentView.ADD_SLIDE_RECORD, if: currentView() == CurrentView.ADD_SLIDE_RECORD"; _class "modal bd-example-modal-lg"; _role "dialog"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: newSlideVM"; _class "modal-dialog modal-lg" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title" ] [ encodedText "Add a Slide: Single" ]
                        button [ _type "button"; _class "close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ rawText "&times" ]
                    ]
                    div [ _class "modal-body" ] [
                        p [] [ 
                            encodedText "We require information on the taxonomic identity, sample origin, spatial properties, and temporal properties for every slide. Please fill these in below. For more information,"
                            a [ _href "/Guide" ] [ encodedText "please refer to the GPP guide" ]
                            encodedText "."
                        ]
                        div [ _class "form-group row" ] [
                            label [ _for "inputExistingId"; _class "col-sm-2 col-form-label" ] [ encodedText "Existing ID" ]
                            Grid.column Small 10 [
                                input [ _koBind "value: existingId"; _class "form-control"; _id "inputExistingId"; _placeholder "Identifier" ]
                                small [ _class "form-text text-muted" ] [ encodedText "If you have already assigned IDs to your slides, you can specify this here. Your ID will be used in place of a Global Pollen Project ID within this collection." ]
                            ]
                        ]
                        h5 [] [ encodedText "1. Taxonomic Identity" ]
                        hr []
                        p [] [ encodedText "How has the material on this slide been identified?"; requiredSymbol ]
                        div [ _class "form-check" ] [
                            label [ _class "form-check-label" ] [
                                input [ _class "form-check-input"; _type "radio"; _name "sampleType"; _id "botanical"; _value "botanical"; _koBind "checked: identificationMethod" ]
                                strong [] [ encodedText "Direct." ]
                                encodedText "Pollen or spores sampled from plant material."
                            ]
                            label [ _class "form-check-label" ] [
                                input [ _class "form-check-input"; _type "radio"; _name "sampleType"; _id "morphological"; _value "morphological"; _koBind "checked: identificationMethod" ]
                                strong [] [ encodedText "Morphological." ]
                                encodedText "A taxonomic identification attributed to the grains by morphology, for example using pollen keys."
                            ]
                            label [ _class "form-check-label" ] [
                                input [ _class "form-check-input"; _type "radio"; _name "sampleType"; _id "environmental"; _value "environmental"; _koBind "checked: identificationMethod" ]
                                strong [] [ encodedText "Environmental." ]
                                encodedText "The pollen was extracted from an environmental sample, for example surface water or a pollen trap. The taxonomic identification has been constrained by species occuring known to occur in this area."
                            ]
                        ]
                        p [] [
                            encodedText "This reference slide is of"
                            select [ _koBind "value: rank"; _class "form-control input-sm inline-dropdown" ] [
                                option [ _value "Species" ] [ encodedText "Species" ]
                                option [ _value "Genus" ] [ encodedText "Genus" ]
                                option [ _value "Family" ] [ encodedText "Family" ]
                            ]
                            encodedText "rank."
                        ]
                        p [] [ encodedText "Please enter the original taxonomic identity given to the slide."; requiredSymbol ]
                        Grid.row [
                            Grid.column Small 3 [
                                input [ _koBind "value: family, event: { blur: capitaliseFirstLetter($element) }"; _type "text"; _id "original-Family"; _class "form-control"; _autocomplete "off"; _placeholder "Family"; ]
                                div [ _class "dropdown-menu taxon-dropdown"; _id "FamilyList"; _style "display:none" ] []
                            ]
                            Grid.column Small 3 [
                                input [ _koBind "value: genus, enable: rank() != 'Family', event: { blur: capitaliseFirstLetter($element) }"; _type "text"; _id "original-Genus"; _class "form-control"; _autocomplete "off"; _placeholder "Genus"; ]
                                div [ _class "dropdown-menu taxon-dropdown"; _id "GenusList"; _style "display:none" ] []
                            ]
                            Grid.column Small 3 [
                                input [ _koBind "value: species, disable: rank() != 'Species'"; _type "text"; _id "original-Species"; _class "form-control"; _autocomplete "off"; _placeholder "Species"; ]
                                div [ _class "dropdown-menu taxon-dropdown"; _id "SpeciesList"; _style "display:none" ] []
                            ]
                            Grid.column Small 3 [
                                input [ _koBind "value: author, disable: rank() != 'Species', event: { blur: capitaliseFirstLetter($element) }"; _type "text"; _class "form-control"; _autocomplete "off"; _placeholder "Auth." ]
                            ]
                        ]
                        small [ _id "taxon-help"; _class "form-text text-muted" ] [ encodedText "This identity will be validated against the taxonomic backbone. If / when taxonomic changes occur, or have occurred, these will be reflected on this slide automatically." ]
                        button [ _class "btn btn-default"; _koBind "visible: newSlideTaxonStatus() == null, click: validateTaxon, enable: isValidTaxonSearch"; _style "margin-bottom:0.5em" ] [ encodedText "Validate Taxon" ]
                        div [ _koBind "visible: newSlideTaxonStatus, if: newSlideTaxonStatus" ] [
                            div [ _koBind "visible: newSlideTaxonStatus() == 'Error'" ] [ encodedText "There was a problem communicating with the taxonomic backbone." ]
                            div [ _koBind "if: newSlideTaxonStatus() != 'Error'" ] [
                                div [ _class "alert alert-success"; _koBind "visible: newSlideTaxonStatus()[0].TaxonomicStatus == 'accepted'" ] [
                                    p [] [ strong [] [ encodedText "This taxon is an accepted name." ] ]
                                    p [] [
                                        encodedText "GPP Taxon:"
                                        span [ _koBind "text: newSlideTaxonStatus()[0].Family" ] []
                                        span [] [ encodedText ">" ]
                                        span [ _koBind "text: newSlideTaxonStatus()[0].Genus" ] []
                                        span [ _koBind "text: newSlideTaxonStatus()[0].Species" ] []
                                        span [ _koBind "text: newSlideTaxonStatus()[0].NamedBy" ] []
                                    ]
                                ]
                                // Synonym
                                // Ambiguous
                                // Unverified
                                // Invalid
                            ]
                        ]
                    ]
                ]
            ]
        ]

let appView =
    [
        span [ _id "digitise-app" ] []
        link [ _rel "stylesheet"; _href "/lib/bootstrap-datepicker/dist/css/bootstrap-datepicker.min.css" ]
        link [ _rel "stylesheet"; _href "https://cdn.datatables.net/1.10.15/css/dataTables.bootstrap4.min.css"]
        Partials.addCollectionModal Requests.Empty.addCollection
        Partials.recordSlide
        Partials.slideDetail
        Partials.calibrationModal

        div [ _class "btn-toolbar mb-3"; _role "toolbar" ] [
            button [ _koBind "click: function() { switchView(CurrentView.ADD_COLLECTION) }"; _class "btn btn-secondary" ] [ encodedText "Create new collection" ]
            button [ _koBind "click: function() { switchView(CurrentView.CALIBRATE) }"; _class "btn btn-secondary" ] [ encodedText "Setup Microscope Calibrations" ]            
        ]

        Grid.row [
            Grid.column Medium 3 [
                div [ _class "card"; _id "collection-list-card" ] [
                    div [ _class "card-header" ] [ encodedText "My Collections" ]
                    div [ _class "card-block" ] [
                        span [ _id "collection-loading-spinner" ] [ encodedText "Loading..." ]
                        ul [ _class "list-group"; _id "collection-list"; _koBind "foreach: myCollections"; _style "display:none" ] [
                            li [ _class "list-group-item"; _koBind "text: name, click: function() { $parent.switchView(CurrentView.DETAIL, $data); $parent.setActiveCollectionTab($element); }" ] []
                        ]
                    ]
                ]
            ]
            div [ _class "col-md-9"; _koBind "visible: activeCollection, if: activeCollection" ] [
                div [ _class "card"; _id "collection-detail-card" ] [
                    div [ _class "card-header" ] [ span [ _koBind "text: activeCollection().name" ] [] ]
                    div [ _class "card-block" ] [
                        div [ _class "card-title row" ] [
                            Grid.column Medium 5 [ p [ _koBind "text: activeCollection().description" ] [] ]
                            Grid.column Medium 7 [
                                Grid.row [
                                    Grid.column Medium 5 [
                                        button [ _koBind "click: function() { switchView(CurrentView.ADD_SLIDE_RECORD, $data) }"; _class "btn btn-primary"; _id "collection-add-slide-button" ] [
                                            Icons.fontawesome "plus-square"
                                            encodedText "Add new slide"
                                        ]
                                    ]
                                    Grid.column Medium 7 [
                                        button [ _koBind "click: publish, visible: activeCollection().AwaitingReview == false"; _class "btn btn-primary"; _id "collection-publish-button" ] [
                                            Icons.fontawesome "beer"
                                            encodedText "Request Publication"
                                        ]
                                        p [ _koBind "visible: activeCollection().AwaitingReview" ] [ encodedText "Your collection has been submitted for review. The outcome will appear here." ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    table [ _class "table"; _id "slides-data-table" ] [
                        thead [] [
                            tr [] [
                                th [] [ encodedText "ID" ]
                                th [] [ encodedText "Family" ]
                                th [] [ encodedText "Genus" ]
                                th [] [ encodedText "Species" ]
                                th [] [ encodedText "Current Taxon" ]
                                th [] [ encodedText "Image Count" ]
                                th [] [ encodedText "Actions" ]
                            ]
                        ]
                        tbody [ _koBind "foreach: activeCollection().slides" ] [
                            tr [ _koBind "css: { 'table-danger': $data.voided }" ] [
                                td [ _koBind "text: $data.collectionSlideId" ] []
                                td [ _koBind "text: $data.familyOriginal" ] []
                                td [ _koBind "text: $data.genusOriginal" ] []
                                td [ _koBind "text: $data.speciesOriginal" ] []
                                td [ _koBind "text: $data.currentFamily + ' ' + $data.currentGenus + ' ' + $data.currentSpecies + ' ' + $data.currentSpAuth" ] []
                                td [ _koBind "text: $data.images.length" ] []
                                td [] [
                                    button [ _koBind "click: function() { $parent.switchView(CurrentView.SLIDE_DETAIL, $data) }, enable: function() { $data.Voided() == false }"; _class "btn btn-secondary collection-slide-upload-image-button" ] [ encodedText "Details" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ] |> Layout.standard [] "Digitise Dashboard" "Use this tool to digitise your collections. A digitised slide contains two components: (1) metadata, and (2) images."
