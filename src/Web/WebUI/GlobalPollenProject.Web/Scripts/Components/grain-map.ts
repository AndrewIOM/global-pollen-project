import * as d3 from 'd3'
import * as $ from 'jquery'
import * as topojson from 'topojson-client'

type Grain = {
    Id: string
    Latitude: number
    Longitude: number
};

export function activate(container: HTMLElement) {
    let width = $('#locations-map').width();
    let height = width * 0.5;

    let projection = d3.geoEquirectangular()
        .scale((width + 1) / 2 / Math.PI)
        .translate([width / 2, height * 0.5])
        .precision(.1);

    let svg = d3.select("#locations-map").append("svg")
                .attr("viewBox", "0 0 " + width + " " + height )
                .attr("preserveAspectRatio", "xMinYMin");

    let path = d3.geoPath()
        .projection(projection);
    let g = svg.append("g").attr('class','map');

    let topo = d3.json<TopoJSON.Topology>('/geojson/world110.json');
    let data = d3.json<Grain[]>("/api/v1/grain/location");

    topo.then((tj) => {

        let geojson = topojson.feature(tj, tj.objects.countries);
        if (geojson.type != "FeatureCollection" ) {
            throw "World topology not in correct format";
        }
        g.selectAll("path")
            .data(geojson.features)
            .enter()
            .append("path")
            .attr("d", path);
    });

    data.then( data => {
        let unidentifiedNumber = document.getElementById('unidentified-count');
        unidentifiedNumber.innerHTML = '0';
        let unidentifiedCounter = 0;

        let circles = svg.selectAll("circle")
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
            .ease(d3.easeLinear)
            .attr('opacity', '1');
    });

}