///////////////////////////////////////////////
/// Global Pollen Project - Base Script Bundle
///////////////////////////////////////////////

// Hook up individual GPP components
import * as ES6Promise from "es6-promise";
ES6Promise.polyfill();

async function autocomplete() {
    const container = document.getElementById("ref-collection-search");
    if (container !== null) {
        const component = await import(/* webpackChunkName: "suggest" */"./Components/suggest");
        component.activate(container);
    }
}

async function grainMap() {
    const container = document.getElementById("locations-map");
    if (container !== null) {
        const component = await import(/* webpackChunkName: "map-unidentified" */"./Components/grain-map");
        component.activate(container);
    }
}

autocomplete();
grainMap();
