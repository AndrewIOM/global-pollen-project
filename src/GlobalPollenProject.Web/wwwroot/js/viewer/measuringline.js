/**
 * Add-on for a slide viewer - when enabled, allows the user to click-drag
 * or click-click a line over the image, which shows an updating value for line length
 * @param {Viewer} viewer       the Viewer object to attach the measuring line to
 * @param {String} toolId       the desired line tool id
 * @param {Bool} disappear      should the line disappear after drawing?
 * @param {Float} scale         how many micrometers a single pixel represents - null if unknown
 * @param {String} customText   if set, shows custom text next to cursor while drawing line,
 *                              instead of distance information
 */
function MeasuringLine(viewer, toolId, disappear, scale, customText) {
    var self = this;

    // function parameters
    self.viewer = viewer;
    self.id = toolId;
    self.disappear = disappear;
    self.scale = scale;
    self.customText = customText;

    // the component's state
    self.state = MeasuringLine.STATE_DEACTIVATED;

    // line geometry
    self.line = null;
    self.startX = null;
    self.startY = null;
    self.endX = null;
    self.endY = null;

    // viewer zoom information
    self.viewerZoom = null;
    self.savedTransformX = null;
    self.savedTransformY = null;

    self.activate = function () {
        self.state = MeasuringLine.STATE_ACTIVATED;

        self.svg = d3.select(self.viewer.containerId).append("svg")
            .attr("width", self.viewer.width)
            .attr("height", self.viewer.height)
            .attr("id", self.id.substr(1))
            .on("mousedown", function () {
                var m = d3.mouse(this);
                if (self.state == MeasuringLine.STATE_ACTIVATED) {
                    self.startLine(m[0], m[1]);
                } else if (self.state == MeasuringLine.STATE_DRAWING) {
                    self.endLine(m[0], m[1]);
                }
            })
            .on("mousemove", function () {
                if(self.state == MeasuringLine.STATE_DRAWING) {
                    var m = d3.mouse(this);
                    self.endX = m[0];
                    self.endY = m[1];
                    self.redrawLine();
                }
            })
            .append("g");

        self.line = self.svg.append("line")
            .attr("stroke-width", "2")
            .attr("stroke", "white")
            .attr("visibility", "hidden");

        $(self.id).css("position", "absolute");
        $(self.id).css("cursor", "crosshair");

        $(self.viewer).on(Viewer.EVENT_ZOOMED, function() {
            if(self.state == MeasuringLine.STATE_DRAWN) {
                self.redrawLine();
            }
        });
    }

    self.redrawLine = function () {
        if(self.state == MeasuringLine.STATE_DRAWING) {
            self.line.attr("visibility", "visible");
            self.line.attr("x1", self.startX);
            self.line.attr("y1", self.startY);
            self.line.attr("x2", self.endX);
            self.line.attr("y2", self.endY);
        } else if (self.state == MeasuringLine.STATE_DRAWN) {
            var zoomDelta = self.viewer.getZoom() / self.viewerZoom;

            self.line.attr("visibility", "visible");
            self.line.attr("x1", (self.startX + (self.viewer.getTransformX() / zoomDelta - self.savedTransformX)) * zoomDelta);
            self.line.attr("y1", (self.startY + (self.viewer.getTransformY() / zoomDelta - self.savedTransformY)) * zoomDelta);
            self.line.attr("x2", (self.endX + (self.viewer.getTransformX() / zoomDelta - self.savedTransformX)) * zoomDelta);
            self.line.attr("y2", (self.endY + (self.viewer.getTransformY() / zoomDelta - self.savedTransformY)) * zoomDelta);
        }        
    }

    self.startLine = function (x, y) {
        self.state = MeasuringLine.STATE_DRAWING;
        self.startX = x;
        self.startY = y;
        self.endX = x;
        self.endY = y;
        self.redrawLine();
    }

    self.endLine = function (x, y) {
        self.state = MeasuringLine.STATE_DRAWN;
        self.endX = x;
        self.endY = y;

        self.viewerZoom = self.viewer.getZoom();
        self.savedTransformX = self.viewer.getTransformX();
        self.savedTransformY = self.viewer.getTransformY();

        $(self.id).css("pointer-events", "none");
    }
}

MeasuringLine.STATE_DEACTIVATED = 0;
MeasuringLine.STATE_ACTIVATED = 1;
MeasuringLine.STATE_DRAWING = 2;
MeasuringLine.STATE_DRAWN = 3;