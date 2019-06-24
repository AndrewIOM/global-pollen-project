import 'googlemaps'

/**
 * Simple object to hold data for a single unknown grain image
 */
class ImageObject {
    b64: string;
    crop: JQuery.Jcrop.SelectionInfo;
    floatingCal: string;
    measuredDistance: string;
    nativeWidth: number

    ready() {
        return (
            this.b64 != null &&
            this.crop != null &&
            this.floatingCal != null &&
            this.measuredDistance != null
        )
    }
}

// should the interface be "locked" (e.g. should be locked when drawing measuring line)
var locked = false;

// has at least one image uploaded?
var hasUploaded = false;

/**
 * (Re-)creates the tabs shown on the add unknown grain interface
 * One tab for each image supplied
 * @param {*[ImageObject]} images 
 */
var createTabs = function (images:ImageObject[]) {
    // remove existing tabs
    $("#identify-image-config-tabs").html("");
    $("#identify-image-config-content").html("");

    images.forEach((img,i) => {
        var tab = $([
            "<li class='nav-item' id='identify-image-config-tab-" + i + "'>",
            "<a class='nav-link' data-toggle='tab' href='#tab" + i + "' role='tab'>",
            "<img src='data:" + img.b64 + "' alt=''>",
            "</a>",
            "</li>"
        ].join("\n"));

        if (i == 0)
            $(tab).find("a").addClass("active");

        $("#identify-image-config-tabs").append(tab);

        var tabContent = $("<div class='tab-pane' id='tab" + i + "' role='tabpanel'></div>");

        if (i == 0)
            $(tabContent).addClass("active");

        $(tabContent).append(
            $([
                "<div class='row'>",
                "<div class='col-md-6' class='identify-image-cropper'>",
                "<div class='card'>",
                "<div class='card-header'>",
                "Draw a square box around the pollen grain to crop the image <strong>(click + drag)</strong>",
                "</div>",
                "<div class='card-block'>",
                "<img src='data:" + img.b64 + "' alt='' class='identify-image-cropper-view'>",
                "</div>",
                "</div>",
                "</div>",
                "<div class='col-md-6 identify-image-viewer' style='visibility: hidden'>",
                "<div class='card'>",
                "<div class='card-header'>",
                "Draw a line below of known length for calibration (e.g. grain diameter)",
                "</div>",
                "<div class='card-block'>",
                "<div id='identify-image-viewer-container-" + i + "'></div>",
                "</div>",
                "<div class='card-footer'>",
                "<div class='form-group row'>",
                "<div class='col-sm-3'>",
                "<button type='button' class='btn btn-primary identify-draw-line-button'>Draw Line</button>",
                "</div>",
                "<div class='col-sm-9'>",
                "<div class='input-group'>",
                "<input class='identify-measured-distance form-control' />",
                "<span class='input-group-addon'>μm</span>",
                "</div>",
                "<small class='help'>Enter the length of your measurement line in micrometres (μm)</small>",
                "</div>",
                "</div>",
                "</div>",
                "</div>",
                "</div>",
                "</div>"
            ].join("\n"))
        );

        $(tabContent).find(".identify-measured-distance").change(function () {
            $(this).val($(this).val().toString().replace(/[^0-9.]/g, ""));
        });

        $(tabContent).find("img").Jcrop({
            onSelect: function (c) {
                var w = $("#tab" + this.opt.tab).find(".jcrop-active").width();
                var h = $("#tab" + this.opt.tab).find(".jcrop-active").height();
                images[this.opt.tab].crop = c;
                images[this.opt.tab].crop.x /= w;
                images[this.opt.tab].crop.y /= h;
                images[this.opt.tab].crop.x2 /= w;
                images[this.opt.tab].crop.y2 /= h;
                images[this.opt.tab].crop.w /= w;
                images[this.opt.tab].crop.h /= h;
                createImageViewer(images, this.opt.tab);
            },
            //tab: i
        });
        $("#identify-image-config-content").append(tabContent);
    });

    // show tabs
    $("#identify-image-configuration").css("display", "block");
}

var createImageViewer = function (images, imageNo) {
    $("#tab" + imageNo).find(".identify-image-viewer").css("visibility", "visible");

    // reset everything in the panel
    $("#tab" + imageNo).find(".identify-measured-distance").val("");
    $("#identify-image-viewer-container-" + imageNo).html("");

    // create the viewer
    var viewer = new Viewer("#identify-image-viewer-container-" + imageNo,
        "#identify-image-viewer-" + imageNo,
        $("#identify-image-viewer-container-" + imageNo).width(),
        $("#identify-image-viewer-container-" + imageNo).width(), [images[imageNo].b64],
        images[imageNo].crop
    );

    $("#tab" + imageNo).find(".identify-draw-line-button").off("click");
    $("#tab" + imageNo).find(".identify-draw-line-button").click(function (e) {
        e.preventDefault();

        if ($("#identify-image-viewer-" + imageNo).data("measuring-line") != null) {
            if ($("#identify-image-viewer-" + imageNo).data("measuring-line").state == MeasuringLineState.STATE_DRAWING) {
                return;
            }
            $("#identify-image-viewer-" + imageNo).data("measuring-line").dispose();
        }

        // save the measuring line as data attached to the viewer
        $("#identify-image-viewer-" + imageNo).data("measuring-line", new MeasuringLine(viewer,
            "#static-image-previewer-measuring-line-" + imageNo, false, null,
            "Enter the actual distance below"));

        $($("#identify-image-viewer-" + imageNo).data("measuring-line")).on(MeasuringLineEvent.EVENT_DRAWN, function () {
            $("#tab" + imageNo).find(".identify-draw-line-button").removeClass("disabled");
        });

        $("#tab" + imageNo).find(".identify-draw-line-button").addClass("disabled");
        $("#identify-image-viewer-" + imageNo).data("measuring-line").activate();
    });
}

