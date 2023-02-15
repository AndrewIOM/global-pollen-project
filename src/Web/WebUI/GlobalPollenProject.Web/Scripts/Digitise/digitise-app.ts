import * as ko from 'knockout'
import 'bootstrap-datepicker';
import {Viewer, ViewerEvent} from "../Components/Viewer/viewer";
import {FocusSlider} from "../Components/Viewer/focusslider";
import {MeasuringLine, MeasuringLineEvent, MeasuringLineState} from "../Components/Viewer/measuringline";
import {ScaleBar} from "../Components/Viewer/scalebar";

////////////////////////
/// Setup - KO Bindings
////////////////////////

ko.bindingHandlers.BSModal = {
    init: function (element, valueAccessor) {
        var value = valueAccessor();
        $(element).modal({
            keyboard: false,
            show: ko.unwrap(value)
        });
    },
    update: function (element, valueAccessor) {
        var value = valueAccessor();
        ko.unwrap(value) ? $(element).modal('show') : $(element).modal('hide');
    }
};

////////////////////////
/// Root View Model
////////////////////////

export function activate(container: HTMLElement) {
    jQuery(function() {
        $('#tutorialModal').modal('show');
        var vm = new DigitiseViewModel();
        vm.switchView(CurrentView.MASTER);
        ko.applyBindings(vm);
    });
}

export enum CurrentView {
    MASTER = 1,
    DETAIL = 2,
    ADD_COLLECTION = 3,
    ADD_SLIDE_RECORD = 4,
    SLIDE_DETAIL = 5,
    CALIBRATE = 6
};
(window as { CurrentView? }).CurrentView = CurrentView;

export enum SlideDetailTab {
    OVERVIEW = 1,
    UPLOAD_STATIC = 2,
    UPLOAD_FOCUSABLE = 3
};

function DigitiseViewModel() {
    var self = this;
    let apiPrefix = "/api/v1/digitise/";

    self.CurrentView = CurrentView;
    self.SlideDetailTab = SlideDetailTab;
    self.CalibrateView = CalibrateView;

    self.currentView = ko.observable(CurrentView.MASTER);
    self.myCollections = ko.observableArray([]);
    self.activeCollection = ko.observable(null);
    self.newCollectionVM = ko.observable(null);
    self.newSlideVM = ko.observable(null);
    self.slideDetailVM = ko.observable(null);
    self.calibrateVM = ko.observable(null);

    self.refreshCollectionList = function () {
        $.ajax({
            url: apiPrefix + "collection/list",
            cache: false,
            success: function (serverCols) {
                $("#collection-list").show();
                $("#collection-loading-spinner").hide();
                self.myCollections(serverCols);
            }
        });
    };

    self.switchView = function (view, data) {
        switch (view) {
            case CurrentView.MASTER:
                self.refreshCollectionList();
                self.activeCollection(null);
                self.currentView(view);
                break;
            case CurrentView.DETAIL:
                $.ajax({
                        url: apiPrefix + "collection/" + data.id,
                        type: "GET"
                    })
                    .done(function (col) {
                        self.activeCollection(col);
                        self.currentView(view);
                    });
                break;
            case CurrentView.ADD_COLLECTION:
                self.newCollectionVM(new AddCollectionViewModel());
                self.currentView(view);
                break;
            case CurrentView.ADD_SLIDE_RECORD:
                self.newSlideVM(new RecordSlideViewModel(self.activeCollection()));
                self.currentView(view);
                break;
            case CurrentView.SLIDE_DETAIL:
                self.slideDetailVM(new SlideDetailViewModel(data));
                self.slideDetailVM().loadCalibrations();
                self.currentView(view);
                break;
            case CurrentView.CALIBRATE:
                self.calibrateVM(new CalibrateViewModel());
                self.calibrateVM().refreshMicroscopes();
                self.currentView(view);
                break;
        }
    };

    self.setActiveCollectionTab = function (element) {
        $(element).parent().find("li").removeClass("active");
        $(element).addClass("active");
    };

    self.publish = function () {
        let requestUrl = "/api/v1/digitise/collection/publish/" + self.activeCollection().id;
        $.ajax({
                url: requestUrl,
                type: "GET"
            })
            .done(function (data) {
                alert("Your collection has been submitted for review. You will be notified of the outcome on this page when it is available.");
            });
    };
}

