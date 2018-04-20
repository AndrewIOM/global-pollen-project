import * as d3 from 'd3'
import * as $ from 'jquery'
import * as topojson from 'topojson'
import { GeometryCollection } from "topojson-specification";

type Grain = {
    Id: string
    Latitude: number
    Longitude: number
};

export function activate(container: HTMLElement) {
    var width = $('#locations-map').width();
    var height = width * 0.5;

    var projection = d3.geoEquirectangular()
        .scale((width + 1) / 2 / Math.PI)
        .translate([width / 2, height * 0.5])
        .precision(.1);

    var svg = d3.select("#locations-map").append("svg")
        .attr("width", width)
        .attr("height", height);
    var path = d3.geoPath()
        .projection(projection);
    var g = svg.append("g").attr('class','map');

    d3.json('/geojson/world110.json'), (error, t) => {
        let geojson:any = topojson.feature(t, t.objects.countries)
        g.selectAll("path")
            .data(geojson.features)
            .enter()
            .append("path")
            .attr("d", path);
    };

    d3.json("/api/v1/grain/location"), function (error, data:Grain[]) {
        var unidentifiedNumber = document.getElementById('unidentified-count');
        unidentifiedNumber.innerHTML = '0';
        var unidentifiedCounter = 0;

        var circles = svg.selectAll("circle")
            .data(data).enter()
            .append("svg:a")
            .attr("xlink:href", (d) => { return '/Identify/' + d.Id; })
            .append("circle")
            .attr('opacity', 0)
            .attr("cx", d => { return projection([d.Longitude, d.Latitude])[0]; })
            .attr("cy", function (d) { return projection([d.Longitude, d.Latitude])[1]; })
            .attr("r", 4)
            .attr('stroke', '#83296F')
            .attr('stroke-width', 2)
            .attr("fill", 'rgba(184, 0, 136, 0.1)')
            .on('mouseenter', function () {
                d3.select(this)
                  .transition()
                  .attr('r', 8);
            })
            .on('mouseleave', function () {
                d3.select(this)
                    .transition()
                    .attr('r', 4);
            })
            .transition()
            .duration(500) 
            .each((d,i) => { 
                unidentifiedCounter++;
                unidentifiedNumber.innerHTML = unidentifiedCounter.toString();
            })
            .delay((d,i) => { return 2000 + (i / data.length * 1500); })
            //.ease("variable")
            .attr('opacity', '1');
    };
}