var eventMethod = window.addEventListener ? "addEventListener" : "attachEvent";
var eventer = window[eventMethod];
var messageEvent = eventMethod == "attachEvent" ? "onmessage" : "message";

eventer(messageEvent, function(e) {

	if (e.data == "CheckChildLoaded") {
		window.opener.postMessage("ChildIsLoaded", "*");
	}
	if (e.data == "FinishAndClose") {
	    window.close();
	}

}, false);


function get_parent_page() {
    var parent_url = window.opener.document.URL;
    var origin = document.location.origin;

    return parent_url.replace(origin + "/", "");
}