////////////////////////
/// Create R. Collection
////////////////////////

function AddCollectionViewModel() {
    let self = this;
    self.CurrentView = CurrentView;
    self.name = ko.observable();
    self.description = ko.observable();
    self.curatorFirstNames = ko.observable();
    self.curatorSurname = ko.observable();
    self.accessMethod = ko.observable();
    self.institutionName = ko.observable();
    self.institutionUrl = ko.observable();
    self.email = ko.observable();

    self.isProcessing = ko.observable();
    self.validationErrors = ko.observableArray();    
    
    self.submit = function (rootVM) {
        self.isProcessing(true);
        let req = {
            name: self.name(),
            description: self.description(),
            curatorFirstNames: self.curatorFirstNames(),
            curatorSurname: self.curatorSurname(),
            curatorEmail: self.email(),
            accessMethod: self.accessMethod(),
            institution: self.institutionName(),
            institutionUrl: self.institutionUrl()
        };
        $.ajax({
            url: "/api/v1/digitise/collection/start",
            type: "POST",
            data: JSON.stringify(req),
            dataType: "json",
            contentType: "application/json",
            success: function () {
                rootVM.switchView(CurrentView.MASTER);
            },
            statusCode: {
                400: err => {
                    self.validationErrors(err.responseJSON.errors);
                    self.isProcessing(false);
                    $('.modal').animate({ scrollTop: 0 }, 'slow');
                },
                500: data => {
                    self.validationErrors([{'property': 'unknown', 'errors': ['Internal error. Please try again later.']}]);
                    self.isProcessing(false);
                    $('.modal').animate({ scrollTop: 0 }, 'slow');
                }
            }
        });
    };
}

////////////////////////
/// Record Slide Dialog
////////////////////////

