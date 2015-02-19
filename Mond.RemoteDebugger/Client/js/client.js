var connectBtn = $("#connect");
var disconnectBtn = $("#disconnect");
var breakBtn = $("#break");
var continueBtn = $("#continue");
var stepInBtn = $("#step-in");
var stepOverBtn = $("#step-over");
var stepOutBtn = $("#step-out");

var sources = $("#sources");

var socket = null;

connectBtn.click(function() {
    if (socket !== null)
        return;

    socket = new WebSocket("ws://127.0.0.1:1597/");

    socket.onopen = function () {
        alert("connected!");
    };

    socket.onmessage = function (msg) {
        var obj = JSON.parse(msg.data);
        console.log(obj);

        switch (obj.Type) {
            case "FullState":
                sources.html("");
                break;
        }
    };
});
