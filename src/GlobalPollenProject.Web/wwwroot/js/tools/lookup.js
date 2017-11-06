function BotanicalLookupToolViewModel() {
    let self = this;
    self.rank = ko.observable("");
    self.family = ko.observable("");
    self.genus = ko.observable("");
    self.species = ko.observable("");
    self.author = ko.observable("");
    self.newSlideTaxonStatus = ko.observable(null);
    self.currentTaxon = ko.observable();

    self.rank.subscribe(function (rank) {
        if (rank == "Family") {
            self.genus("");
            self.species("");
            self.author("");
        } else if (rank == "Genus") {
            self.species("");
            self.author("");
        }
    });

    self.isValidTaxonSearch = ko.computed(function () {
        if (self.rank() == "Family" && self.family().length > 0) return true;
        if (self.rank() == "Genus" && self.genus().length > 0) return true;
        if (self.rank() == "Species" && self.genus().length > 0 && self.species().length > 0) return true;
        return false;
    }, self);

    self.validateTaxon = function () {
        var query;
        if (self.rank() == "Family") {
            query = "rank=Family&family=" + self.family() + "&latinname=" + self.family();
        } else if (self.rank() == 'Genus') {
            query = "rank=Genus&family=" + self.family() + "&genus=" + self.genus() + "&latinname=" + self.genus();
        } else if (self.rank() == "Species") {
            query = "rank=Species&family=" + self.family() + "&genus=" + self.genus() + "&species=" + self.species() + "&latinname=" + self.genus() + " " + self.species() + "&authorship=" + encodeURIComponent(self.author());
        }
        $.ajax({
                url: "/api/v1/backbone/trace?" + query,
                type: "GET"
            })
            .done(function (data) {
                if (data.length == 1 && data[0].TaxonomicStatus == "accepted") self.currentTaxon(data[0].Id);
                self.newSlideTaxonStatus(data);
            })
    }

    self.capitaliseFirstLetter = function (element) {
        $(element).val($(element).val().charAt(0).toUpperCase() + $(element).val().slice(1));
    }

    self.getTaxonIdIfValid = function() {
        if (self.currentTaxon() != null) return self.currentTaxon();
        return null;
    }
}

// Helpers
var typingTimer;
var doneTypingInterval = 100;

function capitaliseFirstLetter(string) {
    return string.charAt(0).toUpperCase() + string.slice(1);
}

function suggest(entryBox, rank) {
    clearTimeout(typingTimer);
    if (entryBox.value) {
        typingTimer = setTimeout(function () {
            updateList(entryBox, rank);
        }, doneTypingInterval);
    }
};

//Update suggestion list when timeout complete
function updateList(entryBox, rank) {
    var query = '';
    var value = entryBox.value;
    if (rank == "Family" || rank == "Genus") {
        value = capitaliseFirstLetter(value);
    }

    if (rank == 'Species') {
        //Combine genus and species for canonical name
        var genus = document.getElementById('original-Genus').value;
        query += genus + " ";
    }
    query += value;

    if (value == "") {

    } else {
        var request = "/api/v1/backbone/search?rank=" + rank + "&latinName=" + query;
        $.ajax({
            url: request,
            type: "GET"
        }).done(function (data) {
            var list = document.getElementById(rank + 'List');
            $('#' + rank + 'List').css('display', 'block');
            list.innerHTML = "";
            for (var i = 0; i < data.length; i++) {
                if (i > 10) continue;
                var option = document.createElement('li');
                var link = document.createElement('a');
                option.appendChild(link);
                link.innerHTML = data[i];

                var matchCount = 0;
                for (var j = 0; j < data.length; j++) {
                    if (data[j].latinName == data[i]) {
                        matchCount++;
                    }
                };
                link.addEventListener('click', function (e) {
                    var name = this.innerHTML;
                    if (rank == 'Species') {
                        $('#original-Species').val(name.split(' ')[1]).change();
                        $('#original-Genus').val(name.split(' ')[0]).change();
                    } else if (rank == 'Genus') {
                        $('#original-Genus').val(name).change();
                    } else if (rank == 'Family') {
                        $('#original-Family').val(name).change();
                    }
                    $('#' + rank + 'List').fadeOut();
                });
                list.appendChild(option);
            }
        });
    }
}

function disable(rank) {
    var element;
    if (rank == 'Family') element = 'FamilyList';
    if (rank == 'Genus') element = 'GenusList';
    if (rank == 'Species') element = 'SpeciesList';

    setTimeout(func, 100);

    function func() {
        $('#' + element).fadeOut();
    }
}