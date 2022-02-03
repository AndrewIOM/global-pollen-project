import * as d3 from 'd3';
/// <reference types="@types/jquery-jcrop" />

export interface BoundingBox {
    x: number
    y: number
    w: number
    h: number
}

// Creates a new slide image canvas - allows panning and zooming, and ensures the slide is always visible
export class Viewer {

    id: string;
    containerId: string;
    width: number;
    height: number;
    imagePaths: Array<string>;
    crop: BoundingBox;
    images = [];
    loadedCounter: number;
    base: d3.Selection<d3.BaseType, {}, HTMLElement, any>; // the parent container of the whole viewer
    canvas: d3.Selection<HTMLCanvasElement, {}, HTMLElement, any>; // the canvas element - displays the image of the slide
    transform: d3.ZoomTransform; // zoom/pan transform
    context: CanvasRenderingContext2D; // the canvas 2d context - for drawing on the canvas
    imgWidth: number | undefined; // stores the width of the slide image
    imgHeight: number | undefined; // stores the height of the slide image
    nativeWidth: number;
    nativeHeight: number;
    focusLevel: number; // holds the current focus level (image to draw)

    /**
     * Creates a new slide image canvas - allows panning and zooming, and ensures the slide is always visible
     * @param {string} containerId      the parent object's id
     * @param {string} canvasId         the desired canvas object id
     * @param {number} width            the desired width of the canvas
     * @param {number} height           the desired height of the canvas
     * @param {[string]} imagePaths     an array of image paths
     * @param {{x,y,w,h}} crop          an optional object representing the cropped region to draw (represented as percentage decimals)
     * @return {Viewer}                 the relevant viewer object
     */
    constructor(containerId:string, canvasId:string, width:number, height:number, imagePaths:string[], crop?:BoundingBox) {
        this.id = canvasId;
        this.containerId = containerId;
        this.width = width;
        this.height = height;
        this.imagePaths = imagePaths;
        this.crop = crop;
        this.images = [];
        this.loadedCounter = 0;
        this.focusLevel = 0;
        this.imgHeight = undefined;
        this.imgWidth = undefined;
        $(this.containerId).css("position", "relative");
        $(this.containerId).css("width", this.width);
        $(this.containerId).css("height", this.height);
        this.loadImages(() => {
            console.log(this);
            this.createCanvas(() => {
                console.log(this);
                this.render();
            });
        });
    }
    
    public getImagePaths() {
        return this.imagePaths;
    }
    
    /**
     * Populates the "images" array with image objects loaded using image paths
     * Will throw an error if the images are not equal sized
     */
    public loadImages(callback) {
        // loop through all image paths (focus levels), and load the images
        let error = false;
        for (let i = 0; i < this.imagePaths.length; i++) {
            let img = new Image();
            img.onload = (_) => {
                // ensure all focus level images have the same dimensions
                if (this.imgWidth != undefined && this.imgHeight != undefined) {
                    if (img.width != this.imgWidth || img.height != this.imgHeight) {
                        console.error("Focus images are not of equal size! Size of image #" + i + ": " +
                            img.width + "x" + img.height + " - expected size: " + 
                            this.imgWidth + "x" + this.imgHeight);
                        if(!error) {
                            error = true;
                            $(this.id).trigger(ViewerEvent.EVENT_IMAGES_MISMATCHED_SIZE);
                        }
                        return;
                    }
                } else {
                    this.imgWidth = this.nativeWidth = img.width;
                    this.imgHeight = this.nativeHeight = img.height;
                }

                this.loadedCounter++;
                if (this.loadedCounter == this.imagePaths.length) {
                    // override imgWidth and imgHeight if a crop has been defined
                    if(this.crop != null) {
                        this.nativeWidth = this.imgWidth;
                        this.nativeHeight = this.imgHeight;

                        this.imgWidth *= this.crop.w;
                        this.imgHeight *= this.crop.h;
                    }

                    // proceed if all images have been loaded - trigger a jQuery function too
                    callback();
                    $(this.id).trigger(ViewerEvent.EVENT_LOADED_IMAGES);
                }
            }
            img.src = this.imagePaths[i];
            this.images.push(img);
        }
    }

