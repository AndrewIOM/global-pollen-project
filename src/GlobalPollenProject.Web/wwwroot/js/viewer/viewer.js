var width = 0;
var height = 500;

var base, canvas, transform, context, imgWidth, imgHeight;

var imagePaths = ["/images/tmp1.png", "/images/tmp2.png", "/images/tmp3.png", "/images/tmp4.png", "/images/tmp5.png"];
var images = [];

$(function () {
    width = $('#viewer-container').width();
    var loadedCounter = 0;

    // loop through all image paths (focus levels), and load the images
    for (var i = 0; i < imagePaths.length; i++) {
        var img = new Image();
        img.onload = function () {
            // ensure all focus level images have the same dimensions
            if (imgWidth != undefined && imgHeight != undefined) {
                if (this.width != imgWidth || this.height != imgHeight) {
                    console.error("Focus images are not of equal size! Size of image #" + i + ": " +
                        this.width + "x" + this.height + " - expected size: " + imgWidth + "x" + imgHeight);
                }
            } else {
                imgWidth = this.width;
                imgHeight = this.height;
            }

            loadedCounter++;
            if (loadedCounter == imagePaths.length - 1) {
                // proceed if all images have been loaded
                load();
            }
        }
        img.src = imagePaths[i];
        images.push(img);
    }
});

function load() {
    base = d3.select("#viewer-container");
    canvas = base.append("canvas")
        .attr("width", width)
        .attr("height", height)
        .call(d3.zoom()
            .extent([
                [-imgWidth / 2, -imgHeight / 2],
                [imgWidth + imgWidth / 2, imgHeight + imgHeight / 2]
            ])
            .scaleExtent([0.5, 4])
            .on("zoom", zoomed)
            .on("start", startZoom)
            .on("end", endZoom));
    transform = d3.zoomIdentity;
    context = canvas.node().getContext("2d");
    
    $("canvas").bind("wheel mousewheel", function(e) {e.preventDefault()});$("canvas");
    
    endZoom();
    canvas.call(render);
}

function zoomed() {
    if(transform.k > d3.event.transform.k) {
        $("canvas").css( 'cursor', 'zoom-out' );
    } else if(transform.k < d3.event.transform.k) {
        $("canvas").css( 'cursor', 'zoom-in' );
    } else {
        $("canvas").css( 'cursor', 'move' );
    }

    if(d3.event.transform.x > width / 2) d3.event.transform.x = width / 2;
    if(d3.event.transform.y > height / 2) d3.event.transform.y = height / 2;

    if(d3.event.transform.x < width / 2 - imgWidth * d3.event.transform.k) d3.event.transform.x = width / 2 - imgWidth * d3.event.transform.k;
    if(d3.event.transform.y < height / 2 - imgHeight * d3.event.transform.k) d3.event.transform.y = height / 2 - imgHeight * d3.event.transform.k;
    
    transform = d3.event.transform;

    render();
}

function startZoom() {
    
}

function endZoom() {
    $("canvas").css( 'cursor', 'grab' );
}

var j = 0;
function render() {
    context.save();
    context.clearRect(0, 0, width, height);
    context.fillStyle = "#444444";
    context.fillRect(0, 0, width, height);
    context.translate(transform.x, transform.y);
    context.scale(transform.k, transform.k);
    context.drawImage(images[0], 0, 0);
    context.drawImage(images[1], 0, 0);
    context.drawImage(images[2], 0, 0);
    context.drawImage(images[3], 0, 0);
    context.drawImage(images[4], 0, 0);
    context.restore();
}