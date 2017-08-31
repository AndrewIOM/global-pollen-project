/**
 * Simple object to hold data for a single unknown grain image
 */
var ImageObject = function () {
    this.b64 = null;
    this.crop = null;
    this.floatingCal = null;
    this.measuredDistance = null;
    this.nativeWidth = null;

    this.ready = function () {
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

/**
 * (Re-)creates the tabs shown on the add unknown grain interface
 * One tab for each image supplied
 * @param {*[ImageObject]} images 
 */
var createTabs = function (images) {
    // remove existing tabs
    $("#identify-image-config-tabs").html("");
    $("#identify-image-config-content").html("");

    for (var i in images) {
        var img = images[i];
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
            $(this).val($(this).val().replace(/[^0-9.]/g, ""));
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
            tab: parseInt(i)
        });
        $("#identify-image-config-content").append(tabContent);
    }

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
            if ($("#identify-image-viewer-" + imageNo).data("measuring-line").state == MeasuringLine.STATE_DRAWING) {
                return;
            }
            $("#identify-image-viewer-" + imageNo).data("measuring-line").dispose();
        }

        // save the measuring line as data attached to the viewer
        $("#identify-image-viewer-" + imageNo).data("measuring-line", new MeasuringLine(viewer,
            "#static-image-previewer-measuring-line-" + imageNo, false, null,
            "Enter the actual distance below"));

        $($("#identify-image-viewer-" + imageNo).data("measuring-line")).on(MeasuringLine.EVENT_DRAWN, function () {
            $("#tab" + imageNo).find(".identify-draw-line-button").removeClass("disabled");
        });

        $("#tab" + imageNo).find(".identify-draw-line-button").addClass("disabled");
        $("#identify-image-viewer-" + imageNo).data("measuring-line").activate();
    });
}

var uploadGrain = function () {
    $('#submit').prop('disabled', true);
    $('#submit').addClass('disabled');

    function getBase64s(callback) {
        function getBase64FromViewer(viewer, callback) {
            var img = new Image();
            img.crossOrigin = 'Anonymous';
            img.onload = function () {
                var canvas = document.createElement('CANVAS');
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

            output[vid]["X1"] = parseInt(Math.round(pp[0][0]));
            output[vid]["Y1"] = parseInt(Math.round(pp[0][1]));
            output[vid]["X2"] = parseInt(Math.round(pp[1][0]));
            output[vid]["Y2"] = parseInt(Math.round(pp[1][1]));
            output[vid]["MeasuredLength"] = parseFloat($(vid).parent().parent().parent().find(".identify-measured-distance").val());
        }

        var outputArray = Object.keys(output).map(function(v) { return output[v]; });

        var request = {
            Images: outputArray,
            SampleType: "Fossil",
            LatitudeDD: 52.0173,
            LongitudeDD: -2.1313,
            Year: 2000,
            YearType: "Lead210"
        }
        console.log(request);


        $.ajax({
            url: "/Identify/Upload",
            type: "POST",
            data: JSON.stringify(request),
            dataType: "json",
            contentType: "application/json",
            xhr: function () {
                var xhr = $.ajaxSettings.xhr();
                xhr.upload.onprogress = function (evt) {
                    if (evt.lengthComputable) {
                        var percentComplete = evt.loaded / evt.total;
                        console.log(percentComplete * 100);
                    }
                }
                return xhr;
            },
            success: function (data) {
                console.log(data);
            },
            statusCode: {
                400: function (err) {
                    console.log(err);
                },
                500: function (data) {
                    console.log("Internal error. Please try again later.");
                }
            }
        });
    });
}

$(function () {
    // array of image objects
    var images = [];

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
            google.maps.event.addListener(marker, 'dragend', function (event) {
                updateLocationFormFields(event.latLng);
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
        document.getElementById('LatitudeDD').value = lat;
        document.getElementById('LongitudeDD').value = lon;
    }

});