/**
 * Creates a new slide image canvas - allows panning and zooming, and ensures the slide is always visible
 * @param {String} containerId the parent object's id
 * @param {Int} width the desired width of the canvas
 * @param {Int} height the desired height of the canvas
 * @return {Viewer} the relevant viewer object
 */
function Viewer(containerId, width, height) {
    var self = this;

    this.id = "#viewer-canvas";

    // holds the function parameters
    this.containerId = containerId;
    this.width = width;
    this.height = height;

    // paths to the png slide images
    this.imagePaths = [
        "/images/tmp1.png",
        "/images/tmp2.png",
        "/images/tmp3.png",
        "/images/tmp4.png",
        "/images/tmp5.png"
    ];

    // stores the loaded image objects
    this.images = [];
    this.loadedCounter = 0;

    this.base = null; // the parent container of the whole viewer
    this.canvas = null; // the canvas element - displays the image of the slide
    this.transform = null; // zoom/pan transform
    this.context = null; // the canvas 2d context - for drawing on the canvas
    this.imgWidth = null; // stores the width of the slide image
    this.imgHeight = null; // stores the height of the slide image
    
    this.focusLevel = 0; // holds the current focus level (image to draw)

    /**
     * Populates the "images" array with image objects loaded using image paths
     * Will throw an error if the images are not equal sized
     */
    this.loadImages = function (callback) {
        // loop through all image paths (focus levels), and load the images
        for (var i = 0; i < this.imagePaths.length; i++) {
            var img = new Image();
            img.onload = function () {
                // ensure all focus level images have the same dimensions
                if (self.imgWidth != undefined && self.imgHeight != undefined) {
                    if (this.width != self.imgWidth || this.height != self.imgHeight) {
                        console.error("Focus images are not of equal size! Size of image #" + i + ": " +
                            this.width + "x" + this.height + " - expected size: " + 
                            self.imgWidth + "x" + self.imgHeight);
                    }
                } else {
                    self.imgWidth = this.width;
                    self.imgHeight = this.height;
                }

                self.loadedCounter++;
                if (self.loadedCounter == self.imagePaths.length - 1) {
                    // proceed if all images have been loaded - trigger a jQuery function too
                    callback();
                    $(self).trigger("loadedImages");
                }
            }
            img.src = this.imagePaths[i];
            this.images.push(img);
        }
    }

    /**
     * Creates the <canvas> element, and initialises panning + zooming through d3
     */
    this.createCanvas = function (callback) {

        // sub-function - zoom callback - gets called when canvas is zoomed
        function zoomed() {
            if (self.transform.k > d3.event.transform.k) {
                $(this.id).css("cursor", "zoom-out");
            } else if (self.transform.k < d3.event.transform.k) {
                $(this.id).css("cursor", "zoom-in");
            } else {
                $(this.id).css("cursor", "move");
            }

            if (d3.event.transform.x > self.width / 2) 
                d3.event.transform.x = self.width / 2;
            if (d3.event.transform.y > self.height / 2) 
                d3.event.transform.y = self.height / 2;

            if (d3.event.transform.x < self.width / 2 - self.imgWidth * d3.event.transform.k) 
                d3.event.transform.x = self.width / 2 - self.imgWidth * d3.event.transform.k;
            if (d3.event.transform.y < self.height / 2 - self.imgHeight * d3.event.transform.k) 
                d3.event.transform.y = self.height / 2 - self.imgHeight * d3.event.transform.k;

            self.transform = d3.event.transform;
        }

        // create the canvas element
        this.base = d3.select(this.containerId);
        this.canvas = this.base.append("canvas")
            .attr("id", this.id.substr(1))
            .attr("width", this.width)
            .attr("height", this.height)
            .call(d3.zoom()
                .extent([
                    [-this.imgWidth / 2, -this.imgHeight / 2],
                    [this.imgWidth + this.imgWidth / 2, this.imgHeight + this.imgHeight / 2]
                ])
                .scaleExtent([0.5, 4])
                .on("zoom", function() {
                    zoomed();
                    self.render();
                })
                .on("end", function() {
                    $(this.id).css("cursor", "grab");
                }));
        this.transform = d3.zoomIdentity;
        this.context = this.canvas.node().getContext("2d");

        $(this.id).css("cursor", "grab"); // set the cursor to "grab" initially

        // stop the page from scrolling when zooming with the mouse wheel
        $(this.id).bind("wheel mousewheel", function (e) {
            e.preventDefault()
        });

        // initialisation is complete - proceed through callback
        callback();
    }

    /**
     * To be called when the canvas needs to be redrawn
     */
    this.render = function() {
        this.context.save();
        
        // clear the screen
        this.context.clearRect(0, 0, this.width, this.height);
        this.context.fillStyle = "#444444";
        this.context.fillRect(0, 0, this.width, this.height);

        // draw the correct slide image in the correct location
        this.context.translate(this.transform.x, this.transform.y);
        this.context.scale(this.transform.k, this.transform.k);
        this.context.drawImage(this.images[this.focusLevel], 0, 0);

        this.context.restore();
    }

    /**
     * Switches the displayed image to the intended focus level
     */
    this.setFocusLevel = function(level) {
        if(level < 0) level = 0;
        if(level > this.getMaxFocusLevel()) level = this.getMaxFocusLevel();
        this.focusLevel = level;
        this.render();
    }

    /**
     * Returns the maximum possible focus level (minimum is always 0)
     */
    this.getMaxFocusLevel = function() {
        return this.images.length - 1;
    }

    // ENTRY POINT - functions have all been defined
    this.loadImages(function() {
        self.createCanvas(function() {
            self.render();
        });
    });
}