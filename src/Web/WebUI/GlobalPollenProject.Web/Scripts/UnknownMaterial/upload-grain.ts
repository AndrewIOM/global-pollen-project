/// <reference types="@types/googlemaps" />
import 'jquery-jcrop';
import 'bootstrap-datepicker';
import {BoundingBox, Viewer} from "../Components/Viewer/viewer";
import { MeasuringLine, MeasuringLineEvent, MeasuringLineState } from "../Components/Viewer/measuringline";

/**
 * Simple object to hold data for a single unknown grain image
 */
class ImageObject {
    b64: string;
    crop?: BoundingBox;
    floatingCal: string;
    measuredDistance: string;
    nativeWidth: number;
    
    ready() {
        return (
            this.b64 != null &&
            this.crop != null &&
            this.floatingCal != null &&
            this.measuredDistance != null
        )
    }
}

export function activate(_: HTMLElement) {
    $(() => {
        
        // has at least one image uploaded?
        let hasUploaded = false;

        // Setup initial form state
        $(() => {
            let images: Array<ImageObject> = [];

            // Attach upload function to submit button
            const uploadButton = $('#submit');
            uploadButton.on("click", () => uploadGrain(hasUploaded));
            
            $("#errors-box").hide();
            $("#add-grain-form").trigger("reset");

            // form is completed in stages
            $("#identify-sampling-method").on("change",() => {
                if ($("#identify-sampling-method-environmental").is(":checked")) {
                    $("#identify-temporal-fossil").hide();
                    $("#identify-temporal-environmental").show();
                } else if ($("#identify-sampling-method-fossil").is(":checked")) {
                    $("#identify-temporal-environmental").hide();
                    $("#identify-temporal-fossil").show();
                }
            });

            $("#identify-temporal-fossil-type").on("change",() => {
                if ($("#identify-temporal-fossil-unknown").is(":checked")) {
                    $("#identify-temporal-fossil-value-section").hide();
                } else {
                    $("#identify-temporal-fossil-value-section").show();
                }
            });

            // on image upload
            $("#identify-image-upload-button").on("change", () => {
                images = [];

                // load in each image file into an image object
                const files = $("#identify-image-upload-button").prop("files");
                let loaded = 0;

                for (let i = 0; i < files.length; i++) {
                    const file = files[i];
                    const reader = new FileReader();
                    reader.onloadend = (_) => {
                        let img = new ImageObject();
                        img.b64 = reader.result as string;
                        images.push(img);
                        loaded++;
                        if (loaded == files.length) {
                            hasUploaded = true;
                            createTabs(images);
                        }
                    }
                    if (file) {
                        reader.readAsDataURL(file);
                    }
                }
            });

            const latLong = new google.maps.LatLng(51.4975941, -0.0803232);
            const map = new google.maps.Map(document.getElementById('map'), {
                center: latLong,
                zoom: 5,
                mapTypeId: google.maps.MapTypeId.TERRAIN
            });

            let marker;

            const placeMarker = (location) => {
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

            const updateLocationFormFields = (latLng) => {
                const lat = latLng.lat().toFixed(4);
                const lon = latLng.lng().toFixed(4);
                (<HTMLInputElement>document.getElementById('latitude-input')).value = lat;
                (<HTMLInputElement>document.getElementById('longitude-input')).value = lon;
            }

            google.maps.event.addListener(map, 'click', function (event) {
                placeMarker(event.latLng);
                updateLocationFormFields(event.latLng);
            });

            $("#identify-temporal-environmental-year").datepicker({
                format: " yyyy",
                //viewMode: "years",
                startDate: '1850',
                endDate: '+0d',
                minViewMode: "years"
            });
        });
    });
}

/**
 * (Re-)creates the tabs shown on the add unknown grain interface
 * One tab for each image supplied
 * @param {*[ImageObject]} images
 */
function createTabs(images: ImageObject[]) {
    $("#identify-image-config-tabs").html("");
    $("#identify-image-config-content").html("");

    images.forEach((img, i) => {
        const tab = $([
            "<li class='nav-item' id='identify-image-config-tab-" + i + "'>",
            "<a class='nav-link' data-toggle='tab' href='#tab" + i + "' role='tab'>",
            "<img src='data:" + img.b64 + "' alt=''>",
            "</a>",
            "</li>"
        ].join("\n"));

        if (i == 0)
            $(tab).find("a").addClass("active");

        $("#identify-image-config-tabs").append(tab);

        const tabContent = $("<div class='tab-pane' id='tab" + i + "' role='tabpanel'></div>");

        if (i == 0)
            $(tabContent).addClass("active");

        $(tabContent).append(
            $([
                "<div class='row'>",
                "<div class='col-md-6 identify-image-cropper'>",
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

        $(tabContent).find(".identify-measured-distance").on("change",(e) => {
            $(e.target).val($(e.target).val().toString().replace(/[^0-9.]/g, ""));
        });

        const cropperWidth = tabContent.find(".identify-image-viewer").width();

        $(tabContent).find("img").Jcrop({
            boxWidth: cropperWidth,
            boxHeight: 400,
            onSelect: (c) => {
                const activeImage = $("#tab" + i).find(".jcrop-holder");//.find(".jcrop-active");
                const w = activeImage.width();
                const h = activeImage.height();
                console.log(w);
                console.log(h);
                console.log(c);
                images[i].crop = {
                    x: c.x /= w,
                    y: c.y /= h,
                    w: c.w /= w,
                    h: c.h /= h
                };
                createImageViewer(images, i);
            }
        });
        $("#identify-image-config-content").append(tabContent);
    });

    // show tabs
    $("#identify-image-configuration").css("display", "block");
}

/**
 * Creates 
 * @param images
 * @param imageNo
 */
function createImageViewer(images:ImageObject[], imageNo:number) {
    const tabContainer = $("#tab" + imageNo);
    const imageContainer = $("#identify-image-viewer-container-" + imageNo);

    // Make tab visible and reset contents
    tabContainer.find(".identify-image-viewer").css("visibility", "visible");
    tabContainer.find(".identify-measured-distance").val("");
    imageContainer.html("");
    
    // Create a new Viewer component
    const viewer = new Viewer("#identify-image-viewer-container-" + imageNo,
        "#identify-image-viewer-" + imageNo,
        imageContainer.width(),
        300,//imageContainer.height(), 
        [images[imageNo].b64],
        images[imageNo].crop
    );

    // Activate line-drawing tool
    tabContainer.find(".identify-draw-line-button").off("click");
    tabContainer.find(".identify-draw-line-button").on("click", (e) => {
        e.preventDefault();
        const viewerContainer = $("#identify-image-viewer-" + imageNo);
        console.log(viewerContainer);
        console.log(imageNo);

        if (viewerContainer.data("measuring-line") != null) {
            if (viewerContainer.data("measuring-line").state == MeasuringLineState.STATE_DRAWING) {
                return;
            }
            viewerContainer.data("measuring-line").dispose();
        }

        // save the measuring line as data attached to the viewer
        viewerContainer.data("measuring-line", new MeasuringLine(viewer,
            "#static-image-previewer-measuring-line-" + imageNo, false, null,
            "Enter the actual distance below"));

        // TODO Somehow emit the event from the measuring line class
        $(self).on(MeasuringLineEvent.EVENT_DRAWN, () => {
            $("#tab" + imageNo).find(".identify-draw-line-button").removeClass("disabled");
        });

        tabContainer.find(".identify-draw-line-button").addClass("disabled");
        viewerContainer.data("measuring-line").activate();
    })
}

/**
 * Checks that all required fields in the form have been filled in
 */
function validationErrors(hasUploaded:boolean) {
    let output = "";
    const e = (t) => {
        output += (t + "<br>")
    }

    // check that the first radio buttons have a value
    if (!$("input[name='identify-method-radio']:checked").val()) {
        e("Sampling method must be defined (step 1)");
    }

    if (!hasUploaded) {
        e("You must upload at least one image of your grain (step 2)");
    }

    let nocrop = false;
    let noline = false;
    let novalue = false;
    $(".tab-pane").each(function () {
        if (!nocrop) {
            if ($(this).find(".identify-image-viewer").find("canvas").length == 0) {
                // this tab has not cropped
                nocrop = true;
                e("Each uploaded image must be cropped to fit just the grain (step 2)");
            }
        }

        if (!noline) {
            if ($(this).find(".identify-image-viewer").find("canvas").data("measuring-line") == undefined) {
                e("Each uploaded image must have a calibration measurement line (step 2)")
                noline = true;
            }
        }

        if (!novalue) {
            if ($(this).find(".identify-image-viewer").find(".identify-measured-distance").val() == "") {
                e("Each uploaded image must have a known distance typed in (step 2)")
                novalue = true;
            }
        }
    });

    if ($("#latitude-input").val() == "" || $("#longitude-input").val() == "") {
        e("You must specify where the grain was collected from (step 3)")
    }

    if ($("#identify-sampling-method-environmental").is(":checked")) {
        if ($("#identify-temporal-environmental-year").val() == "") {
            e("You must specify the year the sample was collected (step 4)");
        }
    } else if ($("#identify-sampling-method-fossil").is(":checked")) {
        if (!$("#identify-temporal-fossil-unknown").is(":checked")) {
            if ($("#identify-temporal-fossil-ybp").val() == "") {
                e("You must specify the temporal context (step 4)");
            }
        }
    }

    if (output == "") {
        // no errors found - return null
        return null;
    } else {
        return output;
    }
}

interface Base64Result {
    PngBase64: string
}

interface Base64Dictionary {
    [index: string]: Base64Result;
}

function getBase64FromViewer(viewer:Viewer, callback) {
    let img = new Image();
    img.crossOrigin = 'Anonymous';
    img.onload = () => {
        console.log("Loaded image")
        let canvas = <HTMLCanvasElement>document.createElement('CANVAS');
        let ctx = canvas.getContext('2d');
        let dataURL: string;
        canvas.height = viewer.imgHeight;
        canvas.width = viewer.imgWidth;
        ctx.drawImage(
            img,
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
    img.src = viewer.getImagePaths()[0];
}

function getAllBase64s(callback) {
    let output : Base64Dictionary = {};
    let loaded = 0;
    const elem = $("[id^=identify-image-viewer-container-]");
    const total = elem.length;
    elem.each((i, e) => {
        const viewer = $(e).find("canvas").data("viewer");
        if (viewer) {
            const id = $(e).find("canvas").attr("id");
            getBase64FromViewer(viewer, (d:string, v:Viewer) => {
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

function uploadGrain(hasUploaded) {
    const error = validationErrors(hasUploaded);
    if (error) {
        $("#errors").html(error);
        $("#errors-box").show();
        $("html, body").animate({scrollTop: 0}, "slow");
        return;
    }

    $('#submit')
        .css('display', 'none')
        .addClass('disabled');
    
    console.log("Getting base64s...")
    getAllBase64s((output:Base64Dictionary) => {
        console.log("Got base64s. Sending request...")
        for (let vid in output) {
            const v = output[vid];
            const measuringLine = $(vid).data("measuring-line");
            const pp = measuringLine.getPixelPoints();

            output[vid]["X1"] = Math.floor(Math.round(pp[0][0]));
            output[vid]["Y1"] = Math.floor(Math.round(pp[0][1]));
            output[vid]["X2"] = Math.floor(Math.round(pp[1][0]));
            output[vid]["Y2"] = Math.floor(Math.round(pp[1][1]));
            output[vid]["MeasuredLength"] = parseFloat($(vid).parent().parent().parent().find(".identify-measured-distance").val().toString());
        }

        const outputArray = Object.keys(output).map(function (v) {
            return output[v];
        });

        let request = {
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
                const xhr = $.ajaxSettings.xhr();
                xhr.upload.onprogress = function (evt) {
                    $('#upload-progress').css('display', 'block');
                    if (evt.lengthComputable) {
                        const percentComplete = evt.loaded / evt.total;
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
                    $('#submit')
                        .css('display', 'block')
                        .removeClass('disabled');
                    console.log(err);
                },
                500: function (data) {
                    $('#upload-progress').css('display', 'none');
                    $('#submit')
                        .css('display', 'block')
                        .removeClass('disabled');
                    console.log("Internal error. Please try again later.");
                }
            }
        });
    });
}