// Custom Knockout bindings
ko.bindingHandlers.BSModal= {
    init: function (element, valueAccessor) {
        var value = valueAccessor();
        $(element).modal({ keyboard: false, show: ko.unwrap(value) });;
    },
    update: function (element, valueAccessor) {
         var value = valueAccessor();
         ko.unwrap(value) ? $(element).modal('show') : $(element).modal('hide');
    }
};

// View Model
function DigitiseViewModel(users, analyses) {
    var self = this;
    self.myCollections = ko.observableArray([]);

    // Creating Collection
    self.isCreatingCollection = ko.observable(false);
    self.newCollectionData = ko.observable({Name: "", Description: ""});

    // Editing Collection
    self.activeCollection = ko.observable(null);
    
    // Adding a slide
    self.isAddingSlide = ko.observable(false);
    self.newSlideData = ko.observable({});
    self.rank = ko.observable("Species");
    self.family = ko.observable();
    self.genus = ko.observable();
    self.species = ko.observable();
    self.author = ko.observable();
    self.newSlideTaxonStatus = ko.observable(null);

    self.updateMyCollections = function() {
        $.ajax({
            url: "/api/v1/collection/list",
            cache: false,
            success: function(serverCols)
            {
                self.myCollections(serverCols);
            }
        });
    }

    self.startCreatingCollection = function() {
        self.newCollectionData({Name: "", Description: ""});
        self.isCreatingCollection(true);
    }

    self.startCollection = function() {
        console.log(self.newCollectionData())
        $.ajax({
            url: "/api/v1/collection/start",
            type: "POST",
            data: JSON.stringify(self.newCollectionData()),
            dataType: "json",
            contentType: "application/json"
        })
        .done(function (data) {
            self.isCreatingCollection(false);
            self.newCollectionData({Name: "", Description: ""});
            self.updateMyCollections();
        })
    }

    self.viewCollection = function(collection) {
        // get collection detail
        // set as the active collection
        self.activeCollection(collection);
    }

    self.addSlide = function() {
        self.isAddingSlide(true);
        self.addSlide({});
    }

    self.validateTaxon = function() {
        if (self.newSlideData == null) return;

        var query;
        if (self.rank() == "Family") {
            query = "rank=Family&family=" + self.family() + "&latinname=" + self.family();
        } else if (self.rank() == 'Genus') {
            query = "rank=Species&family=" + self.family() + "&genus=" + self.genus() + "&latinname=" + self.genus();
        } else if (self.rank() == "Species") {
            query = "rank=Species&family=" + self.family() + "&genus=" + self.genus() + "&species=" + self.species() + "&latinname=" + self.genus() + " " + self.species() + "&authorship=" + self.author();
        }

        $.ajax({
            url: "/api/v1/backbone/trace?" + query,
            type: "GET"
        })
        .done(function(data) {
            if (data.length == 1 && data[0].TaxonomicStatus == "accepted") self.newSlideData().Taxon = data[0].Id;
            self.newSlideTaxonStatus(data);
        })
    }
}

$(document).ready(function() {
  var vm = new DigitiseViewModel();
  vm.updateMyCollections();
  ko.applyBindings(vm);
});


// Helpers
//Ensure API call sent only when typing completed
var typingTimer;
var doneTypingInterval = 100;

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
    if (rank == 'Species') {
        //Combine genus and species for canonical name
        var genus = document.getElementById('original-genus').value;
        query += genus + " ";
    }
    query += entryBox.value;

    if (entryBox.value == "") {

    } else {
        var request = "/api/v1/backbone/search?rank=" + rank + "&latinName=" + query;
        ajaxHelper(request, 'GET', 'json').done(function (data) {
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

                // if (rank == 'Genus') {
                //     var familySpan = document.createElement('span');
                //     familySpan.innerHTML = (data[i].parentLatinName + ',' + matchCount);
                //     familySpan.className = 'family-name';
                //     link.appendChild(familySpan);
                // }
                link.addEventListener('click', function (e) {
                    var name = this.innerHTML;
                    if (rank == 'Species') {
                        var species = name.split(' ')[1];
                        $('#original-' + rank).val(species);
                    } else {
                        $('#original-' + rank).val(name);
                    }

                    //Autofill family name
                    // var familySpan = this.getElementsByClassName("family-name");
                    // if (familySpan.length > 0) {
                    //     var family = this.getElementsByClassName("family-name")[0].innerHTML.split(',')[0];
                    //     var matchCount = this.getElementsByClassName("family-name")[0].innerHTML.split(',')[1];
                    //     if (matchCount == 1) {
                    //         $('#Family').val(family);
                    //     };
                    // }
                    $('#' + rank + 'List').fadeOut();
                });
                list.appendChild(option);
            }
            $('.family-name').css('display', 'none');
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

//Base Functions
function ajaxHelper(uri, method, dataType, data) {
    //self.error('');
    return $.ajax({
        type: method,
        url: uri,
        dataType: dataType,
        contentType: 'application/json',
        data: data ? JSON.stringify(data) : null
    }).fail(function (jqXhr, textStatus, errorThrown) {
        console.log(errorThrown);
        //self.error(errorThrown);
    });
}