// store the components in global vars
var viewer = null;
var slider = null;
var scaleBar = null;

/**
 * Creates/updates the image viewer
 * @param {*} frames        an array of image URLs, in focus order
 * @param {*} pixelWidth    micrometers in a single pixel
 */
var createViewer = function(frames, pixelWidth) {
    // destroy old components, and empty the container
    if(viewer != null) viewer.dispose();
    if(slider != null) slider.dispose();
    if(scaleBar != null) scaleBar.dispose();
    $("#viewer-container").html("");

    // create the viewer
    viewer = new Viewer("#viewer-container", "#viewer-canvas",
        $("#viewer-container").width(), 500, frames
    );

    // add a focus slider if this image is focusable
    if(frames.length > 1) { 
        slider = new FocusSlider(viewer, "#viewer-focus-slider");
    }

    // add a scale bar in the bottom left
    scaleBar = new ScaleBar(viewer, "#viewer-scalebar", pixelWidth);
};

var clickedGalleryItem = function(element) {
    if($(element.target).hasClass("active")) return;

    $("#slide-gallery").find(".slide-gallery-item").removeClass("active");
    $(element.target).addClass("active");
    
    var frames = $(element.target).data("frames").slice(1, -1).split(";");

    createViewer(frames, parseFloat($(element.target).data("pixelwidth")));
};

var jcrop_api;
var initialiseCropper = function() {
    $("#viewer-interface").css("display", "none");
    $("#cropping-panel").css("display", "none");
    $("#cropping-interface").css("display", "block");
    // $("#toolbar-clear").attr("disabled", "disabled");
    // $("#toolbar-done").attr("disabled", "disabled");
    
    var img = viewer.imagePaths[Math.floor(viewer.imagePaths.length / 2)];

    $("#cropping-container").html("<img id='cropper-image' src='" + img + "'></img>");
    var box_width = $('#cropping-container').width();

    $("#cropper-image").Jcrop({multi: true, boxWidth: box_width}, function() {
        jcrop_api = this;
    });

    $("#toolbar-clear").click(function() {
        if(jcrop_api.ui.multi.length > 1) {
            jcrop_api.removeSelection(jcrop_api.ui.multi[0]);
            jcrop_api.setSelection(jcrop_api.ui.multi[0]);
        }
    });

    $('#toolbar-done').click(function() {
        if(jcrop_api.ui.multi.length > 0) {
            console.log(jcrop_api.ui.multi);
        }
    });

    $("#toolbar-cancel").click(function() {
        $("#viewer-interface").css("display", "block");
        $("#cropping-panel").css("display", "block");
        $("#cropping-interface").css("display", "none");
    });
};

$(function() {
    $(".slide-gallery-item").first().addClass("active");
    $(".slide-gallery-item").click(clickedGalleryItem);

    $("#cropping-button").click(initialiseCropper);
});