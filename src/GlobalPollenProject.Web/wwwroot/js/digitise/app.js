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

$(document).ready(function () {
    var vm = new DigitiseViewModel();
    vm.switchView(CurrentView.MASTER);
    ko.applyBindings(vm);
});

var CurrentView = {
    MASTER: 1,
    DETAIL: 2,
    ADD_COLLECTION: 3,
    ADD_SLIDE_RECORD: 4,
    SLIDE_DETAIL: 5,
    CALIBRATE: 6
};

var SlideDetailTab = {
    OVERVIEW: 1,
    UPLOAD_STATIC: 2,
    UPLOAD_FOCUSABLE: 3
};

function DigitiseViewModel(users, analyses) {
    var self = this;
    let apiPrefix = "/api/v1/digitise/";
    self.currentView = ko.observable(CurrentView.BASE);
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
    }

    self.switchView = function (view, data) {
        switch (view) {
            case CurrentView.MASTER:
                self.refreshCollectionList();
                self.activeCollection(null);
                self.currentView(view);
                break;
            case CurrentView.DETAIL:
                $.ajax({
                        url: apiPrefix + "collection?id=" + data.Id,
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
    }

    self.submitSlideRequest = function () {
        let request = self.newSlideVM().getRequest();
        $.ajax({
                url: "/api/v1/digitise/collection/slide/add",
                type: "POST",
                data: JSON.stringify(request),
                dataType: "json",
                contentType: "application/json"
            })
            .done(function (data) {
                self.switchView(CurrentView.DETAIL, self.activeCollection());
            })
    }

    self.setActiveCollectionTab = function (element) {
        $(element).parent().find("li").removeClass("active");
        $(element).addClass("active");
    }

    self.publish = function () {
        let requestUrl = "/api/v1/digitise/collection/publish?id=" + self.activeCollection().Id;
        $.ajax({
                url: requestUrl,
                type: "GET"
            })
            .done(function (data) {
                alert("Published??");
            })
    }
}

////////////////////////
/// Create R. Collection
////////////////////////

function AddCollectionViewModel() {
    let self = this;
    self.name = ko.observable();
    self.description = ko.observable();
    self.curatorFirstNames = ko.observable();
    self.curatorSurname = ko.observable();
    self.institutionName = ko.observable();
    self.email = ko.observable();
    self.externalUrl = ko.observable();
    
    self.submit = function (rootVM) {
        let req = {
            Name: self.name(),
            Description: self.description(),
            CuratorFirstNames: self.curatorFirstNames(),
            CuratorSurname: self.curatorSurname(),
            Institution: self.institutionName(),
            CuratorEmail: self.email(),
            ExternalUrl: self.externalUrl()
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
            error: function (errors) {
                // Handle validation errors here
            }
        })
    }
}

////////////////////////
/// Record Slide Dialog
////////////////////////

function RecordSlideViewModel(currentCollection) {
    let self = this;
    self.collection = ko.observable(currentCollection);
    self.rank = ko.observable("");
    self.family = ko.observable("");
    self.genus = ko.observable("");
    self.species = ko.observable("");
    self.author = ko.observable("");
    self.newSlideTaxonStatus = ko.observable(null);
    self.currentTaxon = ko.observable();
    self.collectionMethod = ko.observable();
    self.existingId = ko.observable();
    self.yearCollected = ko.observable();
    self.collectorFirstNames = ko.observable();
    self.collectorLastName = ko.observable();
    self.locationType = ko.observable();
    self.locality = ko.observable();
    self.district = ko.observable();
    self.region = ko.observable();
    self.country = ko.observable();
    self.continent = ko.observable();
    self.yearPrepared = ko.observable();
    self.preperationMethod = ko.observable();
    self.mountingMaterial = ko.observable();

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

    self.isValidTaxonSearch = ko.computed(function () {
        if (self.rank() == "Family" && self.family().length > 0) return true;
        if (self.rank() == "Genus" && self.family().length > 0 && self.genus().length > 0) return true;
        if (self.rank() == "Species" && self.genus().length > 0 && self.species().length > 0) return true;
        return false;
    }, self);

    self.isValidAddSlideRequest = ko.computed(function () {
        if (self.rank() == "") return false;
        if (self.currentTaxon() == "") return false;
        if (self.collectionMethod() == "") return false;
        return true;
    }, self)

    self.validateTaxon = function () {
        var query;
        if (self.rank() == "Family") {
            query = "rank=Family&family=" + self.family() + "&latinname=" + self.family();
        } else if (self.rank() == 'Genus') {
            query = "rank=Genus&family=" + self.family() + "&genus=" + self.genus() + "&latinname=" + self.genus();
        } else if (self.rank() == "Species") {
            query = "rank=Species&family=" + self.family() + "&genus=" + self.genus() + "&species=" + self.species() + "&latinname=" + self.genus() + " " + self.species() + "&authorship=" + self.author();
        }
        $.ajax({
                url: "/api/v1/backbone/trace?" + query,
                type: "GET"
            })
            .done(function (data) {
                if (data.length == 1 && data[0].TaxonomicStatus == "accepted") self.currentTaxon(data[0].Id);
                self.newSlideTaxonStatus(data);
            })
    }

    self.getRequest = function () {
        let firstNames = self.collectorFirstNames() == "" ? self.collectorFirstNames().split(' ') : [];
        let request = {
            Collection: self.collection().Id,
            ExistingId: self.existingId(),
            OriginalFamily: self.family(),
            OriginalGenus: self.genus(),
            OriginalSpecies: self.species(),
            OriginalAuthor: self.author(),
            ValidatedTaxonId: self.currentTaxon(),
            SamplingMethod: self.collectionMethod(),
            YearCollected: parseInt(self.yearCollected()),
            YearSlideMade: parseInt(self.yearPrepared()),
            LocationType: self.locationType(),
            LocationRegion: self.region(),
            LocationCountry: self.country(),
            LocationContinent: self.continent(),
            PreperationMethod: self.preperationMethod(),
            MountingMaterial: self.mountingMaterial(),
            CollectedByFirstNames: firstNames,
            CollectedBySurname: self.collectorLastName()
        };
        return request;
    }

    self.capitaliseFirstLetter = function (element) {
        $(element).val($(element).val().charAt(0).toUpperCase() + $(element).val().slice(1));
    }
}

////////////////////////
/// Add Image View
////////////////////////

function SlideDetailViewModel(detail) {
    let self = this;
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

    self.calibrations = ko.observableArray();

    self.selectedMicroscope = ko.observable(null);
    self.selectedMagnification = ko.observable(null);

    // Add slide request
    self.framesBase64 = ko.observableArray([]);
    self.floatingCal = ko.observable();
    self.measuredDistance = ko.observable();
    self.digitisedYear = ko.observable(null);

    self.isValidStaticRequest = ko.computed(function () {
        if (self.floatingCal() == null) return false;
        if (self.measuredDistance() == null) return false;
        if (self.digitisedYear() == null) return false;
        if (self.measuredDistance() <= 0) return false;
        return true;
    }, self);

    self.isValidFocusRequest = ko.computed(function () {
        if (self.selectedMicroscope() == null) return false;
        if (self.selectedMagnification() == null) return false;
        if (self.digitisedYear() == null) return false;
        return true;
    }, self);

    self.loadCalibrations = function () {
        $.ajax({
                url: "/api/v1/digitise/calibration/list",
                type: "GET"
            })
            .done(function (cals) {
                
                console.log(cals);
                self.calibrations(cals);
            })
    }

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
                viewMode: "years",
                startDate: '1850',
                endDate: '+0d',
                minViewMode: "years"
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
                viewMode: "years",
                startDate: '1850',
                endDate: '+0d',
                minViewMode: "years"
            });

            self.selectedMicroscope(null);
            self.selectedMagnification(null);

            if (self.scaleBar != null) {
                self.scaleBar.dispose();
            }
        }
    }

    self.submit = function (rootVM, base64Array) {
        let request = {
            CollectionId: self.slideDetail().CollectionId,
            SlideId: self.slideDetail().CollectionSlideId,
            IsFocusImage: base64Array.length > 1,
            FramesBase64: base64Array,
            DigitisedYear: parseInt(self.digitisedYear())
        }
        if(base64Array.length == 1) {
            request["FloatingCalPointOneX"] = Math.round(self.floatingCal()[0][0]);
            request["FloatingCalPointOneY"] = Math.round(self.floatingCal()[0][1]);
            request["FloatingCalPointTwoX"] = Math.round(self.floatingCal()[1][0]);
            request["FloatingCalPointTwoY"] = Math.round(self.floatingCal()[1][1]);
            request["MeasuredDistance"] = parseFloat(self.measuredDistance());
        } else if (base64Array.length > 1) {
            request["CalibrationId"] = self.selectedMicroscope().Id;
            request["Magnification"] = self.selectedMagnification().Level;
        } else {
            return;
        }
        console.log(request);

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
                        console.log(percentComplete * 100);
                    }
                }
                return xhr;
            },
            success: function (data) {
                console.log(self.slideDetail());
                rootVM.switchView(CurrentView.DETAIL, {
                    Id: self.slideDetail().CollectionId
                });
            },
            statusCode: {
                400: function (err) {
                    console.log(err);
                    self.validationErrors([]);
                    err.responseJSON.Errors.forEach(function (e) {
                        self.validationErrors().push(e.Errors[0]);
                        console.log(self.validationErrors());
                    })
                },
                500: function (data) {
                    self.validationErrors(['Internal error. Please try again later.']);
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
            viewMode: "years",
            startDate: '1850',
            endDate: '+0d',
            minViewMode: "years"
        });

        $("#measuredDistance").change(function () {
            self.measuredDistance($(this).val().replace(/[^0-9.]/g, ""));
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

        return self.selectedMagnification().Level + "x";
    }

    self.selectedMicroscopeName = function () {
        if (self.selectedMicroscope() == null)
            return "Select microscope"

        return self.selectedMicroscope().Name;
    }

    self.selectedMicroscopeMagnifications = function () {
        if (self.selectedMicroscope() == null)
            return [];

        return self.selectedMicroscope().Magnifications;
    }

    self.selectMicroscope = function (element) {
        var selected = self.calibrations().find(function (e) {
            return e.Id == element.Id;
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
            viewMode: "years",
            startDate: '1850',
            endDate: '+0d',
            minViewMode: "years"
        });

        if (self.viewer != null) {
            self.viewer.dispose();
            self.loadedFocusImages(false);
        }

        var files = element.files;
        var loaded = 0;
        var readers = [];
        var base64s = [];

        if (files.length < 2) {
            alert("You must upload at least 2 images (shift-click/ctrl-click files to select multiple)");
            return;
        }

        for (var i = 0; i < files.length; i++) {
            readers.push(new FileReader());

            readers[i].onloadend = function (e) {
                loaded++;
                base64s.push(this.result);

                if (loaded >= files.length) {
                    self.viewer = new Viewer("#focus-image-previewer",
                        "#focus-image-previewer-canvas",
                        $("#focus-image-previewer-container").width() * 0.8, 500, base64s
                    );
                    self.slider = new FocusSlider(self.viewer, "#focus-image-previewer-slider");
                    self.loadedFocusImages(true);

                    $(self.viewer).on(Viewer.EVENT_IMAGES_MISMATCHED_SIZE, function () {
                        self.loadedFocusImages(false);
                        $("#focus-image-previewer-container").empty();
                        self.viewer.dispose();
                        self.slider.dispose();
                        alert("Error - images must all have the same dimensions (width x height)");
                    });
                }
            }
            if (files[i]) {
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

        self.scaleBar = new ScaleBar(self.viewer, "#focus-image-previewer-scalebar", self.selectedMagnification().PixelWidth);
        self.scaleBar.initialise();
    }

    self.activateMeasuringLine = function () {
        if (self.measuringLine != null) {
            // don't do anything if the measuring line is being drawn
            if (self.measuringLine.state == MeasuringLine.STATE_DRAWING ||
                self.measuringLine.state == MeasuringLine.STATE_ACTIVE) {
                return;
            }

            self.measuringLine.dispose();
        }

        self.measuringLine = new MeasuringLine(self.viewer,
            "#static-image-previewer-measuring-line", false, null,
            "Enter the actual distance below");

        self.measuringLine.activate();
        $("#slidedetail-draw-line-button").prop("disabled", true);

        $(self.measuringLine).on(MeasuringLine.EVENT_DRAWN, function () {
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
    self.friendlyName = ko.observable();
    self.microscopeType = ko.observable();
    self.ocular = ko.observable();
    self.microscopeModel = ko.observable("FAKE MICROSCOPE");
    self.magnifications = ko.observableArray([10, 20, 40, 100]);

    self.addMag = function () {
        self.magnifications.push();
    }

    self.removeMag = function (mag) {
        self.magnifications.remove(mag);
    }

    self.submit = function (parentVM) {
        let request = {
            Name: self.friendlyName(),
            Type: self.microscopeType(),
            Model: self.microscopeModel(),
            Ocular: self.ocular(),
            Objectives: self.magnifications()
        };
        $.ajax({
                url: "/api/v1/digitise/calibration/use",
                type: "POST",
                data: JSON.stringify(request),
                dataType: "json",
                contentType: "application/json"
            })
            .done(function (data) {
                parentVM.changeView(CalibrateView.MASTER);
            })
    }
}

function ImageCalibrationViewModel(currentMicroscope) {
    let self = this;
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
        return data.Level + 'x';
    }

    self.submit = function (parent) {
        if (self.viewer == null) return;

        convertToDataURLviaCanvas(self.viewer.imagePaths[0], function (d) {
            let request = {
                CalibrationId: self.microscope().Id,
                Magnification: self.magnification(),
                X1: Math.round(self.floatingCal()[0][0]),
                X2: Math.round(self.floatingCal()[1][0]),
                Y1: Math.round(self.floatingCal()[0][1]),
                Y2: Math.round(self.floatingCal()[1][1]),
                MeasuredLength: parseFloat(self.measuredDistance()),
                ImageBase64: d
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
            if (self.measuringLine.state == MeasuringLine.STATE_DRAWING ||
                self.measuringLine.state == MeasuringLine.STATE_ACTIVE) {
                return;
            }

            self.measuringLine.dispose();
        }

        self.measuringLine = new MeasuringLine(self.viewer,
            "#calibration-viewer-measuring-line", false, null,
            "Enter the actual distance below");

        self.measuringLine.activate();
        $("#calibration-draw-line-button").prop("disabled", true);

        $(self.measuringLine).on(MeasuringLine.EVENT_DRAWN, function () {
            $("#calibration-draw-line-button").prop("disabled", false);

            self.floatingCal(self.measuringLine.getPixelPoints());
        });
    }

    self.createViewer = function (element) {
        console.log(self.microscope());
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

// Helpers - Dropdown autocomplete
var typingTimer;
var doneTypingInterval = 100;

function capitaliseFirstLetter(string) {
    return string.charAt(0).toUpperCase() + string.slice(1);
}

function suggest(entryBox, rank) {
    clearTimeout(typingTimer);
    if (entryBox.value) {
        typingTimer = setTimeout(function () {
            updateList(entryBox, rank);
        }, doneTypingInterval);
    }
};

//Update suggestion list when timeout complete
function updateList(entryBox, rank) {
    var query = '';
    var value = entryBox.value;
    if (rank == "Family" || rank == "Genus") {
        value = capitaliseFirstLetter(value);
    }

    if (rank == 'Species') {
        //Combine genus and species for canonical name
        var genus = document.getElementById('original-Genus').value;
        query += genus + " ";
    }
    query += value;

    if (value == "") {

    } else {
        var request = "/api/v1/backbone/search?rank=" + rank + "&latinName=" + query;
        $.ajax({
            url: request,
            type: "GET"
        }).done(function (data) {
            var list = document.getElementById(rank + 'List');
            $('#' + rank + 'List').css('display', 'block');
            list.innerHTML = "";
            for (var i = 0; i < data.length; i++) {
                if (i > 10) continue;
                var option = document.createElement('li');
                var link = document.createElement('a');
                option.appendChild(link);
                link.innerHTML = data[i];

                var matchCount = 0;
                for (var j = 0; j < data.length; j++) {
                    if (data[j].latinName == data[i]) {
                        matchCount++;
                    }
                };
                link.addEventListener('click', function (e) {
                    var name = this.innerHTML;
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


function disable(rank) {
    var element;
    if (rank == 'Family') element = 'FamilyList';
    if (rank == 'Genus') element = 'GenusList';
    if (rank == 'Species') element = 'SpeciesList';

    setTimeout(func, 100);

    function func() {
        $('#' + element).fadeOut();
    }
}

//Base Functions
function convertToDataURLviaCanvas(url, callback) {
    var img = new Image();
    img.crossOrigin = 'Anonymous';
    img.onload = function () {
        var canvas = document.createElement('CANVAS');
        var ctx = canvas.getContext('2d');
        var dataURL;
        canvas.height = this.height;
        canvas.width = this.width;
        ctx.drawImage(this, 0, 0);
        dataURL = canvas.toDataURL("image/png");
        callback(dataURL);
        canvas = null;
    };
    img.src = url;
}