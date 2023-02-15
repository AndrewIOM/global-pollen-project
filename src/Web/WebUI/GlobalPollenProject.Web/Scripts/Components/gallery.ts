import { Viewer } from "./Viewer/viewer";
import { FocusSlider } from "./Viewer/focusslider";
import { ScaleBar } from "./Viewer/scalebar";
import "knockout";

export function activate(_: HTMLElement) {
    const image = $(".slide-gallery-item").first();
    if (image) {
        const frames = image.data("frames");
        const width = image.data("pixelwidth") as number;
        console.log(frames);
        console.log(width);
        new Gallery(frames, width);
    }
}

export class Gallery {
    
    viewer: Viewer
    slider: FocusSlider
    scaleBar: ScaleBar
    //jCropApi: JQuery.Jcrop.Api
    
    containerId = "#viewer-container"
    canvasId = "#viewer-canvas"
    sliderId = "#viewer-focus-slider"
    scaleBarId = "#viewer-scaleBar"    

    /**
     * Creates/updates the image viewer
     * @param {*} frames        an array of image URLs, in focus order
     * @param {*} pixelWidth    micrometers in a single pixel
     */
    constructor(frames:string[], pixelWidth:number) {
        this.changeImage(frames, pixelWidth);
        this.activateGalleryLinks();
        $(".slide-gallery-item").first().addClass("active");
    }
    
    changeImage(frames: string[], pixelWidth: number) {
        if(this.slider != null) this.slider.dispose();
        if(this.scaleBar != null) this.scaleBar.dispose();
        if(this.viewer != null) this.viewer.dispose();
        $("#viewer-container").empty();
        this.viewer = new Viewer(this.containerId, this.canvasId, $(this.containerId).width(), 500, frames);
        if(frames.length > 1) {
            this.slider = new FocusSlider(this.viewer, this.sliderId);
        }
        this.scaleBar = new ScaleBar(this.viewer, this.scaleBarId, pixelWidth);
    }
    
    activateGalleryLinks() {
        $("#slide-gallery")
            .find(".slide-gallery-item").on("click", e => this.clickedGalleryItem(e));
    }
    
    clickedGalleryItem(element:JQuery.ClickEvent) {
        if($(element.target).hasClass("active")) return;
        $("#slide-gallery")
            .find(".slide-gallery-item")
            .removeClass("active");
        $(element.target).addClass("active");
        const frames = $(element.target).data("frames");
        this.changeImage(frames, parseFloat($(element.target).data("pixelwidth")));
    }
    
    // TODO Have removed cropper function
    
}
