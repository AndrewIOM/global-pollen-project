import * as d3 from 'd3';
import { Viewer, ViewerEvent } from './viewer';

/**
 * Add-on for a slide viewer - when enabled, allows the user to click-drag
 * or click-click a line over the image, which shows an updating value for line length
 */
export class MeasuringLine {
    
    viewer: Viewer;
    id: string;
    disappear: boolean;
    scale: number;
    customText: string;
    state: MeasuringLineState;
    svg: d3.Selection<d3.BaseType, {}, HTMLElement, any>;

    line: d3.Selection<d3.BaseType, {}, HTMLElement, any>;
    startX: number;
    startY: number;
    endX: number;
    endY: number;

    // viewer zoom information
    viewerZoom: number;
    savedTransformX: number;
    savedTransformY: number;

    /**
     * @param viewer       the Viewer object to attach the measuring line to
     * @param toolId       the desired line tool id
     * @param disappear    should the line disappear after drawing?
     * @param scale        how many micrometers a single pixel represents - null if unknown
     * @param customText   if set, shows custom text next to cursor while drawing line, instead of distance information
     */
    constructor(viewer: Viewer, toolId: string, disappear: boolean, scale: number, customText: string) {
        this.viewer = viewer;
        this.id = toolId;
        this.disappear = disappear;
        this.scale = scale;
        this.customText = customText;
        this.state = MeasuringLineState.STATE_INACTIVE;
    }
    
    activate() {
        this.state = MeasuringLineState.STATE_ACTIVE;

        this.svg = d3.select(this.viewer.containerId).append("svg")
            .attr("width", this.viewer.width)
            .attr("height", this.viewer.height)
            .attr("id", this.id.substr(1))
            .on("mousedown", () => {
                const m = d3.mouse(d3.event.currentTarget);
                if (this.state == MeasuringLineState.STATE_ACTIVE) {
                    this.startLine(m[0], m[1]);
                } else if (this.state == MeasuringLineState.STATE_DRAWING) {
                    this.endLine(m[0], m[1]);
                }
            })
            .on("mousemove", () => {
                if(this.state == MeasuringLineState.STATE_DRAWING) {
                    const m = d3.mouse(d3.event.currentTarget);
                    this.endX = m[0];
                    this.endY = m[1];
                    this.redrawLine();
                }
            })
            .append("g");

        this.line = this.svg.append("line")
            .attr("stroke-width", "2")
            .attr("stroke", "white")
            .attr("visibility", "hidden");

        $(this.id).css("position", "absolute");
        $(this.id).css("cursor", "crosshair");

        $(this.viewer.containerId).on(ViewerEvent.EVENT_ZOOMED, () => {
            if(this.state == MeasuringLineState.STATE_DRAWN) {
                this.redrawLine();
            }
        });
    }

    redrawLine() {
        if(this.state == MeasuringLineState.STATE_DRAWING) {
            this.line.attr("visibility", "visible");
            this.line.attr("x1", this.startX);
            this.line.attr("y1", this.startY);
            this.line.attr("x2", this.endX);
            this.line.attr("y2", this.endY);
        } else if (this.state == MeasuringLineState.STATE_DRAWN) {
            const zoomDelta = this.viewer.getZoom() / this.viewerZoom;
            this.line.attr("visibility", "visible");
            this.line.attr("x1", (this.startX + (this.viewer.getTransformX() / zoomDelta - this.savedTransformX)) * zoomDelta);
            this.line.attr("y1", (this.startY + (this.viewer.getTransformY() / zoomDelta - this.savedTransformY)) * zoomDelta);
            this.line.attr("x2", (this.endX + (this.viewer.getTransformX() / zoomDelta - this.savedTransformX)) * zoomDelta);
            this.line.attr("y2", (this.endY + (this.viewer.getTransformY() / zoomDelta - this.savedTransformY)) * zoomDelta);
        }        
    }

    startLine(x, y) {
        this.state = MeasuringLineState.STATE_DRAWING;
        
        this.startX = x;
        this.startY = y;
        this.endX = x;
        this.endY = y;
        this.redrawLine();
        $(this.id).trigger(MeasuringLineEvent.EVENT_DRAWING);
    }

    endLine(x, y) {
        this.state = MeasuringLineState.STATE_DRAWN;
        this.endX = x;
        this.endY = y;

        this.viewerZoom = this.viewer.getZoom();
        this.savedTransformX = this.viewer.getTransformX();
        this.savedTransformY = this.viewer.getTransformY();

        $(this.id).css("pointer-events", "none");
        $(this.id).trigger(MeasuringLineEvent.EVENT_DRAWN);
    }

    getPixelPoints() {
        if(this.startX == null || this.startY == null || this.endX == null || this.endY == null) return null;

        return [
            [(this.startX - this.savedTransformX) / this.viewerZoom, (this.startY - this.savedTransformY) / this.viewerZoom],
            [(this.endX - this.savedTransformX) / this.viewerZoom, (this.endY - this.savedTransformY) / this.viewerZoom]
        ];
    }

    public getPixelLength() {
        const points = this.getPixelPoints();
        return Math.sqrt((points[0][0] - points[1][0]) * 
            (points[0][0] - points[1][0]) + 
            (points[0][1] - points[1][1]) *
            (points[0][1] - points[1][1]));
    }

    /**
     * Cleanly disposes of the viewer
     */
    dispose() {
        $(this.id).remove();
    }
}

export enum MeasuringLineState {
    STATE_INACTIVE = 0,
    STATE_ACTIVE = 1,
    STATE_DRAWING = 2,
    STATE_DRAWN = 3,
}

export enum MeasuringLineEvent {
    EVENT_DRAWING = "drawing",
    EVENT_DRAWN = "drawn"
}