    /**
     * Creates the <canvas> element, and initialises panning + zooming through d3
     */
    public createCanvas(callback) {
        // sub-function - zoom callback - gets called when canvas is zoomed
        const zoomed = () => {
            if (this.transform.k > d3.event.transform.k) {
                $(this.id).css("cursor", "zoom-out");
            } else if (this.transform.k < d3.event.transform.k) {
                $(this.id).css("cursor", "zoom-in");
            } else {
                $(this.id).css("cursor", "move");
            }

            if (d3.event.transform.x > this.width / 2) 
                d3.event.transform.x = this.width / 2;
            if (d3.event.transform.y > this.height / 2) 
                d3.event.transform.y = this.height / 2;

            if (d3.event.transform.x < this.width / 2 - this.imgWidth * d3.event.transform.k) 
                d3.event.transform.x = this.width / 2 - this.imgWidth * d3.event.transform.k;
            if (d3.event.transform.y < this.height / 2 - this.imgHeight * d3.event.transform.k) 
                d3.event.transform.y = this.height / 2 - this.imgHeight * d3.event.transform.k;

            this.transform = d3.event.transform;

            // trigger a zoom event
            $(this.id).trigger(ViewerEvent.EVENT_ZOOMED);
        }

        const defaultZoom = Math.min(this.width / this.imgWidth, this.height / this.imgHeight);

        // create the canvas element
        this.base = d3.select(this.containerId);
        this.canvas = this.base.append<HTMLCanvasElement>("canvas")
            .attr("id", this.id.substr(1))
            .attr("width", this.width)
            .attr("height", this.height)
            .call(d3.zoom()
                .extent([
                    [-this.imgWidth / 2, -this.imgHeight / 2],
                    [this.imgWidth + this.imgWidth / 2, this.imgHeight + this.imgHeight / 2]
                ])
                .scaleExtent([defaultZoom * 0.4, defaultZoom * 10])
                .on("zoom", () => {
                    zoomed();
                    this.render();
                })
                .on("end", () => {
                    $(this.id).css("cursor", "grab");
                }));
        this.transform = d3.zoomIdentity;
        this.transform.scale(defaultZoom - 0.1);
        this.transform.translate(this.width / 2 - this.imgWidth / 2 * this.transform.k,
            this.height / 2 - this.imgHeight / 2 * this.transform.k);

        this.context = this.canvas.node().getContext("2d");
        
        $(this.id).data("viewer", this);
        
        $(this.id).css("position", "absolute");
        $(this.id).css("cursor", "grab"); // set the cursor to "grab" initially

        // stop the page from scrolling when zooming with the mouse wheel
        $(this.id).on("wheel mousewheel", (e) => {
            e.preventDefault()
        });

        // initialisation is complete - proceed through callback
        callback();
    }

    /**
     * To be called when the canvas needs to be redrawn
     */
    public render() {
        this.context.save();
        
        // clear the screen
        this.context.clearRect(0, 0, this.width, this.height);
        this.context.fillStyle = "#777777";
        this.context.fillRect(0, 0, this.width, this.height);

        // draw the correct slide image in the correct location
        this.context.translate(this.transform.x, this.transform.y);
        this.context.scale(this.transform.k, this.transform.k);
        this.context.shadowColor = '#555555';
        this.context.shadowBlur = 20;
        this.context.shadowOffsetX = 15;
        this.context.shadowOffsetY = 15;
        if(this.crop == null) {
            this.context.drawImage(this.images[this.focusLevel], 0, 0);
        } else {
            this.context.drawImage(
                this.images[this.focusLevel], 
                this.crop.x * this.nativeWidth, 
                this.crop.y * this.nativeHeight,
                this.imgWidth,
                this.imgHeight, 
                0, 0, 
                this.imgWidth,
                this.imgHeight
            );
        }

        this.context.restore();
    }

    /**
     * Switches the displayed image to the intended focus level
     */
    public setFocusLevel(level) {
        if(level < 0) level = 0;
        if(level > this.getMaxFocusLevel()) level = this.getMaxFocusLevel();
        this.focusLevel = level;
        this.render();
    }

    /**
     * Returns the maximum possible focus level (minimum is always 0)
     */
    public getMaxFocusLevel() {
        return this.images.length - 1;
    }

    /**
     * Returns the current zoom level
     */
    public getZoom() {
        return this.transform.k;
    }

    /**
     * Returns the current X transform (panning)
     */
    public getTransformX() {
        return this.transform.x;
    }

    /**
     * Returns the current Y transform (panning)
     */
    public getTransformY() {
        return this.transform.y;
    }

    /**
     * Cleanly disposes of the viewer
     */
    public dispose() {
        $(this.id).remove();
    }
}

export enum ViewerEvent {
    EVENT_LOADED_IMAGES = "loadedImages",
    EVENT_ZOOMED = "zoomed",
    EVENT_IMAGES_MISMATCHED_SIZE = "imagesMismatchedSize"
}