/**
 * Add-on for a slide viewer - adds a slider to the right hand side, 
 * which calls the "setFocusLevel" function of the viewer, to flick between
 * focus levels
 * @param {Viewer} viewer   the Viewer object to attach the slider to
 * @param {String} sliderId the desired slider object id
 */
function FocusSlider(viewer, sliderId) {
    var self = this;

    self.viewer = viewer;

    self.id = sliderId;
    self.scale = null;

    // initialise after the images are loaded
    $(self.viewer).on(ViewerEvent.EVENT_LOADED_IMAGES, function () {

        var startLevel = Math.floor(Math.ceil(self.viewer.getMaxFocusLevel() / 2));
        self.viewer.setFocusLevel(startLevel);

        var svg = d3.select(self.viewer.containerId).append("svg")
            .attr("width", 50)
            .attr("height", self.viewer.height * 0.65)
            .attr("id", self.id.substr(1));

        $(self.id).css("position", "absolute");
        $(self.id).css("right", 15);
        $(self.id).css("top", self.viewer.height * 0.25);

        var slider = svg.append("g");

        // create the background box for the slider
        slider.append("rect")
            .attr("class", "viewer-focusslider-background")
            .attr("x", 0)
            .attr("y", 0)
            .attr("width", 100)
            .attr("height", self.viewer.height * 0.65)
            .attr("opacity", 0.5)
            .attr("rx", 15)
            .attr("ry", 15);
        
        // "eye" icon
        slider.append("text")
            .attr("font-family", "FontAwesome")
            .attr("font-size", "26px")
            .attr("x", 11)
            .attr("y", self.viewer.height * 0.07)
            .attr("fill", "white")
            .text("\uf030");

        // create the line for the slider
        var ly = self.viewer.height * 0.1;
        var lh = self.viewer.height * 0.5;
        self.scale = d3.scaleLinear()
            .domain([self.viewer.getMaxFocusLevel(), 0])
            .range([ly, lh + ly])
            .clamp(true);

        slider.append("rect")
            .attr("class", "viewer-focusslider-line")
            .attr("x", 23)
            .attr("y", ly)
            .attr("width", 4)
            .attr("height", lh)
            .attr("fill", "white");

        var tickArray = Array.apply(null, {
            length: self.viewer.getMaxFocusLevel() + 1
        }).map(Number.call, Number);

        slider.selectAll(".tick")
            .data(tickArray)
            .enter()
            .append("rect")
            .attr("y", function (d) {
                return self.scale(d);
            })
            .attr("x", 20)
            .attr("width", 10)
            .attr("height", 2)
            .attr("fill", "white");

        // create the handle for the slider
        self.handle = slider.append("circle")
            .attr("cx", 25)
            .attr("cy", self.scale(self.viewer.focusLevel))
            .attr("stroke", "white")
            .attr("stroke-width", 3)
            .attr("r", 9);

        // invisible dragging rectangle
        slider.append("rect")
            .style("cursor", "crosshair")
            .attr("x", 0)
            .attr("y", 0)
            .attr("width", 50)
            .attr("height", self.viewer.height * 0.6 + 20)
            .attr("opacity", 0)
            .call(d3.drag()
                .on("start.interrupt", function () {
                    slider.interrupt();
                })
                .on("start drag", function () {
                    var cy = d3.event.y;
                    if (cy < ly) cy = ly;
                    if (cy > ly + lh) cy = ly + lh;

                    self.handle.attr("cy", cy);
                    self.viewer.setFocusLevel(Math.round(self.scale.invert(d3.event.y)));
                })
                .on("end", function() {
                    var t = d3.transition()
                        .duration(100)
                        .ease(d3.easeCubic);

                    self.handle.transition(t)
                        .attr("cy",self.scale(Math.round(self.scale.invert(d3.event.y))));
                })
            );
    });

    /**
     * Cleanly disposes of the slider
     */
    self.dispose = function() {
        $(self.id).remove();
    }
}