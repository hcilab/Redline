function RingBuffer(N){
    this.size = N;
    this.data = [];
    this.currentIndex = 0;
    this.sum = 0;

    this.push_front = function (a) {
        if(this.data.length == this.size) this.sum -= this.data[this.currentIndex];
        this.data[this.currentIndex] = a;
        this.sum += a;
        this.currentIndex = (this.currentIndex + 1) % this.size;
    }

    this.average = function () {
        return this.sum / this.data.length;
    }

    this.full = function () {
        return this.data.length == this.size;
    }

    this.getCurrent = function () {
        var index = this.currentIndex - 1;
        if(index < 0) index = this.size - 1;
        return this.data[index];
    }

    this.weightedAvg = function () {
        var wa = 0;
        var currentWeight = 0.8;
        for(var i = 1; i < this.data.length + 1; i++) {
            var index = this.currentIndex - i;
            if(index < 0) index += this.size;
        }
    }
}

function kurtosis(data) {
    var avg = 0;
    for(var i = 0; i < data.length; i++) avg += data[i];
    avg /= data.length;

    var top = 0;
    var bottom = 0;

    for(var i = 0; i < data.length; i++) {
        var tmp = data[i] - avg;
        tmp *= tmp;
        top += (tmp * tmp);
        bottom += tmp;
    }
    bottom *= bottom;

    return data.length * (top / bottom);
}

function average(data){
    var sum = data.reduce(function(sum, value){
        return sum + value;
    }, 0);

    var avg = sum / data.length;
    return avg;
}

function standardDeviation(values){
    var avg = average(values);

    var squareDiffs = values.map(function(value){
        var diff = value - avg;
        var sqrDiff = diff * diff;
        return sqrDiff;
    });

    var avgSquareDiff = average(squareDiffs);

    var stdDev = Math.sqrt(avgSquareDiff);
    return stdDev;
}

function indexOfMax(arr) {
    if (arr.length === 0) {
        return -1;
    }

    var max = arr[0];
    var maxIndex = 0;

    for (var i = 1; i < arr.length; i++) {
        if (arr[i] > max) {
            maxIndex = i;
            max = arr[i];
        }
    }
    return maxIndex;
}