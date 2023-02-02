module GlobalPollenProject.Web.DigitiseDashboard

open Giraffe.ViewEngine 
open GlobalPollenProject.Web.HtmlViews

[<AutoOpen>]
module Knockout =
    let _koBind value = KeyValue("data-bind", value)

module Partials =

    let tutorialModal =
        div [ _id "tutorialModal"; _class "modal fade" ] [
            div [ _class "modal-dialog modal-dialog-centered"; _role "document" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title" ] [ str "Welcome to the digitisation tools." ]
                    ]
                    div [ _class "modal-body" ] [
                        p [] [ str "The Global Pollen Project's digitisation tools are available for researchers, organisations, and individuals who hold collections of pollen / spore reference slides." ]
                        p [] [ str "These tools can be used to record your slides' important metadata alongside pollen / spore images for the use of other researchers." ]
                        hr []
                        h5 [] [ str "More information" ]
                        p [] [ str "We strongly encourage you to read the getting started guide before proceeding with these tools." ]
                    ]
                    div [ _class "modal-footer" ] [
                        a [ _type "button"; _class "btn btn-outline-secondary"; _href "/Guide/Digitise" ] [ str "Step 1: Read the docs" ]
                        button [ _type "button"; _class "btn btn-primary"; _data "dismiss" "modal" ] [ str "Step 2: I'm ready" ]
                    ]
                ]
            ]
        ]

    let addCollectionModal (vm:StartCollectionRequest) =
        div [ _koBind "BSModal: currentView() == CurrentView.ADD_COLLECTION, if: currentView() == CurrentView.ADD_COLLECTION"; _class "modal"; _aria "hidden" "true"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: newCollectionVM"; _class "modal-dialog modal-lg" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title" ] [ encodedText "Digitise a Collection" ]
                        button [ _type "button"; _class "close"; _aria "label" "Close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ span [ _aria "hidden" "true"] [ rawText "&times" ] ]
                    ]
                    div [ _class "modal-body" ] [
                        p [] [ encodedText "Please tell us about the reference collection you wish to digitise. You can edit this information later if necessary." ]
                        div [ _class "alert alert-danger"; _role "alert"; _koBind "visible: validationErrors().length > 0" ] [
                            ul [ _koBind "foreach: validationErrors" ] [
                                li [ _koBind "text: $data.errors[0]" ] []
                            ]
                        ]
                        div [ _class "form-group" ] [
                            label [] [ str "Collection Name"]
                            input [ _koBind "textInput: name"; _class "form-control"; _id "name" ]
                            small [ _id "name-help"; _class "form-text text-muted" ] [ str "Use a name specific to the collection." ]
                        ]
                        div [ _class "form-group" ] [
                            label [] [ str "Description" ]
                            textarea [ _koBind "textInput: description"; _class "form-control"; _id "description"; _rows "3" ] []
                            small [ _id "description-help"; _class "form-text text-muted" ] [ str "Your collection description could include the motivation for creating the collection, geographical coverage, or the nature of the material, for example."]
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
        div [ _koBind "BSModal: currentView() == CurrentView.SLIDE_DETAIL, if: currentView() == CurrentView.SLIDE_DETAIL"; _class "modal"; _aria "hidden" "true"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: slideDetailVM"; _class "modal-dialog model-lg" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title"; _koBind "text: 'Slide: ' + slideDetail().collectionSlideId + ' - ' + slideDetail().familyOriginal + ' ' + slideDetail().genusOriginal + ' ' + slideDetail().speciesOriginal" ] []
                        button [ _type "button"; _class "close"; _aria "label" "Close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ span [ _aria "hidden" "true" ] [ rawText "&times;" ] ]
                    ]
                    div [ _class "modal-body" ] [
                        ul [ _class "nav nav-tabs" ] [
                            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "#"; _koBind "click: function() { switchTab(1) }, css: { active: currentTab() == 1 }" ] [ encodedText "Overview" ] ]
                            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "#"; _koBind "click: function() { switchTab(2) }, css: { active: currentTab() == 2 }" ] [ encodedText "Upload static image" ] ]
                            li [ _class "nav-item" ] [ a [ _class "nav-link"; _href "#"; _koBind "click: function() { switchTab(3) }, css: { active: currentTab() == 3 }" ] [ encodedText "Upload focusable image" ] ]
                        ]

                        div [ _id "slidedetail-overview-tab"; _koBind "visible: currentTab() == 1" ] [
                            div [ _class "alert alert-info"; _koBind "visible: !slideDetail().isFullyDigitised" ] [
                                Icons.fontawesome "info-circle"
                                encodedText "This slide has not been fully digitised. Upload at least one image"
                            ]
                            div [ _class "alert alert-success"; _koBind "visible: slideDetail().isFullyDigitised" ] [
                                Icons.fontawesome "check-circle"
                                encodedText "Fully digitised"
                            ]
                            Grid.container [
                                ul [ _class "grain-grid"; _koBind "foreach: slideDetail().images" ] [
                                    li [] [
                                        div [ _class "img-container" ] [
                                            a [] [ img [ _koBind "attr: {src: $data.framesSmall[0]}"; _style "max-width: 100%; max-height: 100%;" ] ]
                                        ]
                                    ]
                                ]
                            ]
                            p [] [ 
                                encodedText "The original reference slide has the taxonomic identification:"
                                span [ _koBind "text: slideDetail().familyOriginal + ' ' + slideDetail().genusOriginal + ' ' + slideDetail().speciesOriginal" ] [] ]
                            p [] [
                                encodedText "The most current name for this taxon is: "
                                span [ _koBind "text: slideDetail().currentFamily + ' ' + slideDetail().currentGenus + ' ' + slideDetail().currentSpecies + ' ' + slideDetail().currentSpAuth" ] [] 
                            ]
                            p [] [ encodedText "If this slide contains errors, you can void it. This will remove the slide from the collection and allow re-entry of another slide with the correct information." ]
                            button [ _type "button"; _koBind "click: function() { voidSlide(); }"; _class "btn btn-danger" ] [
                                Icons.fontawesome "trash-o"
                                encodedText "Void Slide"
                            ]
                        ]

                        div [ _id "slidedetail-static-tab"; _koBind "visible: currentTab() == 2" ] [
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
                                    div [ _class "form-group row" ] [
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
                                    div [ _class "form-group row" ] [
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

                        div [ _id "slidedetail-focusable-tab"; _koBind "visible: currentTab() == 3" ] [
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
                                                    button [ _koBind "text: selectedMicroscopeName()"; _class "btn btn-outline-secondary dropdown-toggle calibration-dropdown"; _type "button"; _id "microscope-dropdown"; _data "toggle" "dropdown" ] []
                                                    div [ _koBind "foreach: calibrations"; _class "dropdown-menu calibration-dropdown-list" ] [
                                                        a [ _koBind "value: $data.name, text: $data.name, click: $parent.selectMicroscope"; _class "dropdown-item calibration-option" ] []
                                                    ]
                                                ]
                                            ]
                                            Grid.column Small 6 [
                                                label [ _for "magnification-dropdown" ] [ encodedText "Magnification" ]
                                                div [ _class "dropdown"; _koBind "visible: selectedMicroscope() != null" ] [
                                                    button [ _koBind "text: selectedMagnificationName()"; _class "btn btn-outline-secondary dropdown-toggle calibration-dropdown"; _type "button"; _id "magnification-dropdown"; _data "toggle" "dropdown" ] []
                                                    div [ _koBind "foreach: selectedMicroscopeMagnifications()"; _class "dropdown-menu calibration-dropdown-list" ] [
                                                        a [ _koBind "value: $data, text: $data.level, click: $parent.selectMagnification"; _class "dropdown-item calibration-option" ] []
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
        ]

    let addMicroscope =
        div [ _koBind "if: newMicroscope" ] [
            div [ _koBind "with: newMicroscope" ] [
                h3 [] [ encodedText "Setting up new microscope" ]
                hr []
                p [] [ str "Please enter details about the microscope used for digitisation. Once set up, you will be able to select this calibration when uploading focusable images. Please note that only light microscopes are currently supported." ]
                div [ _class "form-group" ] [
                    label [] [ encodedText "Calibration Friendly Name" ]
                    input [ _koBind "textInput: friendlyName"; _class "form-control"; _id "friendlyname" ]
                    small [ _id "friendlyName-help"; _class "form-text text-muted" ] [ encodedText "Use a name that is specific to this microscope and can identify it, for example its owner or location." ]
                ]
                div [ _class "form-group" ] [
                    label [] [ encodedText "Camera Make and Model" ]
                    input [ _koBind "textInput: microscopeModel"; _class "form-control" ]
                    small [ _class "form-text text-muted" ] [ encodedText "If you are unsure of the model, the brand only is acceptable." ]
                ]
                div [ _class "form-group" ] [
                    label [] [ encodedText "Microscope type" ]
                    select [ _koBind "value: microscopeType"; _class "form-control" ] [
                        option [ _value "Compound" ] [ encodedText "Compound" ]
                        option [ _value "Single" ] [ encodedText "Single" ]
                        option [ _value "Digital" ] [ encodedText "Digital" ]
                    ]
                ]
                div [_koBind "visible: microscopeType() == 'Compound'" ] [
                    div [ _class "form-group" ] [
                        label [] [ encodedText "Ocular magnification" ]
                        input [ _koBind "textInput: ocular"; _type "number"; _class "form-control" ]
                        small [ _class "form-text text-muted" ] [ encodedText "The magnification provided by the ocular lens; this is the lens closest to the eye that magnify the image from the objective. Often this is 10x." ]
                    ]
                    div [ _class "form-group" ] [
                        label [] [ str "Objectives" ]
                        div [ _koBind "foreach: magnifications" ] [
                            div [ _class "input-group mb-3" ] [
                                input [ _class "required form-control"; _koBind "value: $data, uniqueName: true"; _type "number" ]
                                div [ _class "input-group-append" ] [
                                    button [ _koBind "click: $parent.removeMag"; _class "btn btn-outline-danger"; _type "button" ] [ str "Remove" ]
                                ]
                            ]
                        ]
                        button [ _koBind "click: addMag"; _class "btn btn-outline-secondary btn-sm btn-block"; _type "button" ] [ str "Add another objective" ]
                        small [] [ str "For example, often a light microscope will have objectives of 10x, 40x, and 100x. Use the 'Remove' buttons and the below 'Add another objective' button to configure the number of objectives on your microscope." ]
                    ]
                ]
                div [ _class "form-group"; _koBind "visible: microscopeType() != 'Compound'" ] [
                    label [] [ str "Magnification Level" ]
                    input [ _koBind "textInput: magnifications()[0]"; _class "form-control"; _type "number" ]
                    small [ _class "form-text text-muted" ] [ encodedText "For a single or digital microscope, please enter the magnification level used." ]

                ]
                button [ _class "btn btn-outline-secondary"; _koBind "click: function() { $parent.changeView(1) }" ] [ encodedText "Cancel" ]
                button [ _koBind "click: function() { submit($parent) }, enable: canSubmit"; _class "btn btn-primary" ] [ encodedText "Confirm Details" ]
            ]
        ]

    let editMicroscope =
        div [ _koBind "if: microscopeDetail" ] [
            div [ _koBind "with: microscopeDetail" ] [
                h3 [ _koBind "text: microscope().name" ] []
                p [] [ str "Select a magnification level and use the drawing tool to calibrate each magnification that you wish to use for focus images." ]
                hr []
                div [ _class "form-group row" ] [
                    label [ _class "col-sm-2 col-form-label" ] [ str "Already calibrated magnifications:" ]
                    div [ _class "col-sm-10" ] [
                        span [ _style "font-style: italic;"; _koBind "visible: microscope().magnifications.length == 0" ] [ encodedText "None" ]
                        div [ _class "btn-group"; _role "group"; _koBind "foreach: microscope().magnifications, visible: microscope().magnifications.length > 0" ] [
                            button [ _class "btn btn-secondary btn-sm" ] [ span [ _koBind "text: $parent.magName($data)" ] [] ]
                        ]
                    ]
                ]
                div [ _class "form-group row"; _koBind "visible: microscope().uncalibratedMags.length > 0" ] [
                    label [ _class "col-sm-2 col-form-label" ] [ str "Calibrate this magnification:" ]
                    div [ _class "col-sm-10" ] [
                        select [ _koBind "value: magnification, foreach: microscope().uncalibratedMags"; _class "form-control" ] [
                            option [ _koBind "value: $data, text: $data" ] []
                        ]
                    ]
                ]
                div [ _class "form-group row"; _koBind "visible: microscope().uncalibratedMags.length > 0" ] [
                    label [ _class "col-sm-2 col-form-label" ] [ str "Calibration Image:" ]
                    input [ _type "file"; _class "upload"; _koBind "event: { change: function() { createViewer($element) } }" ]
                ]
                hr []
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
                button [ _koBind "click: function() { submit($parent) }, enable: canSubmit"; _class "btn btn-primary" ] [ encodedText "Save Calibration" ]
            ]
        ]

    let calibrationModal =
        div [ _koBind "BSModal: currentView() == CurrentView.CALIBRATE, if: currentView() == CurrentView.CALIBRATE"; _class "modal"; _role "dialog"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: calibrateVM"; _class "modal-dialog modal-xl" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title" ] [ encodedText "Microscope Calibrations" ]
                        button [ _type "button"; _class "close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ span [] [ rawText "&times;" ] ]
                    ]
                    div [ _class "modal-body" ] [
                        p [] [ 
                            str "If you have a fixed camera setup, the Global Pollen Project supports uploading 'focus images'. These consist of a stack of images taken at variable focal levels. To upload focus images, you must first calibrate an image for each magnification level and for each microscope that you have used. "
                            a [ _href "/Guide/Digitise" ] [ str "Please see the documentation for full details." ]
                        ]
                        hr []
                        Grid.row [
                            Grid.column Medium 4 [
                                div [ _class "card"; _id "calibration-list-card" ] [
                                    div [ _class "card-header" ] [ encodedText "My Microscopes" ]
                                    div [ _class "card-block" ] [
                                        ul [ _class "list-group backbone-list"; _id "calibration-list"; _koBind "foreach: myMicroscopes" ] [
                                            li [ _class "list-group-item"; _koBind "text: name, click: function() { $parent.changeView(2, $data); $parent.setActiveCalibrationTab($element); }" ] []
                                        ]
                                        button [ _class "btn btn-outline-secondary"; _id "calibration-add-new-button"; _koBind "click: function() { changeView(CalibrateView.ADD_MICROSCOPE); setActiveCalibrationTab(null); }, visible: $parent.currentView() != CalibrateView.ADD_MICROSCOPE" ] [ encodedText "Set up a microscope" ]
                                    ]
                                ]
                            ]
                            Grid.column Medium 8 [
                                addMicroscope
                                editMicroscope
                            ]
                        ]
                    ]
                    div [ _class "modal-footer" ] [
                        button [ _koBind "click: function() { $root.switchView(CurrentView.MASTER) }"; _class "btn btn-outline-secondary" ] [ encodedText "Done" ]
                    ]
                ]
            ]
        ]

    let requiredSymbol = span [ _class "required-symbol" ] [ encodedText "*" ]

    let recordSlide =
        div [ _koBind "BSModal: currentView() == CurrentView.ADD_SLIDE_RECORD, if: currentView() == CurrentView.ADD_SLIDE_RECORD"; _class "modal"; _role "dialog"; _data "keyboard" "false"; _data "backdrop" "static" ] [
            div [ _koBind "with: newSlideVM"; _class "modal-dialog modal-lg" ] [
                div [ _class "modal-content" ] [
                    div [ _class "modal-header" ] [
                        h5 [ _class "modal-title" ] [ encodedText "Add a Slide: Single" ]
                        button [ _type "button"; _class "close"; _koBind "click: function() { $parent.switchView(CurrentView.MASTER); }" ] [ rawText "&times" ]
                    ]
                    div [ _class "modal-body" ] [
                        p [] [ 
                            encodedText "We require information on the taxonomic identity, sample origin, spatial properties, and temporal properties for every slide. Please fill these in below. For more information,"
                            a [ _href "/Guide"; _target "blank" ] [ encodedText "please refer to the GPP guide" ]
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
                        div [] [
                            div [ _class "custom-control custom-radio" ] [
                                input [ _class "custom-control-input"; _type "radio"; _name "sampleType"; _id "botanical"; _value "botanical"; _koBind "checked: identificationMethod" ]
                                label [ _class "custom-control-label" ] [
                                    strong [] [ encodedText "From plant material ('directly'). " ]
                                    encodedText "Pollen or spores sampled from plant material of a known taxonomic identity." ]
                            ]
                            div [ _class "custom-control custom-radio" ] [
                                input [ _class "custom-control-input"; _type "radio"; _name "sampleType"; _id "morphological"; _value "morphological"; _koBind "checked: identificationMethod" ]
                                label [ _class "custom-control-label" ] [
                                    strong [] [ encodedText "Morphological. " ]
                                    encodedText "A taxonomic identification attributed to the grains by morphology, for example using pollen keys." ]
                            ]
                            div [ _class "custom-control custom-radio" ] [
                                input [ _class "custom-control-input"; _type "radio"; _name "sampleType"; _id "environmental"; _value "environmental"; _koBind "checked: identificationMethod" ]
                                label [ _class "custom-control-label" ] [
                                    strong [] [ encodedText "Environmental. " ]
                                    encodedText "The pollen was extracted from an environmental sample, for example surface water or a pollen trap. The taxonomic identification has been constrained by species occuring known to occur in this area." ]
                            ]
                        ]
                        br []
                        p [] [
                            encodedText "This reference slide is of "
                            select [ _koBind "value: rank"; _class "form-control form-control-sm inline-dropdown" ] [
                                option [ _value "Species" ] [ encodedText "Species" ]
                                option [ _value "Genus" ] [ encodedText "Genus" ]
                                option [ _value "Family" ] [ encodedText "Family" ]
                            ]
                            encodedText "rank."
                        ]
                        p [] [ encodedText "Please enter the original taxonomic identity given to the slide:"; requiredSymbol ]
                        Grid.row [
                            Grid.column Small 3 [
                                input [ _koBind "value: family, event: { blur: capitaliseFirstLetter($element), keyup: suggest($element, 'Family') }"; _type "text"; _id "original-Family"; _class "form-control"; _autocomplete "off"; _placeholder "Family"; ]
                                div [ _class "dropdown-menu taxon-dropdown shadow"; _id "FamilyList"; _style "display:none" ] []
                            ]
                            Grid.column Small 3 [
                                input [ _koBind "value: genus, enable: rank() != 'Family', event: { blur: capitaliseFirstLetter($element), keyup: suggest($element, 'Genus'), blur: disable('Genus') }"; _type "text"; _id "original-Genus"; _class "form-control"; _autocomplete "off"; _placeholder "Genus"; ]
                                div [ _class "dropdown-menu taxon-dropdown shadow"; _id "GenusList"; _style "display:none" ] []
                            ]
                            Grid.column Small 3 [
                                input [ _koBind "value: species, disable: rank() != 'Species', event: { blur: disable('Species'), keyup: suggest($element, 'Species') }"; _type "text"; _id "original-Species"; _class "form-control"; _autocomplete "off"; _placeholder "Species"; ]
                                div [ _class "dropdown-menu taxon-dropdown shadow"; _id "SpeciesList"; _style "display:none" ] []
                            ]
                            Grid.column Small 3 [
                                input [ _koBind "value: author, disable: rank() != 'Species', event: { blur: capitaliseFirstLetter($element) }"; _type "text"; _class "form-control"; _autocomplete "off"; _placeholder "Auth." ]
                            ]
                        ]
                        small [ _id "taxon-help"; _class "form-text text-muted" ] [ encodedText "This identity will be validated against the taxonomic backbone. If / when taxonomic changes occur, or have occurred, these will be reflected on this slide automatically." ]
                        button [ _class "btn btn-secondary"; _koBind "visible: newSlideTaxonStatus() == null, click: validateTaxon, enable: isValidTaxonSearch"; _style "margin-bottom:0.5em" ] [ encodedText "Validate Taxon" ]
                        div [ _koBind "visible: newSlideTaxonStatus, if: newSlideTaxonStatus" ] [
                            div [ _koBind "visible: newSlideTaxonStatus() == 'Error'" ] [ encodedText "There was a problem communicating with the taxonomic backbone." ]
                            div [ _koBind "if: newSlideTaxonStatus() != 'Error'" ] [
                                div [ _class "alert alert-success"; _koBind "visible: newSlideTaxonStatus()[0].taxonomicStatus == 'accepted'" ] [
                                    p [] [ strong [] [ encodedText "This taxon is an accepted name." ] ]
                                    p [] [ encodedText "The taxon record will include the original taxonomic name (as stated above), but the record will be displayed under the following current name on the Master Reference Collection:" ]
                                    p [] [
                                        span [ _koBind "text: newSlideTaxonStatus()[0].family" ] []
                                        encodedText " "
                                        span [ _koBind "text: newSlideTaxonStatus()[0].genus" ] []
                                        encodedText " "
                                        span [ _koBind "text: newSlideTaxonStatus()[0].species" ] []
                                        encodedText " "
                                        span [ _koBind "text: newSlideTaxonStatus()[0].namedBy" ] []
                                    ]
                                ]
                                div [ _class "alert alert-success"; _koBind "visible: newSlideTaxonStatus().length == 1 && newSlideTaxonStatus()[0].taxonomicStatus == 'synonym'" ] [
                                    p [] [ 
                                        str "This taxon is a synonym of "
                                        span [ _koBind "text: newSlideTaxonStatus().length" ] []
                                        str "."
                                    ]
                                ]
                                div [ _class "alert alert-danger"; _koBind "visible: newSlideTaxonStatus() != 'Error' && newSlideTaxonStatus().length > 1" ] [
                                    p [] [ 
                                        str "There are "
                                        span [ _koBind "text: newSlideTaxonStatus().length" ] []
                                        str " plausable matching taxa in the GPP's taxonomic backbone. If you have more details (e.g. name authorship), please enter it to clear ambuguity. The possible names are:"
                                    ]
                                    ul [ _koBind "foreach: newSlideTaxonStatus" ] [
                                        li [ _koBind "text: latinName + ' ' + namedBy + ' (' + taxonomicStatus + ' name)'" ] []
                                    ]
                                ]
                                div [ _class "alert alert-warning"; _koBind "visible: newSlideTaxonStatus() != 'Error' && newSlideTaxonStatus().length == 1 && newSlideTaxonStatus()[0].taxonomicStatus == 'doubtful'" ] [
                                    p [] [ str "This taxon is currently unverified. We are not confident of it's validity, but will accept this slide. The slde will not be visible in the Master Reference Collection until the taxon can be verified."]
                                ]
                                div [ _class "alert alert-danger"; _koBind "visible: newSlideTaxonStatus().length == 0" ] [
                                    p [] [ str "This taxon was not recognised. Please check you entered the name correctly, or enquire with us." ]
                                ]
                            ]
                        ]
                        
                        h5 [] [ str "2. Sample Origin" ]
                        hr []
                        div [ _koBind "visible: identificationMethod() == 'botanical'" ] [
                            p [] [ str "Your pollen or spore sample was taken directly from plant material. We require further information about the nature of the plant identification." ]
                            div [ _class "form-group row" ] [
                                label [ _class "col-sm-2 col-form-label" ] [
                                    str "Plant Identification Method"
                                    requiredSymbol
                                ]
                                Grid.column Small 8 [
                                    select [ _koBind "value: plantIdMethod"; _class "form-control" ] [
                                        option [ _value "unknown" ] [ str "Unknown" ]
                                        option [ _value "voucher" ] [ str "Herbarium Voucher" ]
                                        option [ _value "livingCollection" ] [ str "Plant in a Living Collection" ]
                                        option [ _value "field" ] [ str "Identification in the Field" ]
                                    ]
                                ]
                            ]
                            div [ _class "form-group row"; _koBind "visible: plantIdMethod() == 'voucher'" ] [
                                label [ _class "col-sm-2 col-form-label" ] [ str "Herbarium Voucher Information"; requiredSymbol ]
                                Grid.column Small 4 [
                                    input [ _koBind "value: institutionCode"; _placeholder "Herbarium Code"; _class "form-control" ]
                                    small [ _class "form-text text-muted" ] [ 
                                        str "Please enter a recognised herbarium code, as specified in "
                                        a [ _href "http://sweetgum.nybg.org/science/ih/"; _target "_blank" ] [ str "the Index Herbariorum" ]
                                        str "."
                                    ]
                                ]
                                Grid.column Small 4 [
                                    input [ _koBind "value: institutionInternalId"; _placeholder "Internal ID"; _class "form-control" ]
                                    small [ _class "form-text text-muted" ] [ str "A barcode or other unique identifier for this specimen used by the herbarium." ]
                                ]
                            ]
                            div [ _class "form-group row"; _koBind "visible: plantIdMethod() == 'livingCollection'" ] [
                                label [ _class "col-sm-2 col-form-label" ] [ str "Living Collection Information"; requiredSymbol ]
                                Grid.column Small 4 [
                                    input [ _koBind "value: institutionCode"; _placeholder "BGCI Code"; _class "form-control" ]
                                    small [ _class "form-text text-muted" ] [ 
                                        str "Please enter a recognised Botanic Gardens Conservation International code, as specified in "
                                        a [ _href "https://www.bgci.org/garden_search.php"; _target "_blank" ] [ str "the BGCI online database" ]
                                        str "."
                                    ]
                                ]
                                Grid.column Small 4 [
                                    input [ _koBind "value: institutionInternalId"; _placeholder "Internal ID"; _class "form-control" ]
                                    small [ _class "form-text text-muted" ] [ str "A barcode or other unique identifier for this specimen used by the botanic garden." ]
                                ]
                            ]
                            div [ _class "form-group row"; _koBind "visible: plantIdMethod() == 'field'" ] [
                                label [ _class "col-sm-2 col-form-label" ] [ str "Who identified this plant in the field?"; requiredSymbol ]
                                Grid.column Small 4 [ input [ _koBind "value: identifiedByFirstNames"; _placeholder "Forenames"; _class "form-control" ] ]
                                Grid.column Small 4 [ input [ _koBind "value: identifiedByLastName"; _placeholder "Surname"; _class "form-control" ] ]
                            ]
                        ]
                        div [ _koBind "visible: identificationMethod() == 'botanical' || identificationMethod() == 'field'"; _class "form-group row" ] [
                            label [ _for "inputCollectionYear"; _class "col-sm-2 col-form-label" ] [ str "Year Sample Taken" ]
                            Grid.column Small 5 [
                                div [ _class "input-group mb-3" ] [
                                    input [ _id "inputCollectionYear"; _koBind "value: yearCollected"; _type "number"; _class "form-control"; _aria "describedby" "year-addon" ]
                                    div [ _class "input-group-append" ] [
                                        span [ _class "input-group-text"; _id "year-addon" ] [ str "Calendar Year" ]
                                    ]
                                ]
                            ]
                        ]
                        div [ _class "form-group row"; _koBind "visible: plantIdMethod() == 'field'" ] [
                            label [ _class "col-sm-2 col-form-label" ] [ str "Who collected the plant or pollen from its natural environment?" ]
                            Grid.column Small 4 [ input [ _koBind "value: collectedByFirstNames"; _placeholder "Forenames"; _class "form-control" ] ]
                            Grid.column Small 4 [ input [ _koBind "value: collectedByLastName"; _placeholder "Surname"; _class "form-control" ] ]
                        ]
                        div [ _class "form-group row" ] [
                            label [ _class "col-sm-2 col-form-label" ] [ str "Location" ]
                            Grid.column Small 3 [
                                select [ _koBind "value: locationType"; _class "form-control input-sm inline-dropdown" ] [
                                    option [ _value "Unknown" ] [ str "Unknown" ]
                                    option [ _value "Continent" ] [ str "Continent" ]
                                    option [ _value "Country" ] [ str "Country" ]
                                    option [ _value "Locality" ] [ str "Locality" ]
                                ]
                            ]
                            div [ _class "col-sm-3"; _koBind "visible: locationType() == 'Locality'" ] [
                                input [ _koBind "value: locality"; _class "form-control"; _id "locationLocality"; _placeholder "Locality" ]
                            ]
                            div [ _class "col-sm-3"; _koBind "visible: locationType() == 'Locality'" ] [
                                input [ _koBind "value: district"; _class "form-control"; _id "locationDistrict"; _placeholder "District" ]
                            ]
                            div [ _class "col-sm-3"; _koBind "visible: locationType() == 'Locality'" ] [
                                input [ _koBind "value: region"; _class "form-control"; _id "locationRegion"; _placeholder "Region" ]
                            ]
                            div [ _class "col-sm-3"; _koBind "visible: locationType() == 'Locality' || locationType() == 'Country'" ] [
                                input [ _koBind "value: country"; _class "form-control"; _id "locationCountry"; _placeholder "Country" ]
                            ]
                            div [ _class "col-sm-3"; _koBind "visible: locationType() == 'Continent'" ] [
                                select [ _koBind "value: continent"; _class "form-control input-sm inline-dropdown" ] [
                                    option [ _value "Africa" ] [ str "Africa" ]
                                    option [ _value "Asia" ] [ str "Asia" ]
                                    option [ _value "Europe" ] [ str "Europe" ]
                                    option [ _value "NorthAmerica" ] [ str "North America" ]
                                    option [ _value "SouthAmerica" ] [ str "South America" ]
                                    option [ _value "Antarctica" ] [ str "Antarctica" ]
                                    option [ _value "Australia" ] [ str "Australia" ]
                                ]
                            ]
                        ]

                        h5 [] [ str "3. Slide Preperation" ]
                        hr []
                        div [ _class "form-group row"] [
                            label [ _class "col-sm-2 col-form-label" ] [ str "Slide Prepared By" ]
                            Grid.column Small 3 [ input [ _koBind "value: preparedByFirstNames"; _placeholder "Forenames"; _class "form-control" ] ]
                            Grid.column Small 3 [ input [ _koBind "value: preparedByLastName"; _placeholder "Surname"; _class "form-control" ] ]
                        ]
                        div [ _class "form-group row" ] [
                            label [ _for "preperationMethod"; _class "col-sm-2 col-form-label" ] [ str "Chemical Treatment" ]
                            Grid.column Small 10 [
                                select [ _koBind "value: preperationMethod"; _class "form-control input-sm inline-dropdown" ] [
                                    option [ _value "unknown" ] [ str "Unknown" ]
                                    option [ _value "fresh" ] [ str "Fresh Grains (no processing)" ]
                                    option [ _value "acetolysis" ] [ str "Acetolysis" ]
                                    option [ _value "hf" ] [ str "Hydroflouric Acid (HF)" ]
                                ]
                                small [ _class "form-text text-muted" ] [ str "If you have not applied any chemical treatments, please select 'Fresh Grains'." ]
                            ]
                        ]
                        div [ _class "form-group row" ] [
                            label [ _class "col-sm-2 col-form-label" ] [ str "Mounting Material" ]
                            Grid.column Small 10 [
                                select [ _koBind "value: mountingMaterial"; _class "form-control input-sm inline-dropdown" ] [
                                    option [ _value "unknown" ] [ str "Unknown" ]
                                    option [ _value "siliconeoil" ] [ str "Silicone Oil" ]
                                    option [ _value "glycerol" ] [ str "Glycerol" ]
                                ]
                                small [ _class "form-text text-muted" ] [ str "Which fixant was used to prepare the slide?" ]
                            ]
                        ]
                        div [ _class "form-group row" ] [
                            label [ _class "col-sm-2 col-form-label" ] [ str "Preperation Date" ]
                            Grid.column Small 5 [
                                div [ _class "input-group mb-3" ] [
                                    input [ _koBind "value: yearPrepared"; _type "number"; _class "form-control"; _aria "described-by" "year-addon" ]
                                    div [ _class "input-group-append" ] [
                                        span [ _class "input-group-text"; _id "year-addon" ] [ str "Calendar Year" ]
                                    ]
                                ]
                                small [ _class "form-text text-muted" ] [ str "When was this slide made from the plant material or environmental sample?" ]
                            ]
                        ]
                        div [ _class "alert alert-danger"; _koBind "visible: validationErrors().length > 0, if: validationErrors().length > 0" ] [
                            str "We could not add this slide, as some information did not pass validation. Please address the problems listed below and try again:"
                            ul [ _koBind "foreach: validationErrors" ] [
                                li [ _koBind "text: $data.property + ': ' + $data.errors[0]" ] []
                            ]
                        ]
                    ]
                    div [ _class "modal-footer" ] [
                        button [ _koBind "click: function() { submit($root) }, disable: !isValidAddSlideRequest() || isProcessing()"; _type "button"; _class "btn btn-primary" ] [ str "Record Slide" ]
                    ]
                ]
            ]
        ]

let appView =
    [
        span [ _id "digitise-app" ] []
        link [ _rel "stylesheet"; _href "https://cdn.datatables.net/1.10.15/css/dataTables.bootstrap4.min.css"]
        Partials.addCollectionModal Requests.Empty.addCollection
        Partials.recordSlide
        Partials.slideDetail
        Partials.calibrationModal
        Partials.tutorialModal

        div [ _class "btn-toolbar mb-3"; _role "toolbar" ] [
            button [ _koBind "click: function() { switchView(CurrentView.ADD_COLLECTION) }"; _class "btn btn-outline-secondary" ] [ encodedText "Create new collection" ]
            button [ _koBind "click: function() { switchView(CurrentView.CALIBRATE) }"; _class "btn btn-outline-secondary" ] [ encodedText "Setup Microscope Calibrations" ]            
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
                                        button [ _koBind "click: publish, visible: activeCollection().awaitingReview == false"; _class "btn btn-primary"; _id "collection-publish-button" ] [
                                            Icons.fontawesome "beer"
                                            encodedText "Request Publication"
                                        ]
                                        p [ _koBind "visible: activeCollection().awaitingReview" ] [ encodedText "Your collection has been submitted for review. The outcome will appear here." ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                    table [ _class "table"; _id "slides-data-table" ] [
                        thead [] [
                            tr [] [
                                th [] [ encodedText "ID" ]
                                th [] [ encodedText "Original Family" ]
                                th [] [ encodedText "Original Genus" ]
                                th [] [ encodedText "Original Species" ]
                                th [] [ encodedText "Current Taxon" ]
                                th [] [ encodedText "Number of Images Uploaded" ]
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
                                    button [ _koBind "click: function() { $parent.switchView(CurrentView.SLIDE_DETAIL, $data) }, enable: function() { $data.Voided() == false }"; _class "btn btn-sm btn-outline-secondary collection-slide-upload-image-button" ] [ encodedText "Details" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ] |> Layout.standard [] "Digitise Dashboard" "Use this tool to digitise your collections. A digitised slide contains two components: (1) metadata, and (2) images."
