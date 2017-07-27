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
    
    // Adding a slide: request model
    self.isAddingSlide = ko.observable(false);
    self.rank = ko.observable("");
    self.family = ko.observable("");
    self.genus = ko.observable("");
    self.species = ko.observable("");
    self.author = ko.observable("");
    self.newSlideTaxonStatus = ko.observable(null);
    self.currentTaxon = ko.observable();
    self.collectionMethod = ko.observable();
    self.existingId = ko.observable();
    self.yearCollected = ko.observable();
    self.nameOfCollector = ko.observable();
    self.locality = ko.observable();
    self.district = ko.observable();
    self.country = ko.observable();
    self.region = ko.observable();
    self.yearPrepared = ko.observable();
    self.preperationMethod = ko.observable();
    self.mountingMaterial = ko.observable();

    // Slide Detail
    self.isViewingSlideDetail = ko.observable(false);
    self.slideDetail = ko.observable();
    self.addImage = ko.observable();
    self.focusImagePreview = null;

    self.isValidTaxonSearch = ko.computed(function() {
        if (self.rank() == "Family" && self.family().length > 0) return true;
        if (self.rank() == "Genus" && self.family().length > 0 && self.genus().length > 0) return true;
        if (self.rank() == "Species" && self.genus().length > 0 && self.species().length > 0) return true;
        return false;
    }, self);

    self.isValidAddSlideRequest = ko.computed(function() {
        if (self.rank() == "") return false;
        if (self.currentTaxon() == "") return false;
        if (self.collectionMethod() == "") return false;
        return true;
    }, self)

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
        $.ajax({ url: "/api/v1/collection?id=" + collection.Id, type: "GET" })
        .done(function (col) {
            self.activeCollection(col);
        })
    }

    self.addSlide = function() {
        self.isAddingSlide(true);
    }

    self.stopAddingSlide = function() {
        self.isAddingSlide(false);
    }

    self.submitAddSlide = function() {
        let request = {
            Collection: self.activeCollection().Id,
            ExistingId: self.existingId(),
            OriginalFamily: self.family(),
            OriginalGenus: self.genus(),
            OriginalSpecies: self.species(),
            OriginalAuthor: self.author(),
            ValidatedTaxonId: self.currentTaxon(),
            SamplingMethod: self.collectionMethod(),
            YearCollected: parseInt(self.yearCollected()),
            YearSlideMade: parseInt(self.yearPrepared()),
            LocationRegion: self.region(),
            LocationCountry: self.country(),
            PreperationMethod: self.preperationMethod(),
            MountingMaterial: self.mountingMaterial()
        };
        console.log(request);
        $.ajax({
            url: "/api/v1/collection/slide/add",
            type: "POST",
            data: JSON.stringify(request),
            dataType: "json",
            contentType: "application/json"
        })
        .done(function (data) {
            self.isAddingSlide(false);
            self.updateCurrentCollection();
        })
    }

    self.updateCurrentCollection = function() {
        $.ajax({ url: "/api/v1/collection?id=" + self.activeCollection().Id, type: "GET" })
        .done(function (col) {
            self.activeCollection(col);
        })
    }

    self.validateTaxon = function() {
        var query;
        if (self.rank() == "Family") {
            query = "rank=Family&family=" + self.family() + "&latinname=" + self.family();
        } else if (self.rank() == 'Genus') {
            query = "rank=Genus&family=" + self.family() + "&genus=" + self.genus() + "&latinname=" + self.genus();
        } else if (self.rank() == "Species") {
            query = "rank=Species&family=" + self.family() + "&genus=" + self.genus() + "&species=" + self.species() + "&latinname=" + self.genus() + " " + self.species() + "&authorship=" + self.author();
        }
        $.ajax({
            url: "/api/v1/backbone/trace?" + query,
            type: "GET"
        })
        .done(function(data) {
            if (data.length == 1 && data[0].TaxonomicStatus == "accepted") self.currentTaxon(data[0].Id);
            self.newSlideTaxonStatus(data);
        })
    }

    self.viewSlideDetail = function(slide) {
        self.slideDetail(slide);
        self.isViewingSlideDetail(true);
        self.focusImagePreview = new FocusImagePreview();
    }

    self.stopViewingSlide = function(slide) {
        self.isViewingSlideDetail(false);
    }
}

$(document).ready(function() {
  var vm = new DigitiseViewModel();
  vm.updateMyCollections();
  ko.applyBindings(vm);
});


