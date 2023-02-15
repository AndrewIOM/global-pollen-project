import * as d3 from 'd3';
import { Viewer, ViewerEvent } from './viewer';

/**
 * Add-on for a slide viewer - adds a scale bar to the bottom left of the
 * display. Automatically sizes based on viewer zoom level and pixel scale
 */
export class ScaleBar {

    viewer: Viewer;
    id: string
    scale: number;
    svg: d3.Selection<d3.BaseType, {}, HTMLElement, any>;
    line: d3.Selection<d3.BaseType, {}, HTMLElement, any>;
    startText: d3.Selection<SVGTextContentElement, {}, HTMLElement, any>;
    endText: d3.Selection<SVGTextContentElement, {}, HTMLElement, any>;
    loaded: boolean;
    
    constructor(viewer, barId, scale) {
        this.loaded = false;
        this.viewer = viewer;
        this.id = barId;
        this.scale = scale;
        $(this.viewer.containerId).on(ViewerEvent.EVENT_LOADED_IMAGES, () => {
            this.dispose();
            this.initialise();
            this.loaded = true;
        });
        $(this.viewer.containerId).on(ViewerEvent.EVENT_ZOOMED, () => {
            if (this.loaded) this.redraw();
        });
    }

    public redraw() {
        const endValue = ((this.viewer.width * 0.35 * this.scale) / this.viewer.getZoom()).toFixed(2);
        this.line.attr("width", Math.round(this.viewer.width * 0.35));
        this.endText.text(endValue.toString() + "μm");
        this.endText.attr("x", 10 + Math.round(this.viewer.width * 0.35) - this.endText.node().getComputedTextLength());
    }

    initialise() {
        this.svg = d3.select(this.viewer.containerId).append("svg")
            .attr("width", this.viewer.width)
            .attr("height", 50)
            .attr("pointer-events", "none")
            .attr("id", this.id.substr(1))
            .append("g");

        $(this.id).css("position", "absolute");
        $(this.id).css("left", 15);
        $(this.id).css("bottom", 10);

        this.line = this.svg.append("rect")
            .attr("x", 10)
            .attr("y", 24)
            .attr("stroke", "white")
            .attr("height", 3);
        
        this.startText = this.svg.append<SVGTextContentElement>("text")
            .attr("font-family", "Courier")
            .attr("font-weight", "bold")
            .attr("font-size", "16px")
            .attr("x", 10)
            .attr("y", 40)
            .attr("fill", "black")
            .text("0μm");
        
        this.endText = this.svg.append<SVGTextContentElement>("text")
            .attr("font-family", "Courier")
            .attr("font-weight", "bold")
            .attr("font-size", "16px")
            .attr("y", 40)
            .attr("fill", "black");

        this.redraw();
    }
    
    /**
     * Cleanly disposes of the viewer
     */
    public dispose() {
        $(this.id).remove();
    }
}