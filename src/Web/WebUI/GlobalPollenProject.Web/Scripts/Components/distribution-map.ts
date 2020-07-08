import * as d3 from 'd3'
import * as topojson from 'topojson-client'
import * as noUiSlider from 'noUiSlider'
import * as L from 'leaflet'

// Component that displays modern distributions using leaflet maps,
// and Holocene distributions using D3 maps.

const mapBoxId = "mareep2000.onj49m55"
const mapBoxAccessToken = "pk.eyJ1IjoibWFyZWVwMjAwMCIsImEiOiJjaWppeGUxdm8wMDQ3dmVtNHNhcHh0cHA1In0.OrAULrL8pJaL9N5WerUUDQ"

export function activate(container: HTMLElement) {
    $(() => {
        const gbifId = $('#GbifId').val();
        if (gbifId != 0) {
            gbifMap(gbifId);
        } else {
            console.warn("There was no GBIF ID present (in #GbifId")
        }
        const neotomaId = $('#NeotomaId').val();
        if (typeof(neotomaId) == "number") {
            new PointDistributionMap(neotomaId);
        } else {
            console.warn("Neotoma ID was invalid")
        }
        
        // Toggle for palaeo versus modern map
        $("input[name=distribution]").on("change",e => {
            const selected = ($(e.currentTarget).val());
            if (selected == 'palaeo') {
                $('#palaeo').show();
                $('#modern').hide();
            } else {
                $('#palaeo').hide();
                $('#modern').show();
            }
        });
    })
}

// Display a leaflet map component
function gbifMap(gbifId) {
    const map = L.map('map', {
        center: [30, 0],
        zoom: 1
    });
    L.tileLayer('https://api.tiles.mapbox.com/v4/{id}/{z}/{x}/{y}.png?access_token={accessToken}', {
        attribution: 'Imagery © <a href="http://mapbox.com">Mapbox</a>',
        maxZoom: 18,
        id: mapBoxId,
        accessToken: mapBoxAccessToken
    }).addTo(map);
    const baseUrl = 'https://api.gbif.org/v1/map/density/tile?x={x}&y={y}&z={z}&type=TAXON&key=' + gbifId + '&layer=OBS_2000_2010&layer=SP_2000_2010&layer=OBS_2010_2020&layer=SP_2010_2020&layer=LIVING&palette=yellows_reds';
    const gbifAttrib = 'GBIF contributors';
    new L.TileLayer(baseUrl, { minZoom: 0, maxZoom: 14, attribution: gbifAttrib }).addTo(map);
}


class PointDistributionMap {
    
    points: any[]
    slider: noUiSlider.noUiSlider
    yearOldest: number
    yearYoungest: number
    domPoints: d3.Selection<d3.BaseType, any, d3.BaseType, unknown>
    color: d3.ScaleLinear<string, string>
    loadingElement: JQuery<HTMLElement>
    neotomaId: number
    projection: d3.GeoProjection
    svg: d3.Selection<d3.BaseType, unknown, HTMLElement, any>
    
    constructor(neotomaId:number) {
        $('#palaeo-loading').show();
        
        this.neotomaId = neotomaId;
        this.yearOldest = 10000;
        this.yearYoungest = 1000;
        this.loadingElement = $('#paleo-loading');
        this.color =
            d3.scaleLinear<string>()
                .domain([this.yearYoungest, this.yearOldest])
                .range(["yellow", "#83296F"]);

        let mapElement = document.getElementById('paleo');
        mapElement.style.display = 'block';
        const width = $('#neotoma-map').closest('div').width();
        const height = 250;
        mapElement.style.display = 'none';
        this.projection = d3.geoEquirectangular()
            .scale((width + 1) / 2 / Math.PI)
            .translate([width / 2, height / 2])
            .precision(.1);
        
        const slider = setupSlider();
        const svg = d3.select('#neotoma-map')
            .append('svg')
            .attr('width', width)
            .attr('height', height);
        const path = d3.geoPath().projection(this.projection);
        const g = svg.append("g");
        d3.json('/geojson/world110.json'), (error, topology) => {
            const geoJson : any = topojson.feature(topology, topology.objects.countries)
            g.selectAll("path")
                .data(geoJson.features)
                .enter().append("path")
                .attr("d", path)
                .attr('fill', 'grey');
        };
        if (neotomaId == 0) {
            this.loadingElement.text('Past occurrences for this taxon are not available from Neotoma.');
            this.loadingElement.show();
        } else {
            this.getNeotomaPoints();
            this.updatePastDistributionText();
            slider.on('slide', () => {
                this.updatePastDistributionText();
                this.redraw();
            });
        }
    }

    redraw() {
        this.domPoints.attr('display', 'none');
        this.domPoints.filter(d => {
            return d.youngest < this.yearOldest && this.yearYoungest < d.oldest
        }).attr('display', '');
    }

    updatePastDistributionText() {
        const value = this.slider.get();
        if (Array.isArray(value)) {
            this.yearOldest = Number(value[1]) * 1000;
            this.yearYoungest = Number(value[0]) * 1000;
            $('#palaeo-range-low').text(value[0]);
            $('#palaeo-range-hi').text(value[1] + ' thousand');
        }
    }

    getNeotomaPoints() {
        const neotomaUri = "https://api.neotomadb.org/v1/data/datasets?callback=neotomaCallback&taxonids=" + this.neotomaId + "&ageof=taxon&ageold=" + 50000 + "&ageyoung=" + 1000;
        $.ajax({
            url: neotomaUri,
            jsonp: false,
            jsonpCallback: 'neotomaCallback',
            cache: true,
            dataType: 'jsonp',
            error: function(xhr, textStatus, errorThrown) {
                $('#palaeo-loading').text("Sorry, we couldn't establish a secure connection with NeotomaDB. Please try later.");
            }
        })
    }
    
    neotomaCallback(result) {
        if (result.success == 0) {} else {
            this.points = [];
            for (let i = 0; i < result.data.length; i++) {
                const coordinate = {
                    east: result.data[i].Site.LongitudeEast,
                    north: result.data[i].Site.LatitudeNorth,
                    youngest: result.data[i].AgeYoungest,
                    oldest: result.data[i].AgeOldest
                };
                this.points.push(coordinate);
            }
            this.domPoints = this.svg.selectAll("circle").data(this.points).enter().append("circle").attr("cx", d => {
                return this.projection([d.east, d.north])[0];
            }).attr("cy", d => {
                return this.projection([d.east, d.north])[1];
            }).attr("r", "1.5px").style("opacity", 0.75).attr("fill", d => {
                return this.color(d.youngest);
            });
            $('#palaeo-loading').fadeOut(1000);
            const sliderElem = document.getElementById('range');
            sliderElem.removeAttribute('disabled');
        }
    }
}

function setupSlider() {
    const sliderElem = document.getElementById('range');
    const slider = noUiSlider.create(sliderElem, {
        start: [1, 10],
        margin: 1,
        connect: true,
        orientation: 'horizontal',
        behaviour: 'tap-drag',
        step: 1,
        range: {
            'min': 1,
            'max': 50
        },
        pips: {
            mode: 'values',
            values: [1, 5, 10, 15, 20, 30, 40, 50],
            density: 1,
            stepped: true
        }
    });
    sliderElem.setAttribute('disabled', "true");
    return slider;
}