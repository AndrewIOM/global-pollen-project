﻿function uploadFile() {
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

    ajax.open("POST", "/Grain/Add");
    ajax.send(formData);
}