function RecordSlideViewModel(currentCollection) {
    let self = this;
    self.CurrentView = CurrentView;
    self.collection = ko.observable(currentCollection);
    self.rank = ko.observable("");
    self.family = ko.observable("");
    self.genus = ko.observable("");
    self.species = ko.observable("");
    self.author = ko.observable("");
    self.newSlideTaxonStatus = ko.observable(null);
    self.currentTaxon = ko.observable();
    self.identificationMethod = ko.observable();
    self.existingId = ko.observable();
    self.yearCollected = ko.observable();
    self.locationType = ko.observable();
    self.locality = ko.observable();
    self.district = ko.observable();
    self.region = ko.observable();
    self.country = ko.observable();
    self.continent = ko.observable();
    self.yearPrepared = ko.observable();
    self.preperationMethod = ko.observable();
    self.mountingMaterial = ko.observable();
    self.collectedByFirstNames = ko.observable();
    self.collectedByLastName = ko.observable();
    self.preparedByFirstNames = ko.observable();
    self.preparedByLastName = ko.observable();

    self.plantIdMethod = ko.observable();
    self.institutionCode = ko.observable();
    self.institutionInternalId = ko.observable();
    self.identifiedByFirstNames = ko.observable();
    self.identifiedByLastName = ko.observable();

    self.rank.subscribe(function (rank) {
        if (rank == "Family") {
            self.genus("");
            self.species("");
            self.author("");
        } else if (rank == "Genus") {
            self.species("");
            self.author("");
        }
    });

    // When original taxon is changed, remove current allocated taxon
    self.family.subscribe(function(f) { self.newSlideTaxonStatus(null); });
    self.genus.subscribe(function(g) { self.newSlideTaxonStatus(null); });
    self.species.subscribe(function(s) { self.newSlideTaxonStatus(null); });
    self.author.subscribe(function(a) { self.newSlideTaxonStatus(null); });

    self.isValidTaxonSearch = ko.computed(function () {
        if (self.rank() == "Family" && self.family().length > 0) return true;
        if (self.rank() == "Genus" && self.family().length > 0 && self.genus().length > 0) return true;
        if (self.rank() == "Species" && self.genus().length > 0 && self.species().length > 0) return true;
        return false;
    }, self);

    self.isValidAddSlideRequest = ko.computed(function () {
        console.log(self.currentTaxon());
        if (self.rank() == "") return false;
        if (self.currentTaxon() == undefined) return false;
        if (self.identificationMethod() == undefined) return false;
        return true;
    }, self);

    self.isProcessing = ko.observable(false);
    self.validationErrors = ko.observableArray([]);

    self.typingTimer;
    const doneTypingInterval = 100;
    
    self.suggest = (entryBox:HTMLInputElement, rank:string) => {
        clearTimeout(self.typingTimer);
        if (entryBox.value) {
            self.typingTimer = setTimeout(() => {
                self.updateList(entryBox, rank);
            }, doneTypingInterval);
        }
    };
    
    //Update suggestion list when timeout complete
    self.updateList = (entryBox:HTMLInputElement, rank:string) => {
        let query = '';
        let value = entryBox.value;
        if (rank == "Family" || rank == "Genus") {
            value = this.capitaliseString(value);
        }
        //Combine genus and species for canonical name
        if (rank == 'Species') {
            const genus = (<HTMLInputElement>document.getElementById('original-Genus')).value;
            query += genus + " ";
        }
        query += value;
        if (value != "") {
            const request = "/api/v1/backbone/search?rank=" + rank + "&latinName=" + query;
            $.ajax({
                url: request,
                type: "GET"
            }).done(data => {
                const list = document.getElementById(rank + 'List');
                $('#' + rank + 'List').css('display', 'block');
                list.innerHTML = "";
                for (let i = 0; i < data.length; i++) {
                    if (i > 10) continue;
                    const option = document.createElement('li');
                    const link = document.createElement('a');
                    option.appendChild(link);
                    link.innerHTML = data[i];

                    let matchCount = 0;
                    for (let j = 0; j < data.length; j++) {
                        if (data[j].latinName == data[i]) {
                            matchCount++;
                        }
                    }
                    link.addEventListener('click', e => {
                        const name = link.innerHTML;
                        if (rank == 'Species') {
                            $('#original-Species').val(name.split(' ')[1]).change();
                            $('#original-Genus').val(name.split(' ')[0]).change();
                        } else if (rank == 'Genus') {
                            $('#original-Genus').val(name).change();
                        } else if (rank == 'Family') {
                            $('#original-Family').val(name).change();
                        }
                        $('#' + rank + 'List').fadeOut();
                    });
                    list.appendChild(option);
                }
            });
        }
    }
    
    self.disable = (rank) => {
        let element;
        if (rank == 'Family') element = 'FamilyList';
        if (rank == 'Genus') element = 'GenusList';
        if (rank == 'Species') element = 'SpeciesList';
        setTimeout(func, 100);
        function func() {
            $('#' + element).fadeOut();
        }
    }

    self.validateTaxon = function () {
        self.isProcessing(true);
        var query: string;
        if (self.rank() == "Family") {
            query = "rank=Family&family=" + self.family() + "&latinname=" + self.family();
        } else if (self.rank() == 'Genus') {
            query = "rank=Genus&family=" + self.family() + "&genus=" + self.genus() + "&latinname=" + self.genus();
        } else if (self.rank() == "Species") {
            query = "rank=Species&family=" + self.family() + "&genus=" + self.genus() + "&species=" + self.species() + "&latinname=" + self.genus() + " " + self.species() + "&authorship=" + encodeURIComponent(self.author());
        }
        $.ajax({
                url: "/api/v1/backbone/trace?" + query,
                type: "GET"
            })
            .done(function (data) {
                if (data.length == 1 && data[0].taxonomicStatus == "accepted" || data[0].taxonomicStatus == "doubtful") self.currentTaxon(data[0].id);
                self.newSlideTaxonStatus(data);
                self.isProcessing(false);
            });
    };

    self.getRequest = function () {
        let firstNames = isEmpty(self.identifiedByFirstNames()) ? [] : self.identifiedByFirstNames().split(' ');
        let preparedByFirstNames = isEmpty(self.preparedByFirstNames()) ? [] : self.preparedByFirstNames().split(' ');
        let collectedByFirstNames = isEmpty(self.collectedByFirstNames()) ? [] : self.collectedByFirstNames().split(' ');
        let request = {
            Collection: self.collection().id,
            ExistingId: self.existingId(),
            OriginalFamily: self.family(),
            OriginalGenus: self.genus(),
            OriginalSpecies: self.species(),
            OriginalAuthor: self.author(),
            ValidatedTaxonId: self.currentTaxon(),
            SamplingMethod: self.identificationMethod(),
            YearCollected: parseInt(self.yearCollected()),
            YearSlideMade: parseInt(self.yearPrepared()),
            LocationType: self.locationType(),
            LocationLocality: self.locality(),
            LocationDistrict: self.district(),
            LocationRegion: self.region(),
            LocationCountry: self.country(),
            LocationContinent: self.continent(),
            PreperationMethod: self.preperationMethod(),
            MountingMaterial: self.mountingMaterial(),
            PreparedByFirstNames: preparedByFirstNames,
            PreparedBySurname: self.preparedByLastName(),
            CollectedByFirstNames: collectedByFirstNames,
            CollectedBySurname: self.collectedByLastName(),
            PlantIdMethod: {
                Method: self.plantIdMethod(),
                InstitutionCode: self.institutionCode(),
                InternalId: self.institutionInternalId(),
                IdentifiedByFirstNames: firstNames,
                IdentifiedBySurname: self.identifiedByLastName()
            }
        };
        return request;
    };

    self.capitaliseFirstLetter = function (element) {
        const currentValue = $(element).val();
        if (typeof(currentValue) == "string") {
            $(element).val(this.capitaliseString(currentValue));
        }
    };

    self.capitaliseString = (string:string) => {
        return string.charAt(0).toUpperCase() + string.slice(1);
    }

    self.submit = function (rootVM) {
        self.isProcessing(true);
        let request = self.getRequest();
        $.ajax({
                url: "/api/v1/digitise/collection/slide/add",
                type: "POST",
                data: JSON.stringify(request),
                dataType: "json",
                contentType: "application/json",
                success: function () {
                    rootVM.switchView(CurrentView.DETAIL, rootVM.activeCollection());
                },
                statusCode: {
                    400: function (err) {
                        console.log(err.responseJSON);
                        self.validationErrors(err.responseJSON.errors);
                        self.isProcessing(false);
                        $('.modal').animate({ scrollTop: 0 }, 'slow');
                    },
                    500: function (data) {
                        self.validationErrors([{'property': 'unknown', 'errors': ['Internal error. Please try again later.']}]);
                        self.isProcessing(false);
                        $('.modal').animate({ scrollTop: 0 }, 'slow');
                    }
                }
            });
    };
}

