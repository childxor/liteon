// var wrapper = document.getElementById("signature-pad");
var wrapper_add = document.getElementById("add-signature");
var clearButton_add = wrapper_add.querySelector("[data-action=clear]");
// var changeColorButton = wrapper.querySelector("[data-action=change-color]");
var undoButton_add = wrapper_add.querySelector("[data-action=undo]");
var okButton_add = wrapper_add.querySelector("[data-action=ok]");
// var savePNGButton = wrapper.querySelector("[data-action=save-png]");
// var saveJPGButton = wrapper.querySelector("[data-action=save-jpg]");
// var saveSVGButton = wrapper.querySelector("[data-action=save-svg]");
var canvas_add = wrapper_add.querySelector("canvas");
var signaturePad_add = new SignaturePad(canvas_add, {
    // It's Necessary to use an opaque color when saving image as JPEG;
    // this option can be omitted if only saving as PNG or SVG
    backgroundColor: 'rgb(255, 255, 255,0)'
});

// Adjust canvas coordinate space taking into account pixel ratio,
// to make it look crisp on mobile devices.
// This also causes canvas to be cleared.
function resizeCanvas_add() {
    // When zoomed out to less than 100%, for some very strange reason,
    // some browsers report devicePixelRatio as less than 1
    // and only part of the canvas is cleared then.
    var ratio = Math.max(window.devicePixelRatio || 1, 1);

    // This part causes the canvas to be cleared
    canvas_add.width = canvas_add.offsetWidth * ratio;
    canvas_add.height = canvas_add.offsetHeight * ratio;
    canvas_add.getContext("2d").scale(ratio, ratio);

    // This library does not listen for canvas changes, so after the canvas is automatically
    // cleared by the browser, SignaturePad#isEmpty might still return false, even though the
    // canvas looks empty, because the internal data of this library wasn't cleared. To make sure
    // that the state of this library is consistent with visual state of the canvas, you
    // have to clear it manually.
    signaturePad_add.clear();
}

// On mobile devices it might make more sense to listen to orientation change,
// rather than window resize events.
window.onresize = resizeCanvas_add;
resizeCanvas_add();

/*
 function download(dataURL, filename) {
 if (navigator.userAgent.indexOf("Safari") > -1 && navigator.userAgent.indexOf("Chrome") === -1) {
 window.open(dataURL);
 } else {
 var blob = dataURLToBlob(dataURL);
 var url = window.URL.createObjectURL(blob);
 
 var a = document.createElement("a");
 a.style = "display: none";
 a.href = url;
 a.download = filename;
 
 document.body.appendChild(a);
 a.click();
 
 window.URL.revokeObjectURL(url);
 }
 }
 */

clearButton_add.addEventListener("click", function (event) {
    signaturePad_add.clear();
    $('#add-sign').val("");
});

undoButton_add.addEventListener("click", function (event) {
    var data = signaturePad_add.toData();

    if (data) {
        data.pop(); // remove the last dot or line
        signaturePad_add.fromData(data);
        $('#add-sign').val(data);
    }
});

okButton_add.addEventListener("click", function (event) {
    var data = signaturePad_add.toDataURL();
    if (data) {
        console.log(data);
        $('#add-sign').val(data);
    }
});

// var wrapper = document.getElementById("signature-pad");
var wrapper_edit = document.getElementById("edit-signature");
var clearButton_edit = wrapper_edit.querySelector("[data-action=clear]");
// var changeColorButton = wrapper.querySelector("[data-action=change-color]");
var undoButton_edit = wrapper_edit.querySelector("[data-action=undo]");
var okButton_edit = wrapper_edit.querySelector("[data-action=ok]");
// var savePNGButton = wrapper.querySelector("[data-action=save-png]");
// var saveJPGButton = wrapper.querySelector("[data-action=save-jpg]");
// var saveSVGButton = wrapper.querySelector("[data-action=save-svg]");
var canvas_edit = wrapper_edit.querySelector("canvas");
var signaturePad_edit = new SignaturePad(canvas_edit, {
    // It's Necessary to use an opaque color when saving image as JPEG;
    // this option can be omitted if only saving as PNG or SVG
    backgroundColor: 'rgb(255, 255, 255)'
});

