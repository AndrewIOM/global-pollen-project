///////////////////////////////////////////////
/// Global Pollen Project - Base Script Bundle
///////////////////////////////////////////////

import 'bootstrap';
import * as ES6Promise from "es6-promise";
ES6Promise.polyfill();

// Autocomplete field for latin names, limited to the master reference
// collection taxonomy.
async function autocomplete() {
    const container = document.getElementById("ref-collection-search");
    if (container !== null) {
        const component = await import("./Components/suggest");
        component.activate(container);
    }
}

// A gallery component for displaying one to many microscopic images.
// Includes scale bars and focus levels.
async function gallery() {
    const container = document.getElementById("slide-gallery");
    if (container !== null) {
        const component = await import("./Components/gallery");
        component.activate(container);
    }
}

// Global map of point locations, used on home page
async function grainMap() {
    const container = document.getElementById("locations-map");
    if (container !== null) {
        const component = await import("./Components/grain-map");
        component.activate(container);
    }
}

async function distributionMap() {
    const container = document.getElementById("distribution-map-component");
    if (container !== null) {
        const component = await import("./Components/distribution-map");
        component.activate(container);
    }
}


async function unobtrusiveValidation() {
    if (document.forms.length > 0) {
        await import("./Components/validation");
    }
}

// Page-specific components
// --------------------------

async function addGrainForm() {
    const form = document.getElementById("add-grain-form");
    if (form !== null) {
        const component = await import("./UnknownMaterial/upload-grain");
        component.activate(form);
    }
}

// Family, Genus, Species boxes and dropdowns for looking up
// names in the taxonomic backbone.
async function identifyGrainForm() {
    const form = document.getElementById("identify-form");
    if (form !== null) {
        const component = await import("./UnknownMaterial/identify-grain");
        component.activate(form);
    }
}

// A single page digitise app using Knockout.
async function digitiseSPA() {
    const container = document.getElementById("digitise-app");
    if (container !== null) {
        const component = await import("./Digitise/digitise-app");
        component.activate(container);
    }
}

autocomplete();
gallery();
grainMap();
unobtrusiveValidation();
digitiseSPA();
addGrainForm();
identifyGrainForm();
distributionMap();