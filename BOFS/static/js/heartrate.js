/*
    Initilizes the measurment of heart rate with standard webcam.
    Triggers the "HRdata" event every time new heart rate data is available.
    The event contains attribute HR with the data or you can access the output element of HR for the data.
    The HR data contain a boolean if the data is valid, the time of the measured HR (in form of javascript Date.Now() timestamp),
    the HR in bpm, the quality of the FQ spectrum as the curtosis and
    the data quality as a relation between posssible number of datapoints and number of datapoints with a minimum std as well as
    the status of the detection of the head. Has to be "found" for a valid HR.

    @param size size of the frame buffer for the fft
    @param windowSize size of the window for the moving average
    @param debug if degbug mode is activated. logs output to the console
    @param videoElement the video element that is used to capture the user (only have to be set if you want to use custom input other than the webcam or display the video to the user)
    @param canvasElement the canvas the video input is rendered to (only have to be set if you want custom output)
 */
function HR(size, windowSize, debug, videoElement, canvasElement) {
    var _HR = this;
    this.size = (typeof size === 'undefined') ? 180 : size;
    this.windowSize = (typeof windowSize === 'undefined') ? 20 : windowSize;
    this.debug = (typeof debug === 'undefined') ? false : debug;
    this.video = (typeof videoElement === 'undefined') ? document.createElement("video") : videoElement;
    this.canvas = (typeof canvasElement === 'undefined') ? document.createElement("canvas") : canvasElement;

    this.canvas.style.cssText = "display:none";
    this.video.style.cssText = "display:none";
    document.body.appendChild(this.video);
    document.body.appendChild(this.canvas);

    this.rgb = [];
    this.times = [];
    this.box = {width: 0, height: 0, initialized: false};
    this.position = {x: 0, y: 0, initialized: false, timeLastAdjusted: 0};
    this.output = {valid: false, timestamp: 0, bpm: 0, FqQuality: 0, DataQuality: 0, status: ""};
    this.htracker = new headtrackr.Tracker({ui : false, headPosition : false});
    this.htracker.init(this.video, this.canvas);

    if(_HR.debug) console.log("init");

    this.start = function () {
        if(_HR.debug) console.log("starting");

        document.addEventListener("headtrackrStatus", function(event) {
            _HR.output.status = event.status;
        }, true);

        document.addEventListener("facetrackingEvent", function( event ) {
            if (!_HR.box.initialized) {
                _HR.box.width = _HR.htracker._width * 0.18;
                _HR.box.height = _HR.htracker._height * 0.1;
                _HR.box.initialized = true;
            }

            if (!_HR.position.initialized || (Date.now() - _HR.position.timeLastAdjusted > 2000)) {
                _HR.position.x = event.x - _HR.box.width / 2;
                _HR.position.y = event.y - event.height / 2.7;
                _HR.position.initialized = true;
                _HR.position.timeLastAdjusted = Date.now();
            }

            var ctx = _HR.canvas.getContext('2d');
            var imgData = ctx.getImageData(_HR.position.x, _HR.position.y, _HR.box.width, _HR.box.height);

            var sumGreen = 0;
            for (var i = 0; i < imgData.data.length; i += 4) {
                sumGreen += imgData.data[i+1];
            }
            sumGreen /= imgData.data.length / 4;

            _HR.rgb.push(sumGreen);
            _HR.times.push(Date.now());

            if(_HR.rgb.length > _HR.size) {
                _HR.rgb.shift();
                _HR.times.shift();
                _HR.output.valid = true;
            }
            else {
            	_HR.output.valid = false;
            	return;
            }

            //filtering
            var window = new RingBuffer(_HR.windowSize);
            var means = [];
            var stds = [];
            for (var i = 0; i < _HR.rgb.length; ++i) {
                window.push_front(_HR.rgb[i]);
                if(window.full()) {
                    means.push(window.average());
                    stds.push(standardDeviation(window.data));
                }
            }

            var ntimes = [];
            var nData = [];
            for (var i = 0; i < means.length; i++) {
                if(stds[i] < 0.8) {
                    ntimes.push(_HR.times[i + _HR.windowSize / 2]);
                    nData.push(_HR.rgb[i  + _HR.windowSize / 2] - means[i]);
                }
            }

            if(nData.length === 0) {
                _HR.output.FqQuality = 0;
                _HR.output.DataQuality = 0;
                _HR.output.timestamp = average(_HR.times);
                return;
            }

            var totalTime = _HR.times[_HR.times.length - 1] - _HR.times[0];
            var frameTime = totalTime / _HR.times.length;
            var frameDif = _HR.rgb.length - nData.length;
            var filteredTime = totalTime - (frameDif * frameTime);
            var samplingRate = ntimes.length / (filteredTime / 1000);

            var c_signal = new numeric.T(nData, numeric.rep([nData.length], 0));
            var spectrum = c_signal.fft();

            var spec = [];
            var fqs = [];
            for (var i = 0; i < spectrum.x.length; ++i) {
                var mag = Math.sqrt(spectrum.x[i] * spectrum.x[i] + spectrum.y[i] * spectrum.y[i]);
                var freq = (i / ntimes.length) * samplingRate * 60;

                if(freq > 50 && freq < 200) {
                    spec.push(mag);
                    fqs.push(freq);
                }
            }

            var kurt = kurtosis(spec);
            if(kurt > 4) {
                var indexMax = indexOfMax(spec);
                if(indexMax === 0) {
                    spec[0] /= 2;
                    indexMax = indexOfMax(spec);
                }
                _HR.output.bpm = fqs[indexMax];
                _HR.output.FqQuality = kurt;
                _HR.output.DataQuality = nData.length / (_HR.size - _HR.windowSize / 2);
                _HR.output.timestamp = average(_HR.times);
            }

            if(_HR.debug) console.log(_HR.output.bpm + " bpm (status = " + _HR.output.status + ")");

            var dataEvent = document.createEvent("Event");
            dataEvent.initEvent("HRdata", true, true);
            dataEvent.HR =  _HR.output;
            document.dispatchEvent(dataEvent);
        });

        _HR.htracker.start();
    }

    if(_HR.debug) console.log("HR is set up");
}

