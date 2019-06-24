import * as L from 'leaflet'

var gbifMap = function (gbifId) {
    var map = L.map('map', {
        center: [30, 0],
        zoom: 1
    });
    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token={accessToken}', {
        attribution: 'Imagery © <a href="http://mapbox.com">Mapbox</a>',
        maxZoom: 18,
        id: 'mareep2000.onj49m55',
        accessToken: 'pk.eyJ1IjoibWFyZWVwMjAwMCIsImEiOiJjaWppeGUxdm8wMDQ3dmVtNHNhcHh0cHA1In0.OrAULrL8pJaL9N5WerUUDQ'
    }).addTo(map);
    var baseUrl = 'https://api.gbif.org/v1/map/density/tile?x={x}&y={y}&z={z}&type=TAXON&key=' + gbifId + '&layer=OBS_2000_2010&layer=SP_2000_2010&layer=OBS_2010_2020&layer=SP_2010_2020&layer=LIVING&palette=yellows_reds';
    var gbifAttrib = 'GBIF contributors';
    var gbif = new L.TileLayer(baseUrl, { minZoom: 0, maxZoom: 14, attribution: gbifAttrib }).addTo(map);
}

export function activate(container: HTMLElement) {
    var gbifId = $('#GbifId').val();
    if (gbifId != 0) {
        gbifMap(gbifId);
    }
}