/**
 * Checks that all required fields in the form have been filled in
 */
var canUpload = function () {
    var output = "";
    var e = function(t) { output += (t + "<br>") };

    // check that the first radio buttons have a value
    if (!$("input[name='identify-method-radio']:checked").val()) {
        e("Sampling method must be defined (step 1)");
    }

    if(!hasUploaded) {
        e("You must upload at least one image of your grain (step 2)");
    }

    var nocrop = false;
    var noline = false;
    var novalue = false;
    $(".tab-pane").each(function() {
        if(!nocrop) {
            if($(this).find(".identify-image-viewer").find("canvas").length == 0) {
                // this tab has not cropped
                nocrop = true;
                e("Each uploaded image must be cropped to fit just the grain (step 2)");
            }
        }

        if(!noline) {
            if($(this).find(".identify-image-viewer").find("canvas").data("measuring-line") == undefined) {
                e("Each uploaded image must have a calibration measurement line (step 2)")
                noline = true;
            }
        }

        if(!novalue) {
            if($(this).find(".identify-image-viewer").find(".identify-measured-distance").val() == "") {
                e("Each uploaded image must have a known distance typed in (step 2)")
                novalue = true;
            }
        }
    });

    if($("#latitude-input").val() == "" || $("#longitude-input").val() == "") {
        e("You must specify where the grain was collected from (step 3)")
    }

    if($("#identify-sampling-method-environmental").is(":checked")) {
        if($("#identify-temporal-environmental-year").val() == "") {
            e("You must specify the year the sample was collected (step 4)");
        }
    } else if($("#identify-sampling-method-fossil").is(":checked")) {
        if(!$("#identify-temporal-fossil-unknown").is(":checked")) {
            if($("#identify-temporal-fossil-ybp").val() == "") {
                e("You must specify the temporal context (step 4)");
            }
        }
    }

    if(output == "") {
        // no errors found - return null
        return null;
    } else {
        return output;
    }
}