// Adjust canvas coordinate space taking into account pixel ratio,
// to make it look crisp on mobile devices.
// This also causes canvas to be cleared.
function resizeCanvas_edit() {
    // When zoomed out to less than 100%, for some very strange reason,
    // some browsers report devicePixelRatio as less than 1
    // and only part of the canvas is cleared then.
    var ratio = Math.max(window.devicePixelRatio || 1, 1);

    // This part causes the canvas to be cleared
    canvas_edit.width = canvas_edit.offsetWidth * ratio;
    canvas_edit.height = canvas_edit.offsetHeight * ratio;
    canvas_edit.getContext("2d").scale(ratio, ratio);

    // This library does not listen for canvas changes, so after the canvas is automatically
    // cleared by the browser, SignaturePad#isEmpty might still return false, even though the
    // canvas looks empty, because the internal data of this library wasn't cleared. To make sure
    // that the state of this library is consistent with visual state of the canvas, you
    // have to clear it manually.
    // signaturePad_edit.clear();
}

// On mobile devices it might make more sense to listen to orientation change,
// rather than window resize events.
window.onresize = resizeCanvas_edit;
resizeCanvas_edit();

/*
 function download(dataURL, filename) {
 if (navigator.userAgent.indexOf("Safari") > -1 && navigator.userAgent.indexOf("Chrome") === -1) {
 window.open(dataURL);
 } else {
 var blob = dataURLToBlob(dataURL);
 var url = window.URL.createObjectURL(blob);
 
 var a = document.createElement("a");
 a.style = "display: none";
 a.href = url;
 a.download = filename;
 
 document.body.appendChild(a);
 a.click();
 
 window.URL.revokeObjectURL(url);
 }
 }
 */
clearButton_edit.addEventListener("click", function (event) {
    signaturePad_edit.clear();
    $('#edit-sign').val("");
});

undoButton_edit.addEventListener("click", function (event) {
    var data = signaturePad_edit.toData();

    if (data) {
        data.pop(); // remove the last dot or line
        signaturePad_edit.fromData(data);
        $('#edit-sign').val(data);
    }
});

okButton_edit.addEventListener("click", function (event) {
    var data = signaturePad_edit.toDataURL();
    if (data) {
        console.log(data);
        $('#edit-sign').val(data);
    }
});

/*
 changeColorButton.addEventListener("click", function (event) {
 var r = Math.round(Math.random() * 255);
 var g = Math.round(Math.random() * 255);
 var b = Math.round(Math.random() * 255);
 var color = "rgb(" + r + "," + g + "," + b +")";
 
 signaturePad.penColor = color;
 });
 */
/*
 savePNGButton.addEventListener("click", function (event) {
 if (signaturePad.isEmpty()) {
 alert("Please provide a signature first.");
 } else {
 var dataURL = signaturePad.toDataURL();
 download(dataURL, "signature.png");
 }
 });
 */
/*
 saveJPGButton.addEventListener("click", function (event) {
 if (signaturePad.isEmpty()) {
 alert("Please provide a signature first.");
 } else {
 var dataURL = signaturePad.toDataURL("image/jpeg");
 download(dataURL, "signature.jpg");
 }
 });
 */
/*
 saveSVGButton.addEventListener("click", function (event) {
 if (signaturePad.isEmpty()) {
 alert("Please provide a signature first.");
 } else {
 var dataURL = signaturePad.toDataURL('image/svg+xml');
 download(dataURL, "signature.svg");
 }
 });
 */

// One could simply use Canvas#toBlob method instead, but it's just to show
// that it can be done using result of SignaturePad#toDataURL.
function dataURLToBlob(dataURL) {
    // Code taken from https://github.com/ebidel/filer.js
    var parts = dataURL.split(';base64,');
    var contentType = parts[0].split(":")[1];
    var raw = window.atob(parts[1]);
    var rawLength = raw.length;
    var uInt8Array = new Uint8Array(rawLength);

    for (var i = 0; i < rawLength; ++i) {
        uInt8Array[i] = raw.charCodeAt(i);
    }

    return new Blob([uInt8Array], {type: contentType});
}