////////////////////////
/// Add Image View
////////////////////////

function SlideDetailViewModel(detail) {
    let self = this;
    self.CurrentView = CurrentView;
    self.currentTab = ko.observable(SlideDetailTab.OVERVIEW);
    self.slideDetail = ko.observable(detail);
    self.loadedStaticImage = ko.observable(false);
    self.loadedFocusImages = ko.observable(false);
    self.uploadPercentage = ko.observable(null);
    self.viewer = null;
    self.slider = null;
    self.measuringLine = null;
    self.scaleBar = null;
    self.validationErrors = ko.observableArray([]);
    self.isProcessing = ko.observable(false);
    self.calibrations = ko.observableArray();

    self.selectedMicroscope = ko.observable(null);
    self.selectedMagnification = ko.observable(null);

    // Add slide request
    self.framesBase64 = ko.observableArray([]);
    self.floatingCal = ko.observable();
    self.measuredDistance = ko.observable();
    self.digitisedYear = ko.observable(null);

    self.isValidStaticRequest = ko.computed(() => {
        if (self.isProcessing()) return false;
        if (self.floatingCal() == null) return false;
        if (self.measuredDistance() == null) return false;
        if (self.digitisedYear() == null) return false;
        if (self.measuredDistance() <= 0) return false;
        return true;
    }, self);

    self.isValidFocusRequest = ko.computed(function () {
        if (self.isProcessing()) return false;
        if (self.selectedMicroscope() == null) return false;
        if (self.selectedMagnification() == null) return false;
        if (self.digitisedYear() == null) return false;
        return true;
    }, self);

    self.voidSlide = function(rootVM) {
        let request = { slideId: self.slideDetail().collectionSlideId, collectionId: self.slideDetail().collectionId };
        if (window.confirm("Are you sure? You cannot recover a voided slide. " + self.slideDetail().collectionSlideId + " will be lost. Continue?")) {
            $.ajax({
                url: "/api/v1/digitise/collection/slide/void",
                type: "POST",
                data: JSON.stringify(request),
                dataType: "json",
                contentType: "application/json"
            })
            .done(function (data) {
                rootVM.switchView(CurrentView.DETAIL, {
                    Id: self.slideDetail().collectionId
                });
            })
        }
    }

    self.loadCalibrations = function () {
        $.ajax({
                url: "/api/v1/digitise/calibration/list",
                type: "GET"
            })
            .done(function (cals) {
                self.calibrations(cals);
            });
    };

    self.switchTab = function (tab) {
        self.currentTab(tab);
        if (tab != SlideDetailTab.UPLOAD_STATIC) {
            self.loadedStaticImage(false);
            if (self.viewer != null) {
                self.viewer.dispose();
            }

            if (self.measuringLine != null) {
                self.measuringLine.dispose();
            }

            $("#static-image-previewer-container").html("");
            $("#measuredDistance").val("").change();
            $("#digitisedYearStatic").val("").change();
            $("#digitisedYearStatic").datepicker({
                format: " yyyy",
                maxViewMode: "years",
                minViewMode: "years",
                startDate: '1850',
                endDate: '+0d',
            });
        }

        if (tab != SlideDetailTab.UPLOAD_FOCUSABLE) {
            self.loadedFocusImages(false);
            if (self.viewer != null) {
                self.viewer.dispose();
            }

            if (self.slider != null) {
                self.slider.dispose();
            }

            $("#focus-image-previewer-container").html("");
            $("#digitisedYearFocus").val("").change();
            $("#digitisedYearFocus").datepicker({
                format: " yyyy",
                maxViewMode: "years",
                minViewMode: "years",
                startDate: '1850',
                endDate: '+0d',
            });

            self.selectedMicroscope(null);
            self.selectedMagnification(null);

            if (self.scaleBar != null) {
                self.scaleBar.dispose();
            }
        }
    }

    self.submit = function (rootVM, base64Array) {
        self.isProcessing(true);
        let request = {
            collectionId: self.slideDetail().collectionId,
            slideId: self.slideDetail().collectionSlideId,
            isFocusImage: base64Array.length > 1,
            framesBase64: base64Array,
            digitisedYear: parseInt(self.digitisedYear())
        }
        if(base64Array.length == 1) {
            request["floatingCalPointOneX"] = Math.round(self.floatingCal()[0][0]);
            request["floatingCalPointOneY"] = Math.round(self.floatingCal()[0][1]);
            request["floatingCalPointTwoX"] = Math.round(self.floatingCal()[1][0]);
            request["floatingCalPointTwoY"] = Math.round(self.floatingCal()[1][1]);
            request["measuredDistance"] = parseFloat(self.measuredDistance());
        } else if (base64Array.length > 1) {
            request["calibrationId"] = self.selectedMicroscope().id;
            request["magnification"] = self.selectedMagnification().level;
        } else {
            return;
        }

        $.ajax({
            url: "/api/v1/digitise/collection/slide/addimage",
            type: "POST",
            data: JSON.stringify(request),
            dataType: "json",
            contentType: "application/json",
            xhr: function () {
                var xhr = $.ajaxSettings.xhr();
                xhr.upload.onprogress = function (evt) {
                    if (evt.lengthComputable) {
                        var percentComplete = evt.loaded / evt.total;
                        self.uploadPercentage(percentComplete * 100);
                    }
                };
                return xhr;
            },
            success: function (data) {
                rootVM.switchView(CurrentView.DETAIL, {
                    id: self.slideDetail().collectionId
                });
            },
            statusCode: {
                400: err => {
                    self.validationErrors(err.responseJSON.errors);
                    self.uploadPercentage(null);
                    self.isProcessing(false);
                },
                500: data => {
                    self.validationErrors([{'property': 'unknown', 'errors': ['Internal error. Please try again later.']}]);
                    self.uploadPercentage(null);
                    self.isProcessing(false);
                }
            }
        });
    }

    self.submitFocus = function (rootVM) {
        var loaded = 0;
        var base64Array = [];
        for (var i = 0; i < self.viewer.imagePaths.length; i++) {
            convertToDataURLviaCanvas(self.viewer.imagePaths[i], function (d) {
                loaded++;
                base64Array.push(d);
                if (loaded == self.viewer.imagePaths.length) {
                    self.submit(rootVM, base64Array);
                }
            });
        }
    }

    self.submitStatic = function (rootVM) {
        convertToDataURLviaCanvas(self.viewer.imagePaths[0], function (d) {
            self.submit(rootVM, [d]);
        });
    }

    self.createStaticImageViewer = function (element) {
        $("#static-image-previewer-container").html("<div id=\"static-image-previewer\"></div>");
        $("#digitisedYearStatic").datepicker({
            format: " yyyy",
            maxViewMode: "years",
            minViewMode: "years",
            startDate: '1850',
            endDate: '+0d',
    });

        $("#measuredDistance").change(function () {
            self.measuredDistance($(this).val().toString().replace(/[^0-9.]/g, ""));
        });

        if (self.viewer != null) {
            self.viewer.dispose();
            self.loadedStaticImage(false);
        }

        if (self.measuringLine != null) {
            self.measuringLine.dispose();
        }

        var file = element.files[0];
        var reader = new FileReader();

        reader.onloadend = function (e) {
            self.base64 = reader.result;
            self.viewer = new Viewer("#static-image-previewer",
                "#static-image-previewer-canvas",
                $("#static-image-previewer").width() * 0.8, 500, [
                    self.base64
                ]
            );
            self.loadedStaticImage(true);
        }

        if (file) {
            reader.readAsDataURL(file);
        }
    }

    self.selectedMagnificationName = function () {
        if (self.selectedMagnification() == null)
            return "Select magnification"

        return self.selectedMagnification().level + "x";
    }

    self.selectedMicroscopeName = function () {
        if (self.selectedMicroscope() == null)
            return "Select microscope"

        return self.selectedMicroscope().name;
    }

    self.selectedMicroscopeMagnifications = function () {
        if (self.selectedMicroscope() == null)
            return [];

        return self.selectedMicroscope().magnifications;
    }

    self.selectMicroscope = function (element) {
        var selected = self.calibrations().find(function (e) {
            return e.id == element.id;
        });

        self.selectedMicroscope(selected);
    }

    self.selectMagnification = function (element) {
        self.selectedMagnification(element);
        self.activateFocusScaleBar();
    }

    self.createFocusImageViewer = function (element) {
        $("#focus-image-previewer-container").empty();
        $("#focus-image-previewer-container").html("<div id=\"focus-image-previewer\" style=\"width: 100%\"></div>");

        self.selectedMicroscope(null);
        self.selectedMagnification(null);

        $("#digitisedYearFocus").datepicker({
            format: " yyyy",
            maxViewMode: "years",
            minViewMode: "years",
            startDate: '1850',
            endDate: '+0d',
        });

        if (self.viewer != null) {
            self.viewer.dispose();
            self.loadedFocusImages(false);
        }

        var files = element.files;
        var loaded = 0;
        var readers = [];
        var fileData = [];

        if (files.length < 2) {
            alert("You must upload at least 2 images (shift-click/ctrl-click files to select multiple)");
            return;
        }

        for (var i = 0; i < files.length; i++) {
            var r = new FileReader();
            readers.push(r);

            readers[i].onloadend = function (e) {
                loaded++;
                fileData.push({ name: this.file.name, b64: this.result });
                
                if (loaded >= files.length) {
                    // sort by file name
                    fileData.sort(function(a, b){
                        if(a.name < b.name) return -1;
                        if(a.name > b.name) return 1;
                        return 0;
                    })

                    var base64s = [];
                    for(var j = 0; j < fileData.length; j++) {
                        base64s.push(fileData[j].b64);
                    }
                    
                    self.viewer = new Viewer("#focus-image-previewer",
                        "#focus-image-previewer-canvas",
                        $("#focus-image-previewer-container").width() * 0.8, 500, base64s
                    );
                    self.slider = new FocusSlider(self.viewer, "#focus-image-previewer-slider");
                    self.loadedFocusImages(true);

                    $(self.viewer).on(ViewerEvent.EVENT_IMAGES_MISMATCHED_SIZE, function () {
                        self.loadedFocusImages(false);
                        $("#focus-image-previewer-container").empty();
                        self.viewer.dispose();
                        self.slider.dispose();
                        alert("Error - images must all have the same dimensions (width x height)");
                    });
                }
            }
            if (files[i]) {
                readers[i].file = files[i];
                readers[i].readAsDataURL(files[i]);
            }
        }
    }

    self.activateFocusScaleBar = function () {
        if (self.viewer == null || self.selectedMagnification() == null) {
            return;
        }

        if (self.scaleBar != null) {
            self.scaleBar.dispose();
        }

        self.scaleBar = new ScaleBar(self.viewer, "#focus-image-previewer-scalebar", self.selectedMagnification().pixelWidth);
        // Manually trigger loaded images event to connect the scalebar.
        $(self.viewer.containerId).trigger(ViewerEvent.EVENT_LOADED_IMAGES);
    }

    self.activateMeasuringLine = function () {
        if (self.measuringLine != null) {
            // don't do anything if the measuring line is being drawn
            if (self.measuringLine.state == MeasuringLineState.STATE_DRAWING ||
                self.measuringLine.state == MeasuringLineState.STATE_ACTIVE) {
                return;
            }

            self.measuringLine.dispose();
        }

        self.measuringLine = new MeasuringLine(self.viewer,
            "#static-image-previewer-measuring-line", false, null,
            "Enter the actual distance below");

        self.measuringLine.activate();
        $("#slidedetail-draw-line-button").prop("disabled", true);

        $(self.measuringLine.id).on(MeasuringLineEvent.EVENT_DRAWN, () => {
            console.log("Line drawn.");
            $("#slidedetail-draw-line-button").prop("disabled", false);
            self.floatingCal(self.measuringLine.getPixelPoints());
        });
    }
}

