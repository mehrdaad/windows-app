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

function setupVideoLazyLoading() {
    var videos = document.querySelectorAll(".wallabag-video");

    if (videos.length > 0)
        for (var video, l = 0; l < videos.length; l++) {
            video = videos[l];

            video.addEventListener("click", function (video) {
                var provider = this.getAttribute("data-provider");
                var videoId = this.getAttribute("data-video-id");

                var openMode = this.getAttribute("data-open-mode");

                if (openMode == "browser" || openMode == "app")
                    window.external.notify("video-" + openMode + "|" + provider + "|" + videoId);
                else {
                    this.setAttribute("data-transformed", "true");
                    if (provider == "youtube") {
                        this.innerHTML = "<div class='wallabag-video-container'><iframe type='text/html' src='https://www.youtube-nocookie.com/embed/" + videoId + "?rel=0&showinfo=0&color=white' frameborder='0' allowfullscreen></iframe></div>";
                    }
                    else if (provider == "vimeo") {
                        this.innerHTML = "<div class='wallabag-video-container'><iframe type='text/html' src='http://player.vimeo.com/video/" + videoId + "?portrait=0' frameborder='0' allowfullscreen></iframe></div>";
                    }
                    else
                        this.innerHTML = "<video src='" + videoId + "' preload controls></video>";
                }
            })
        }
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
        var x = 0;
        var y = 0;

        if (e.touches == null) {
            x = e.x;
            y = e.y;
        }
        else {
            x = e.touches[0].clientX;
            y = e.touches[0].clientY;
        }

        if (e.type === "click" && e.button !== 0) {
            return;
        }
        
        if (e.button === 2) {
            window.external.notify("RC|" + e.srcElement.href + "|" + x + "|" + y);
            return;
        }

        longpress = false;

        presstimer = setTimeout(function () {
            window.external.notify("LC|" + e.srcElement.href + "|" + x + "|" + y);
            longpress = true;
        }, 500);

        return false;
    };

    for (i = 1; i < nodes.length; i++) {
        nodes[i].addEventListener("mousedown", start);
        nodes[i].addEventListener("touchstart", start);
        nodes[i].addEventListener("mouseout", cancel);
        nodes[i].addEventListener("touchend", cancel);
        nodes[i].addEventListener("touchleave", cancel);
        nodes[i].addEventListener("touchcancel", cancel);
    }
}