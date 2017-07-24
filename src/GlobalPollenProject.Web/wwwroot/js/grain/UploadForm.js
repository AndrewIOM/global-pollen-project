// Google Maps location picker
window.onload = function () {
    console.log("loading google maps");
    var latlng = new google.maps.LatLng(51.4975941, -0.0803232);
    var map = new google.maps.Map(document.getElementById('map'), {
        center: latlng,
        zoom: 5,
        mapTypeId: google.maps.MapTypeId.TERRAIN
    });

    var marker;
    function placeMarker(location) {
        if (marker) {
            marker.setPosition(location);
        } else {
            marker = new google.maps.Marker({
                position: location,
                map: map,
                title: "Pollen Sample Location",
                draggable: true
            });
            google.maps.event.addListener(marker, 'dragend', function (event) {
                updateLocationFormFields(event.latLng);
            });
        }
    }

    google.maps.event.addListener(map, 'click', function (event) {
        placeMarker(event.latLng);
        updateLocationFormFields(event.latLng);
    });

    function updateLocationFormFields(latLng) {
        var lat = latLng.lat().toFixed(4);
        var lon = latLng.lng().toFixed(4);
        document.getElementById('LatitudeDD').value = lat;
        document.getElementById('LongitudeDD').value = lon;
    }
};

// Image Upload widget
var canvasSize = 300;
var cropSize = canvasSize * 0.80;
var dkrm = null;

function loadDarkroom(imgId) {
    console.log("reloading...");
    dkrm = new Darkroom('#' + imgId, {
        // canvas options
        minWidth: 100,
        minHeight: 100,
        maxWidth: 600,
        maxHeight: 300,
        ratio: 4 / 3,
        backgroundColor: '#000',

        plugins: {
            history: false,
            save: false
        },
        init: function () {
            var cropPlugin = this.getPlugin('crop');
            var xoffset = (canvasSize - cropSize) / 2;
            cropPlugin.selectZone(xoffset, xoffset, cropSize, cropSize);
            cropPlugin.requireFocus();
        }
    });
}
function scaleImage(image) {
    var width = image.getWidth();
    var height = image.getHeight();
    var scaleMin = 1;
    var scaleMax = 1;
    var scaleX = 1;
    var scaleY = 1;

    if (null !== dkrm.options.maxWidth && dkrm.options.maxWidth < width) {
        scaleX = dkrm.options.maxWidth / width;
    }
    if (null !== dkrm.options.maxHeight && dkrm.options.maxHeight < height) {
        scaleY = dkrm.options.maxHeight / height;
    }
    scaleMin = Math.min(scaleX, scaleY);
    scaleX = 1;
    scaleY = 1;
    if (null !== dkrm.options.minWidth && dkrm.options.minWidth > width) {
        scaleX = dkrm.options.minWidth / width;
    }
    if (null !== dkrm.options.minHeight && dkrm.options.minHeight > height) {
        scaleY = dkrm.options.minHeight / height;
    }
    scaleMax = Math.max(scaleX, scaleY);
    var scale = scaleMax * scaleMin; // one should be equals to 1

    image.setScaleX(scale);
    image.setScaleY(scale);

    return image;
}
function resetImage(image) {
    if (dkrm) {
        image = scaleImage(image);
        dkrm.canvas.remove(dkrm.image);
        dkrm.canvas.add(image);
        dkrm.canvas.centerObject(image);
        image.setCoords();
        image.sendToBack();
    }
}
function loadImage(event) {
    var dataURI = event.target.result;
    if (dkrm) {
        fabric.Image.fromURL(dataURI, function (ximg) { resetImage(ximg); },
          {
              // options to make the image static
              selectable: false,
              evented: false,
              lockMovementX: true,
              lockMovementY: true,
              lockRotation: true,
              lockScalingX: true,
              lockScalingY: true,
              lockUniScaling: true,
              hasControls: false,
              hasBorders: false
          }
        );
    }
}
function readerError(event) {
    console.error("FileReader failed: Code " + event.target.error.code);
}

