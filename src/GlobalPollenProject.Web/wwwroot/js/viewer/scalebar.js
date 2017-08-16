/**
 * Add-on for a slide viewer - adds a scale bar to the bottom left of the
 * display. Automatically sizes based on viewer zoom level and pixel scale
 * @param {Viewer} viewer   the Viewer object to attach the scalebar to
 * @param {String} barId    the desired scalebar object id
 * @param {Float} scale     how many micrometers a single pixel represents
 */
function ScaleBar(viewer, barId, scale) {
    var self = this;

    self.viewer = viewer;
    self.scale = scale;

    self.id = barId;
    self.svg = null;
    self.line = null;
    self.startText = null;
    self.endText = null;

    self.redraw = function() {
        var endValue = ((self.viewer.width * 0.35 * self.scale) / self.viewer.getZoom()).toFixed(2);

        self.line.attr("width", Math.round(self.viewer.width * 0.35));
        self.endText.text(endValue.toString() + "μm");
        self.endText.attr("x", 10 + Math.round(self.viewer.width * 0.35) - self.endText.node().getComputedTextLength());
    }

    self.initialise = function() {
        self.svg = d3.select(self.viewer.containerId).append("svg")
            .attr("width", self.viewer.width)
            .attr("height", 50)
            .attr("pointer-events", "none")
            .attr("id", self.id.substr(1))
            .append("g");

        $(self.id).css("position", "absolute");
        $(self.id).css("left", 15);
        $(self.id).css("bottom", 10);

        self.line = self.svg.append("rect")
            .attr("x", 10)
            .attr("y", 24)
            .attr("stroke", "white")
            .attr("height", 3);
        
        self.startText = self.svg.append("text")
            .attr("font-family", "Courier")
            .attr("font-weight", "bold")
            .attr("font-size", "16px")
            .attr("x", 10)
            .attr("y", 40)
            .attr("fill", "black")
            .text("0μm");
        
        self.endText = self.svg.append("text")
            .attr("font-family", "Courier")
            .attr("font-weight", "bold")
            .attr("font-size", "16px")
            .attr("y", 40)
            .attr("fill", "black");

        self.redraw();
    }

    // initialise after the images are loaded
    $(self.viewer).on(Viewer.EVENT_LOADED_IMAGES, function () {
        self.initialise();
    });

    $(self.viewer).on(Viewer.EVENT_ZOOMED, function () {
        self.redraw();
    });

    /**
     * Cleanly disposes of the viewer
     */
    self.dispose = function() {
        $(self.id).remove();
    }
}