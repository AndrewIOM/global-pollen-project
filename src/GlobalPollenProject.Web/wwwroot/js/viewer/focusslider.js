/**
 * 
 * @param {Viewer} viewer the Viewer object to attach the slider to
 */
function FocusSlider(viewer) {
    var self = this;

    this.scale = null;

    // function params
    this.viewer = viewer;

    this.id = "#viewer-focusslider";

    $(this.viewer).on("loadedImages", function () {

        var startLevel = parseInt(Math.ceil(self.viewer.getMaxFocusLevel() / 2));
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
        
        slider.append("text")
            .attr("font-family", "FontAwesome")
            .attr("font-size", "30px")
            .attr("x", 10)
            .attr("y", self.viewer.height * 0.07)
            .attr("fill", "none")
            .attr("stroke", "white")
            .attr("stroke-width", 1.5)
            .text("\uf06e"); 

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
            .attr("rx", 3)
            .attr("ry", 3)
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
            .attr("height", 1)
            .attr("fill", "white");

        // create the handle for the slider
        self.handle = slider.append("circle")
            .attr("cx", 25)
            .attr("cy", self.scale(self.viewer.focusLevel))
            .attr("stroke", "white")
            .attr("stroke-width", 2)
            .attr("r", 9);

        // invisible dragging rectangle
        slider.append("rect")
            .style("cursor", "ns-resize")
            .attr("x", 0)
            .attr("y", 0)
            .attr("width", 50)
            .attr("height", self.viewer.height * 0.6)
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
                    self.viewer.setFocusLevel(parseInt(Math.round(self.scale.invert(d3.event.y))));
                }));
    });
}