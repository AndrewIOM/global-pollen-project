//Suggest species from reference collection
import * as $ from "jquery"

$(document).ready(function () {
    $('#ref-collection-search').keyup(function () {
        var val = (<HTMLInputElement>this).value;
        if (val.length > 0) {
            var results = Suggest(val);
        } else {
            $('#suggestList').fadeOut();
        }
    })
});

function capitaliseFirstLetter(string) {
    return string.charAt(0).toUpperCase() + string.slice(1);
}

//Query local Species API
function Suggest(searchTerm) {
    searchTerm = capitaliseFirstLetter(searchTerm);
    let ajax = new XMLHttpRequest();
    ajax.onreadystatechange = function () {
        if (ajax.readyState == 4) {
            var result = ajax.responseText;
            var resultJson = JSON.parse(result);
            var taxaList = document.getElementById('suggestList');
            taxaList.innerHTML = "";
            $('#suggestList').css('display', 'block');
            for (var i = 0; i < resultJson.length; i++) {
                var linkUrl = "";
                if (resultJson[i].rank == "Family") { linkUrl = "/Taxon/" + resultJson[i].latinName; }
                else if (resultJson[i].rank == "Genus") { linkUrl = "/Taxon/" + resultJson[i].heirarchy[0] + "/" + resultJson[i].heirarchy[1] }
                else if (resultJson[i].rank == "Species") { linkUrl = "/Taxon/" + resultJson[i].heirarchy[0] + "/" + resultJson[i].heirarchy[1] + "/" + (resultJson[i].heirarchy[2].split(' ')[1]); }
                var option = document.createElement('li');            
                var headerDiv = document.createElement('div');
                headerDiv.className = "taxon-name";
                option.appendChild(headerDiv);
                var link = document.createElement('a');
                headerDiv.appendChild(link);
                var rank = document.createElement('span');
                rank.className = "taxon-rank";
                rank.innerHTML = resultJson[i].rank;
                var heirarchy = document.createElement('div');
                heirarchy.className = "heirarchy";
                heirarchy.innerHTML = resultJson[i].heirarchy.join(" > ");
                headerDiv.appendChild(rank);
                option.appendChild(heirarchy);
                link.innerHTML = resultJson[i].latinName;
                link.href = linkUrl;
                link.addEventListener('click', function (e) {
                    var name = this.innerHTML;
                    $('#ref-collection-search').val(name);
                    $('#suggestList').fadeOut();
                });
                taxaList.appendChild(option);
            }
        }
    }
    ajax.open("GET", "/api/v1/taxon/search?pageSize=10&name=" + searchTerm);
    ajax.send();
}