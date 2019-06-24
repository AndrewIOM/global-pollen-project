///////////////////////////////////////////////
/// Global Pollen Project - Base Script Bundle
///////////////////////////////////////////////

// Hook up individual GPP components
import * as ES6Promise from "es6-promise";
ES6Promise.polyfill();

async function autocomplete() {
    const container = document.getElementById("ref-collection-search");
    if (container !== null) {
        const component = await import("./Components/suggest");
        component.activate(container);
    }
}

async function grainMap() {
    const container = document.getElementById("locations-map");
    if (container !== null) {
        const component = await import("./Components/grain-map");
        component.activate(container);
    }
}

async function unobtrusiveValidation() {
    if (document.forms.length > 0) {
        await import("./Components/validation");
    }
}

async function digitiseSPA() {
    const container = document.getElementById("digitise-app");
    if (container !== null) {
        const component = await import("./Digitise/digitise-app");
        component.activate(container);
    }
}

autocomplete();
grainMap();
unobtrusiveValidation();
digitiseSPA();