/**
//parameter
var size = 180;
var windowSize = 20;

var video = document.createElement("video");
var canvas = document.createElement("canvas");

video.style.cssText = "display:none";
canvas.style.cssText = "display:none";

document.body.appendChild(video);
document.body.appendChild(canvas);

// set up video and canvas elements needed
var canvasOverlay = document.getElementById('overlay');
var overlayContext = canvasOverlay.getContext('2d');
canvasOverlay.style.position = "absolute";
canvasOverlay.style.top = '0px';
canvasOverlay.style.zIndex = '100001';
canvasOverlay.style.display = 'block';

// the face tracking setup
var rgb = [];
var times = [];
var box = {width: 0, height: 0, initialized: false};
var position = {x: 0, y: 0, initialized: false, timeLastAdjusted: 0};
var output = {valid: false, timestamp: 0, bpm: 0, FqQuality: 0, DataQuality: 0, status: ""};

var htracker = new headtrackr.Tracker({ui : false, headPosition : false});
htracker.init(video, canvas);
htracker.start();

document.addEventListener("headtrackrStatus", function(event) {
    output.status = event.status;
}, true);

document.addEventListener("facetrackingEvent", function( event ) {
    canvasOverlay.width = htracker._width;
    canvasOverlay.height = htracker._height;

    if (!box.initialized) {
        box.width = htracker._width * 0.18;
        box.height = htracker._height * 0.1;
        box.initialized = true;
    }

    if (!position.initialized || (Date.now() - position.timeLastAdjusted > 2000)) {
        position.x = event.x - box.width / 2;
        position.y = event.y - event.height / 2.7;
        position.initialized = true;
        position.timeLastAdjusted = Date.now();
    }

    var ctx = canvas.getContext('2d');
    var imgData = ctx.getImageData(position.x, position.y, box.width, box.height);

    overlayContext.clearRect(0, 0, htracker._width, htracker._height);
    overlayContext.putImageData(imgData, 0 ,0);

    var sumGreen = 0;
    for (var i = 0; i < imgData.data.length; i += 4) {
        sumGreen += imgData.data[i+1];
    }
    sumGreen /= imgData.data.length / 4;

    rgb.push(sumGreen);
    times.push(Date.now());

    if(rgb.length > size) {
        rgb.shift();
        times.shift();
    }
    else return;

    var colordata = [{ color: "rgb(0, 255, 0)", data: [] }, { color: "rgb(255, 0, 0)", data: [] }, { color: "rgb(0, 0, 255)", data: [] }];
    var d3 = [{ color: "rgb(60, 150, 0)", data: [] }];


    //filtering
    var window = new RingBuffer(windowSize);
    var means = [];
    var stds = [];
    for (var i = 0; i < rgb.length; ++i) {
        window.push_front(rgb[i]);
        if(window.full()) {
            means.push(window.average());
            stds.push(standardDeviation(window.data));
        }
    }

    var ntimes = [];
    var nData = [];
    for (var i = 0; i < means.length; i++) {
        if(stds[i] < 0.8) {
            ntimes.push(times[i + windowSize / 2]);
            nData.push(rgb[i  + windowSize / 2] - means[i]);
        }
    }

    var totalTime = times[times.length - 1] - times[0];
    var frameTime = totalTime / times.length;
    var frameDif = rgb.length - nData.length;
    var filteredTime = totalTime - (frameDif * frameTime);
    var samplingRate = ntimes.length / (filteredTime / 1000);

    for (var i = 0; i < nData.length; i++) {
        colordata[0].data.push([i, nData[i]]);
    }

    //var c_signal = new numeric.T(nData, numeric.rep([nData.length], 0));
    var c_signal = new numeric.T(nData, numeric.rep([nData.length], 0));
    var spectrum = c_signal.fft();

    var spec = [];
    var fqs = [];
    for (var i = 0; i < spectrum.x.length; ++i) {
        var mag = Math.sqrt(spectrum.x[i] * spectrum.x[i] + spectrum.y[i] * spectrum.y[i]);
        var freq = (i / ntimes.length) * samplingRate * 60;

        // zwischen 50 und 150 bpm -> 0,83 und 2,5 Hz
        if(freq > 50 && freq < 200) {
            spec.push(mag);
            fqs.push(freq);
            d3[0].data.push([freq, mag]);
        }
    }

    var kurt = kurtosis(spec);
    if(kurt > 4) {
        var indexMax = indexOfMax(spec);
        if(indexMax === 0) {
            spec[0] /= 2;
            indexMax = indexOfMax(spec);
        }
        output.bpm = fqs[indexMax];
        output.FqQuality = kurt;
        output.DataQuality = nData.length / (size - windowSize / 2);
        output.timestamp = average(times);
    }

    document.getElementById("demo").innerHTML = output.bpm + " bpm (status = " + output.status + ")";

    var colorPlot = $.plot("#color", colordata, {
        series: {
            shadowSize: 0
        },
    });

    var p2 = $.plot("#ica", d3, {
        series: {
            shadowSize: 0
        }
    });

});
 */