// Helpers - Dropdown autocomplete
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
        var genus = document.getElementById('original-Genus').value;
        query += genus + " ";
    }
    query += entryBox.value;

    if (entryBox.value == "") {

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


function FocusImagePreview() {
    let self = this;
    self.canvas = null;
    self.image = null;
    self.ctx = null;

    self.create = function()
    {
        // Canvas
        canvas = document.getElementById('focusImagePreview');
        canvas.height = 300;
        canvas.width = 300;
        $('#focus-preview').show();
        image = new Image;
        ctx = canvas.getContext('2d');
        var firstImage = $('#focus-images img:first');
        image.src = firstImage.attr("src");
        self.redraw();
        $(window).resize(self.redraw());

        //Setup Slider
        slider = document.getElementById('focusSlider');
        noUiSlider.create(slider, {
            start: [1],
            step:1,
            tooltips:true,
            range: {
                'min': [1],
                'max': [5]
            }, orientation: 'vertical'
        });

        // Change focus level
        slider.noUiSlider.on('slide', function () {
            var value = slider.noUiSlider.get();
            var imageContainer = document.getElementById('focus-images');
            image.src = imageContainer.getElementsByTagName('img')[value-1].src;
            self.redraw();
        });
        $('#focus-add-button').removeClass('disabled');
    }

    self.addData = function(input) {
        self.dispose();
        if (input.files.length) {
            var validImages = 0;
            var imageContainer = document.getElementById('focus-images');
            for (var i = 0; i < input.files.length; i++) {
                if (/\.(jpe?g|png|gif)$/i.test(input.files[i].name)) {
                    validImages++;
                    var img = document.createElement('img');
                    img.src = window.URL.createObjectURL(input.files[i]);
                    imageContainer.appendChild(img);
                }
            }
            if (validImages == 5) {
                $('#focus-upload-error').text('');
                self.create();
            } else {
                $('#focus-upload-error').text('The image stack was not valid');
            }
        } else {
            $('#focus-upload-error').text("You didn't select any images");
        }
    }

    self.saveImage = function() {
        // Somehow submit image here...
    }

    self.dispose = function() {
        //Cleanup old focus image from dialog box
        $('#focus-add-button').addClass('disabled');
        $('#focus-upload-button').removeClass('disabled');
        $('#focus-upload-error').text('');
        $('#focus-preview').hide();
        document.getElementById('focusImagePreview').innerHTML = '';
        document.getElementById('focus-images').innerHTML = '';
    }

    self.redraw = function() {
        ctx.fillStyle = '#333333';
        ctx.fillRect(0, 0, canvas.width, canvas.height);

        var imgObj = new Image();
        imgObj.onload = function () {
            var renderHeight = imgObj.naturalHeight;
            var renderWidth = imgObj.naturalWidth;

            var ratio = imgObj.naturalWidth / imgObj.naturalHeight;
            if (ratio < 1) { //Portrait
                var scaling = 1;
                if (renderHeight > canvas.height) {
                    scaling = canvas.height / renderHeight;
                    renderHeight = canvas.height;
                }
                renderWidth = renderWidth * scaling;
            } else { //Landscape
                var scaling = 1;
                if (renderWidth > canvas.width) {
                    scaling = canvas.width / renderWidth;
                    renderWidth = canvas.width;
                }
                renderHeight = renderHeight * scaling;
            }

            var widthOffset = (canvas.width - renderWidth) / 2;
            var heightOffset = (canvas.height - renderHeight) / 2;
            ctx.drawImage(image, widthOffset, heightOffset, renderWidth, renderHeight);
        };
        imgObj.src = image.src;
    }


}

//Base Functions
function convertToDataURLviaCanvas(url, callback) {
    var img = new Image();
    img.crossOrigin = 'Anonymous';
    img.onload = function () {
        var canvas = document.createElement('CANVAS');
        var ctx = canvas.getContext('2d');
        var dataURL;
        canvas.height = this.height;
        canvas.width = this.width;
        ctx.drawImage(this, 0, 0);
        dataURL = canvas.toDataURL("image/png");
        callback(dataURL);
        canvas = null;
    };
    img.src = url;
}


// OLD

// function addImageToGrid(d, images) {
//     //Create elements for image
//     var li = document.createElement('li');
//     d.appendChild(li);
//     var div = document.createElement('div');
//     div.className = "img-container";
//     li.appendChild(div);
//     var a = document.createElement('a');
//     div.appendChild(a);

//     //Create URL holders for each image
//     function convertToBase64(urlHolder, image) {
//         convertToDataURLviaCanvas(image.src, function (base64Img) {
//             urlHolder.src = base64Img;
//         }, false)
//     }
//     for (var i = 0; i < images.length; i++) {
//         var urlHolder = document.createElement('img');
//         if (i != 2) urlHolder.hidden = 'hidden';
//         //urlHolder.src = images[i].src;
//         convertToBase64(urlHolder, images[i]);
//         a.appendChild(urlHolder);
//     }

//     //Create delete button
//     var del = document.createElement('span');
//     del.className = 'delete';
//     var icon = document.createElement('span');
//     icon.className = 'glyphicon glyphicon-trash';
//     del.appendChild(icon);
//     a.appendChild(del);
//     del.onclick = function () {
//         $(this).closest('li').remove();
//     };
// }