////////////////////////
/// Calibrate View
////////////////////////

var CalibrateView = {
    MASTER: 1,
    DETAIL: 2,
    ADD_MICROSCOPE: 3
};

function CalibrateViewModel() {
    let self = this;
    self.CurrentView = CurrentView;
    self.CalibrateView = CalibrateView;
    self.currentView = ko.observable(CalibrateView.MASTER);
    self.myMicroscopes = ko.observableArray([]);
    self.newMicroscope = ko.observable(null);
    self.microscopeDetail = ko.observable(null);

    self.refreshMicroscopes = function () {
        $.ajax({
                url: "/api/v1/digitise/calibration/list",
                type: "GET"
            })
            .done(function (cals) {
                self.myMicroscopes(cals);
            })
    }

    self.changeView = function (view, data) {
        if (view == CalibrateView.MASTER) {
            self.refreshMicroscopes();
            self.microscopeDetail(null);
            self.newMicroscope(null);
            self.currentView(view);
        } else if (view == CalibrateView.DETAIL) {
            self.microscopeDetail(new ImageCalibrationViewModel(data));
            self.newMicroscope(null);
            self.currentView(view);
            // TODO: add viewer
        } else if (view == CalibrateView.ADD_MICROSCOPE) {
            self.newMicroscope(new MicroscopeViewModel());
            self.microscopeDetail(null);
            self.currentView(view);
        }
    }

    self.setActiveCalibrationTab = function (element) {
        $("#calibration-list").find("li").removeClass("active");
        
        if(element != null) {
            $(element).addClass("active");
        }
    }
}

