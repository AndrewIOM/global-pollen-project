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
}

var clickedGalleryItem = function(element) {
    if($(element.target).hasClass("active")) return;

    $("#slide-gallery").find(".slide-gallery-item").removeClass("active");
    $(element.target).addClass("active");
    
    var frames = $(element.target).data("frames").slice(1, -1).split(";");

    createViewer(frames, parseFloat($(element.target).data("pixelwidth")));
}

$(function() {
    $(".slide-gallery-item").first().addClass("active");
    $(".slide-gallery-item").click(clickedGalleryItem);
});