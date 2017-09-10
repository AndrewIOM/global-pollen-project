//Suggest species from reference collection
//Andrew Martin - 11/06/2016

$(document).ready(function () {
    $('#ref-collection-search').keyup(function () {
        var val = $.trim(this.value);
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
    ajax = new XMLHttpRequest();
    ajax.onreadystatechange = function () {
        if (ajax.readyState == 4 || ajax.readyState == "complete") {
            var result = ajax.responseText;
            var resultJson = JSON.parse(result);
            var taxaList = document.getElementById('suggestList');
            taxaList.innerHTML = "";
            $('#suggestList').css('display', 'block');
            for (var i = 0; i < resultJson.length; i++) {
                var linkUrl = "";
                if (resultJson[i].Rank == "Family") { linkUrl = "/Taxon/" + resultJson[i].LatinName; }
                else if (resultJson[i].Rank == "Genus") { linkUrl = "/Taxon/" + resultJson[i].Heirarchy[0] + "/" + resultJson[i].Heirarchy[1] }
                else if (resultJson[i].Rank == "Species") { linkUrl = "/Taxon/" + resultJson[i].Heirarchy[0] + "/" + resultJson[i].Heirarchy[1] + "/" + (resultJson[i].Heirarchy[2].split(' ')[1]); }
                var option = document.createElement('li');            
                var headerDiv = document.createElement('div');
                headerDiv.className = "taxon-name";
                option.appendChild(headerDiv);
                var link = document.createElement('a');
                headerDiv.appendChild(link);
                var rank = document.createElement('span');
                rank.className = "taxon-rank";
                rank.innerHTML = resultJson[i].Rank;
                var heirarchy = document.createElement('div');
                heirarchy.className = "heirarchy";
                heirarchy.innerHTML = resultJson[i].Heirarchy.join(" > ");
                headerDiv.appendChild(rank);
                option.appendChild(heirarchy);
                link.innerHTML = resultJson[i].LatinName;
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