function MicroscopeViewModel() {
    let self = this;
    self.CurrentView = CurrentView;
    self.friendlyName = ko.observable("");
    self.microscopeType = ko.observable("Compound");
    self.ocular = ko.observable(10);
    self.microscopeModel = ko.observable("");
    self.magnifications = ko.observableArray([10, 20, 40, 100]);

    self.microscopeType.subscribe(function (value) {
        if (value == "Compound") {
            self.magnifications = ko.observableArray([10, 20, 40, 100]);
        } else {
            self.magnifications = ko.observableArray([40]);
        }
    });

    self.addMag = function () {
        self.magnifications.push(10);
    }

    self.removeMag = function (mag) {
        console.log("Removing " + mag);
        self.magnifications.remove(mag);
    }

    self.canSubmit = ko.computed(function () {
        if (self.magnifications().length > 0 && self.friendlyName().length > 0
            && self.ocular() != null && self.microscopeModel().length > 0) return true;
        return false;
    }, self);

    self.submit = function (parentVM) {
        let request = {
            name: self.friendlyName(),
            type: self.microscopeType(),
            model: self.microscopeModel(),
            ocular: self.ocular(),
            objectives: self.magnifications()
        };
        $.ajax({
                url: "/api/v1/digitise/calibration/use",
                type: "POST",
                data: JSON.stringify(request),
                dataType: "json",
                contentType: "application/json"
            })
            .done(function (data) {
                parentVM.changeView(CalibrateView.MASTER, null);
            })
    }
}

