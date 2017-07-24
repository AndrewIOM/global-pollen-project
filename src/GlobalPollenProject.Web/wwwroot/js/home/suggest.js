//Suggest species from reference collection
//Andrew Martin - 11/06/2016

$(document).ready(function () {
    $('#ref-collection-search').keyup(function () {
        var val = $.trim(this.value);
        if (val.length > 1) {
            var results = Suggest(val);
        } else {
            $('#suggestList').fadeOut();
        }
    })
});

//Query local Species API
function Suggest(searchTerm) {
    ajax = new XMLHttpRequest();
    ajax.onreadystatechange = function () {
        if (ajax.readyState == 4 || ajax.readyState == "complete") {
            var result = ajax.responseText;
            var resultJson = JSON.parse(result);
            var taxaList = document.getElementById('suggestList');
            taxaList.innerHTML = "";
            $('#suggestList').css('display', 'block');
            for (var i = 0; i < resultJson.length; i++) {
                var option = document.createElement('li');
                var link = document.createElement('a');
                option.appendChild(link);
                link.innerHTML = resultJson[i];
                link.href = '/Taxon/View/' + resultJson[i];
                link.addEventListener('click', function (e) {
                    var name = this.innerHTML;
                    $('#ref-collection-search').val(name);
                    $('#suggestList').fadeOut();
                });
                taxaList.appendChild(option);
            }
        }
    }
    ajax.open("GET", "/api/v1/backbone/search?rank=Species&latinName=" + searchTerm);
    ajax.send();
}