function updateReadingProgress() {
    var root = document.getElementsByTagName("html")[0];
    var top = document.body.scrollTop;
    var scrollPercentage = 100 * top / (root.scrollHeight - root.clientHeight);

    if (root.scrollHeight - top === root.clientHeight || scrollPercentage > 100)
        window.external.notify("finishedReading");
    window.external.notify("S|" + scrollPercentage.toString());
}

function changeHtmlAttributes(color, font, fontsize, textalign) {
    document.getElementsByTagName("html")[0].setAttribute("data-color", color);
    document.getElementsByTagName("html")[0].setAttribute("data-font", font);

    document.body.style.fontSize = parseFloat(fontsize) + "px";
    document.body.style.textAlign = textalign;
}

// Copied and modified from: https://stackoverflow.com/a/13433551
function updateTagsElement(newTags) {
    var Obj = document.getElementById("wallabag-tag-list");
    Obj.outerHTML = newTags;
}

function getSelectionText() {
    var text = "";
    if (window.getSelection) {
        text = window.getSelection().toString();
    } else if (document.selection && document.selection.type != "Control") {
        text = document.selection.createRange().text;
    }
    return text;
}

function rightClickInitialize() {
    var nodes = document.getElementsByTagName("a");
    var longpress = false;
    var presstimer = null;
    var longtarget = null;

    var cancel = function (e) {
        if (presstimer !== null) {
            clearTimeout(presstimer);
            presstimer = null;
        }
    };

    var start = function (e) {
        if (e.type === "click" && e.button !== 0) {
            return;
        }
        if (e.button === 2) {
            window.external.notify("RC|" + e.srcElement.href);
            return;
        }

        longpress = false;

        presstimer = setTimeout(function () {
            window.external.notify("LC|" + e.srcElement.href);
            longpress = true;
        }, 500);

        return false;
    };

    for (i = 0; i < nodes.length; i++) {
        nodes[i].addEventListener("mousedown", start);
        nodes[i].addEventListener("touchstart", start);
        nodes[i].addEventListener("mouseout", cancel);
        nodes[i].addEventListener("touchend", cancel);
        nodes[i].addEventListener("touchleave", cancel);
        nodes[i].addEventListener("touchcancel", cancel);
    }
}