function ImageCalibrationViewModel(currentMicroscope) {
    let self = this;
    self.CurrentView = CurrentView;
    self.microscope = ko.observable(currentMicroscope);
    self.magnification = ko.observable();
    self.floatingCal = ko.observable();
    self.measuredDistance = ko.observable();
    self.viewer = null;
    self.loadedImage = ko.observable(false);
    self.measuringLine = null;

    self.canSubmit = function() {
        if(!self.loadedImage()) return false;
        if(self.measuredDistance() == null) return false;
        if(self.floatingCal() == null) return false;
        if(self.measuredDistance() <= 0) return false;

        return true;
    }

    self.magName = function(data) {
        return data.level + 'x';
    }

    self.submit = function (parent) {
        if (self.viewer == null) return;

        convertToDataURLviaCanvas(self.viewer.imagePaths[0], function (d) {
            let request = {
                calibrationId: self.microscope().id,
                magnification: self.magnification(),
                x1: Math.round(self.floatingCal()[0][0]),
                x2: Math.round(self.floatingCal()[1][0]),
                y1: Math.round(self.floatingCal()[0][1]),
                y2: Math.round(self.floatingCal()[1][1]),
                measuredLength: parseFloat(self.measuredDistance()),
                imageBase64: d
            }
            $.ajax({
                    url: "/api/v1/digitise/calibration/use/mag",
                    type: "POST",
                    data: JSON.stringify(request),
                    dataType: "json",
                    contentType: "application/json"
                })
                .done(function (data) {
                    parent.currentView(CalibrateView.MASTER);
                })
        });
    }

    self.activateMeasuringLine = function () {
        if (self.measuringLine != null) {
            // don't do anything if the measuring line is being drawn
            if (self.measuringLine.state == MeasuringLineState.STATE_DRAWING ||
                self.measuringLine.state == MeasuringLineState.STATE_ACTIVE) {
                return;
            }

            self.measuringLine.dispose();
        }

        self.measuringLine = new MeasuringLine(self.viewer,
            "#calibration-viewer-measuring-line", false, null,
            "Enter the actual distance below");

        self.measuringLine.activate();
        $("#calibration-draw-line-button").prop("disabled", true);

        $(self.measuringLine.id).on(MeasuringLineEvent.EVENT_DRAWN, function () {
            $("#calibration-draw-line-button").prop("disabled", false);
            self.floatingCal(self.measuringLine.getPixelPoints());
        });
    };

    self.createViewer = function (element) {
        if (self.viewer != null) self.viewer.dispose();

        var file = element.files[0];
        var reader = new FileReader();

        reader.onloadend = function (e) {
            self.base64 = reader.result;
            self.viewer = new Viewer("#calibration-viewer-container",
                "#calibration-viewer",
                $("#calibration-viewer-container").width() * 0.8, 500, [
                    self.base64
                ]
            );
            self.loadedImage(true);
        }

        if (file) {
            reader.readAsDataURL(file);
        }
    }
}

//Base Functions
function isEmpty(str) {
    return (!str || 0 === str.length);
}

function convertToDataURLviaCanvas(url, callback) {
    var img = new Image();
    img.crossOrigin = 'Anonymous';
    img.onload = (event) => {
        var canvas = <HTMLCanvasElement>document.createElement('CANVAS');
        var ctx = canvas.getContext('2d');
        var dataURL;
        canvas.height = img.height;
        canvas.width = img.width;
        ctx.drawImage(img, 0, 0);
        dataURL = canvas.toDataURL("image/png");
        callback(dataURL);
        canvas = null;
    };
    img.src = url;
}