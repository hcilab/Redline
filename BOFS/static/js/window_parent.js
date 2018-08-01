var wChild;
var needToOpen = true;
var parentUrlSet = false;

// Create cross-browser event handler (https://davidwalsh.name/window-iframe)
var eventMethod = window.addEventListener ? "addEventListener" : "attachEvent";
var eventer = window[eventMethod];
var messageEvent = eventMethod == "attachEvent" ? "onmessage" : "message";

// Listen to message from child window
eventer(messageEvent, function(e) {
    console.log('parent received message!:  ', e.data);

    if (e.data == "ChildIsLoaded") {
        child_is_loaded();
    }
}, false);


function open_child_window(url, features, force_new) {

    if (features === undefined) {
        features = "height=230,width=360,toolbar=no,menubar=no"
    }

    if (force_new === undefined) {
        force_new = false;
    }

    if (force_new) {
        wChild = window.open(url, "bof-child", features);
        return;
    }

    // This gets a reference to the window if it exists, or opens a new blank window.
    wChild = window.open("", "bof-child", features);

    // Ask the child if it's loaded. If it is, then the event system will respond.
    wChild.postMessage("CheckChildLoaded", "*");

    // Only set the window's URL if it's not already open. The delay gives the child time to respond.
    setTimeout(function() {
        if (needToOpen) {
            wChild = window.open(url, "bof-child", features);
        }
    }, 500);
}

function child_is_loaded() {
    needToOpen = false;
}

function close_child_window_and_redirect() {
    if (needToOpen) {  // Wait a bit and try again
        setTimeout(close_child_window_and_redirect, 200);
        return;
    }
    wChild.postMessage("FinishAndClose", "*");

    window.location.href = "/redirect_next_page";
}