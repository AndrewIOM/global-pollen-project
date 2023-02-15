import * as d3 from 'd3';
import { Viewer, ViewerEvent } from './viewer';

/**
 * Add-on for a slide viewer - adds a slider to the right hand side, 
 * which calls the "setFocusLevel" function of the viewer, to flick between
 * focus levels
 * @param {Viewer} viewer   the Viewer object to attach the slider to
 * @param {String} sliderId the desired slider object id
 */
export class FocusSlider {
    
    id: string;
    viewer: Viewer;
    scale: d3.ScaleLinear<number, number>;
    handle: d3.Selection<d3.BaseType, {}, HTMLElement, any>;
    
    constructor(viewer: Viewer, sliderId: string) {
        this.id = sliderId;
        this.viewer = viewer;
        $(this.viewer.containerId).on(ViewerEvent.EVENT_LOADED_IMAGES, () => {
            this.dispose();
            this.append();
        });
    }
    
    append() {
        const startLevel = Math.floor(Math.ceil(this.viewer.getMaxFocusLevel() / 2));
        this.viewer.setFocusLevel(startLevel);

        const svg = d3.select(this.viewer.containerId).append("svg")
            .attr("width", 50)
            .attr("height", this.viewer.height * 0.65)
            .attr("id", this.id.substr(1));

        $(this.id).css("position", "absolute");
        $(this.id).css("right", 0);
        $(this.id).css("top", (this.viewer.height - (this.viewer.height * 0.65)) / 2);

        let slider = svg.append("g");

        // create the background box for the slider
        slider.append("rect")
            .attr("class", "viewer-focusslider-background")
            .attr("x", 0)
            .attr("y", 0)
            .attr("width", 100)
            .attr("height", this.viewer.height * 0.65)
            .attr("opacity", 0.5)
            .attr("rx", 15)
            .attr("ry", 15);

        // "eye" icon
        slider.append("text")
            .attr("class", "fa")
            .attr("font-size", "26px")
            .attr("x", 11)
            .attr("y", this.viewer.height * 0.07)
            .attr("fill", "white")
            .text("\uf083");

        // create the line for the slider
        const ly = this.viewer.height * 0.1;
        const lh = this.viewer.height * 0.5;
        this.scale = d3.scaleLinear()
            .domain([this.viewer.getMaxFocusLevel(), 0])
            .range([ly, lh + ly])
            .clamp(true);

        slider.append("rect")
            .attr("class", "viewer-focusslider-line")
            .attr("x", 23)
            .attr("y", ly)
            .attr("width", 4)
            .attr("height", lh)
            .attr("fill", "white");

        const tickArray = Array.apply(null, {
            length: this.viewer.getMaxFocusLevel() + 1
        }).map(Number.call, Number);

        slider.selectAll(".tick")
            .data(tickArray)
            .enter()
            .append("rect")
            .attr("y", (d:number) => {
                return this.scale(d);
            })
            .attr("x", 20)
            .attr("width", 10)
            .attr("height", 2)
            .attr("fill", "white");

        // create the handle for the slider
        this.handle = slider.append("circle")
            .attr("cx", 25)
            .attr("cy", this.scale(this.viewer.focusLevel))
            .attr("stroke", "white")
            .attr("stroke-width", 3)
            .attr("r", 9);

        // invisible dragging rectangle
        slider.append("rect")
            .style("cursor", "crosshair")
            .attr("x", 0)
            .attr("y", 0)
            .attr("width", 50)
            .attr("height", this.viewer.height * 0.6 + 20)
            .attr("opacity", 0)
            .call(d3.drag()
                .on("start.interrupt", () => {
                    slider.interrupt();
                })
                .on("start drag", () => {
                    let cy = d3.event.y;
                    if (cy < ly) cy = ly;
                    if (cy > ly + lh) cy = ly + lh;
                    this.handle.attr("cy", cy);
                    this.viewer.setFocusLevel(Math.round(this.scale.invert(d3.event.y)));
                })
                .on("end", () => {
                    const t = d3.transition()
                        .duration(100)
                        .ease(d3.easeCubic);

                    this.handle.transition(t)
                        .attr("cy",this.scale(Math.round(this.scale.invert(d3.event.y))));
                })
            );
    }
    
    // Cleanly disposes of the slider
    public dispose() {
        $(this.id).remove();
    }
}