var uploadGrain = function () {
    var error = canUpload();
    if(error) {
        $("#errors").html(error);
        $("#errors-box").show();
        $("html, body").animate({ scrollTop: 0 }, "slow");
        return;
    }

    $('#submit').css('display', 'none');
    $('#submit').addClass('disabled');

    function getBase64s(callback) {
        function getBase64FromViewer(viewer, callback) {
            var img = new Image();
            img.crossOrigin = 'Anonymous';
            img.onload = () => {
                var canvas = <HTMLCanvasElement> document.createElement('CANVAS');
                var ctx = canvas.getContext('2d');
                var dataURL;
                canvas.height = viewer.imgHeight;
                canvas.width = viewer.imgWidth;
                ctx.drawImage(
                    this,
                    viewer.crop.x * viewer.nativeWidth,
                    viewer.crop.y * viewer.nativeHeight,
                    viewer.imgWidth,
                    viewer.imgHeight,
                    0, 0,
                    viewer.imgWidth,
                    viewer.imgHeight
                );
                dataURL = canvas.toDataURL("image/png");
                callback(dataURL, viewer);
                canvas = null;
            };
            img.src = viewer.imagePaths[0];
        }

        var output = {};

        var loaded = 0;
        var total = $("[id^=identify-image-viewer-container-]").length;
        $("[id^=identify-image-viewer-container-]").each(function () {
            var viewer = $(this).find("canvas").data("viewer");
            if (viewer) {
                var id = $(this).find("canvas").attr("id");

                getBase64FromViewer(viewer, function (d, v) {
                    output[v.id] = {
                        PngBase64: d
                    };
                    loaded++;
                    if (loaded == total) {
                        callback(output);
                    }
                });
            }
        });
    }

    getBase64s(function (output) {
        for (var vid in output) {
            var v = output[vid];
            var measuringLine = $(vid).data("measuring-line");
            var pp = measuringLine.getPixelPoints();

            output[vid]["X1"] = Math.floor(Math.round(pp[0][0]));
            output[vid]["Y1"] = Math.floor(Math.round(pp[0][1]));
            output[vid]["X2"] = Math.floor(Math.round(pp[1][0]));
            output[vid]["Y2"] = Math.floor(Math.round(pp[1][1]));
            output[vid]["MeasuredLength"] = parseFloat($(vid).parent().parent().parent().find(".identify-measured-distance").val().toString());
        }

        var outputArray = Object.keys(output).map(function (v) {
            return output[v];
        });

        var request = {
            Images: outputArray,
            SampleType: null,
            LatitudeDD: parseFloat($("#latitude-input").val().toString()),
            LongitudeDD: parseFloat($("#longitude-input").val().toString()),
            Year: null,
            YearType: null
        }

        if ($("#identify-sampling-method-fossil").is(":checked")) {
            request.SampleType = "Fossil";
            request.Year = parseInt($("#identify-temporal-fossil-ybp").val().toString());
            if ($("#identify-temporal-fossil-radiocarbon").is(":checked")) {
                request.YearType = "Radiocarbon";
            } else if ($("#identify-temporal-fossil-lead").is(":checked")) {
                request.YearType = "Lead210";
            } else {
                request.YearType = "Unknown";
            }
        } else {
            request.SampleType = "Environmental";
            request.YearType = "Calendar";
            request.Year = parseInt($("#identify-temporal-environmental-year").val().toString());
        }

        $.ajax({
            url: "/Identify/Upload",
            type: "POST",
            data: JSON.stringify(request),
            dataType: "json",
            contentType: "application/json",
            xhr: function () {
                var xhr = $.ajaxSettings.xhr();
                xhr.upload.onprogress = function (evt) {
                    $('#upload-progress').css('display', 'block');
                    if (evt.lengthComputable) {
                        var percentComplete = evt.loaded / evt.total;
                        console.log(percentComplete * 100);
                    }
                }
                return xhr;
            },
            success: function (data) {
                $('#upload-progress').addClass('bg-success');
                location.href = "/Identify";
            },
            statusCode: {
                400: function (err) {
                    $('#upload-progress').css('display', 'none');
                    $('#submit').css('display', 'block');
                    $('#submit').removeClass('disabled');                    
                    console.log(err);
                },
                500: function (data) {
                    $('#upload-progress').css('display', 'none');
                    $('#submit').css('display', 'block');          
                    $('#submit').removeClass('disabled');                                        
                    console.log("Internal error. Please try again later.");
                }
            }
        });
    });
}

$(function () {
    // array of image objects
    var images : Array<ImageObject> = [];

    $("#errors-box").hide();
    $("#add-grain-form").trigger("reset");

    // form is completed in stages
    $("#identify-sampling-method").change(function () {
        if ($("#identify-sampling-method-environmental").is(":checked")) {
            $("#identify-temporal-fossil").hide();
            $("#identify-temporal-environmental").show();
        } else if ($("#identify-sampling-method-fossil").is(":checked")) {
            $("#identify-temporal-environmental").hide();
            $("#identify-temporal-fossil").show();
        }
    });

    $("#identify-temporal-fossil-type").change(function () {
        if ($("#identify-temporal-fossil-unknown").is(":checked")) {
            $("#identify-temporal-fossil-value-section").hide();
        } else {
            $("#identify-temporal-fossil-value-section").show();
        }
    });

    // on image upload
    $("#identify-image-upload-button").change(function () {
        images = [];

        // load in each image file into an image object
        var files = $(this).prop("files");
        var loaded = 0;

        for (var i = 0; i < files.length; i++) {
            var file = files[i];
            var reader = new FileReader();

            // callback when the image loads
            reader.onloadend = function (e) {
                var img = new ImageObject();
                img.b64 = this.result;
                images.push(img);
                loaded++;

                if (loaded == files.length) {
                    // all images loaded, so create the tabs
                    hasUploaded = true;
                    createTabs(images);
                }
            }

            if (file) {
                reader.readAsDataURL(file);
            }
        }
    });

    var latlng = new google.maps.LatLng(51.4975941, -0.0803232);
    var map = new google.maps.Map(document.getElementById('map'), {
        center: latlng,
        zoom: 5,
        mapTypeId: google.maps.MapTypeId.TERRAIN
    });

    var marker;

    function placeMarker(location) {
        if (marker) {
            marker.setPosition(location);
        } else {
            marker = new google.maps.Marker({
                position: location,
                map: map,
                title: "Pollen Sample Location",
                draggable: true
            });
        }
    }

    google.maps.event.addListener(map, 'click', function (event) {
        placeMarker(event.latLng);
        updateLocationFormFields(event.latLng);
    });

    $("#identify-temporal-environmental-year").datepicker({
        format: " yyyy",
        viewMode: "years",
        startDate: '1850',
        endDate: '+0d',
        minViewMode: "years"
    });

    function updateLocationFormFields(latLng) {
        var lat = latLng.lat().toFixed(4);
        var lon = latLng.lng().toFixed(4);
        (<HTMLInputElement>document.getElementById('latitude-input')).value = lat;
        (<HTMLInputElement>document.getElementById('longitude-input')).value = lon;
    }

});