function doClick() {
    var el = document.getElementById("fileElem");
    if (el) {
        el.click();
    }
}
function handleImage(files) {
    var d = document.getElementById("img-container");
    if (files.length = 1) {
        var reader = new FileReader();
        reader.onload = loadImage;
        reader.onerror = readerError;
        reader.readAsDataURL(files[0]);
    }
}
function handleFiles(input) {
    var d = document.getElementById("image-thumbnails");
    if (!input.files.length) {
        d.innerHTML = "<p>None</p>";
    } else {
        d.innerHTML = "";
        for (var i = 0; i < input.files.length && i < 4; i++) {
            var div = document.createElement('div');
            div.style.display = 'inline';
            div.className = "image-thumbnail col-md-6";
            d.appendChild(div);
            var img = document.createElement("img");
            img.src = window.URL.createObjectURL(input.files[i]);;
            img.id = "image-upload-" + (i + 1);
            img.style.height = '12em';
            div.appendChild(img);
            loadDarkroom(img.id);
        }
    }
}

// Validate form and send to server
function uploadFile() {
    $('#submit').prop('disabled', true);
    $('#submit').addClass('disabled');

    //Reset base64 images before submission
    var image1 = document.getElementById('ImageOne');
    var image2 = document.getElementById('ImageTwo');
    var image3 = document.getElementById('ImageThree');
    var image4 = document.getElementById('ImageFour');
    image1.value = '';
    image2.value = '';
    image3.value = '';
    image4.value = '';

    //Save image state to Url from DarkroomJS instances
    var instances = document.getElementsByClassName('darkroom-source-container');
    var imgs = [];
    for (var i = 0; i < instances.length; i++) {
        imgs.push(instances[i].getElementsByClassName('lower-canvas')[0]);
    }

    var b64;
    if (imgs.length >= 1) {
        b64 = (imgs[0].toDataURL('image/png')),
        image1.value = b64.slice(b64.indexOf(',') + 1);
    }
    if (imgs.length >= 2) {
        b64 = (imgs[1].toDataURL('image/png')),
        image2.value = b64.slice(b64.indexOf(',') + 1);
    }
    if (imgs.length >= 3) {
        b64 = (imgs[2].toDataURL('image/png')),
        image3.value = b64.slice(b64.indexOf(',') + 1);
    }
    if (imgs.length >= 4) {
        b64 = (imgs[3].toDataURL('image/png')),
        image4.value = b64.slice(b64.indexOf(',') + 1);
    }

    //Get form data
    var form = document.getElementById('addGrainForm');
    var formData = new FormData(form);

    //Progress Bar
    var progbar = form.getElementsByClassName('progress-bar')[0];
    var progDiv = form.getElementsByClassName('progress')[0];
    var submit = document.getElementById('submit');
    progbar.className = 'progress-bar progress-bar-striped active';
    submit.className = 'btn btn-primary disabled';
    progDiv.setAttribute('style', 'display:""');

    //Ajax Request
    ajax = new XMLHttpRequest();
    (ajax.upload || ajax).addEventListener('progress', function (e) {
        var done = e.position || e.loaded
        var total = e.totalSize || e.total;
        var progress = Math.round(done / total * 100) + '%';
        progbar.setAttribute('style', 'width:' + progress);
        progbar.innerHTML = progress;
    });

    ajax.onreadystatechange = function () {
        if (ajax.readyState == 4 || ajax.readyState == "complete") {
            if (ajax.status == 200) {
                progbar.className = 'progress-bar progress-bar-success progress-bar-striped active';
                location.href = "/Grain/Index";
            }
            if (ajax.status == 400 || ajax.status == 500) {
                var result = ajax.responseText;
                var resultJson = JSON.parse(result);
                console.log(resultJson);
                progbar.className = 'progress-bar progress-bar-danger progress-bar-striped';
                submit.className = 'btn btn-primary';
                var errorBox = document.getElementById('validation-errors-box');
                $('#validation-errors-box').css('display', '');
                var newContent = "";
                $.each(resultJson, function (k, v) {
                    newContent = newContent + '<p><span class="glyphicon glyphicon-exclamation-sign" aria-hidden="true"></span><span class="sr-only">Error:</span> ' + v[0] + '</p>';
                });
                errorBox.innerHTML = newContent;
                $("html, body").animate({ scrollTop: 0 }, "slow");
                $('#submit').prop('disabled', false);
                $('#submit').removeClass('disabled');
                progDiv.setAttribute('style', 'display:none');
            }
        }
    }

    ajax.open("POST", "/Identify/Upload");
    ajax.send(formData);
}