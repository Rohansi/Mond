var connectBtn = $("#connect");
var disconnectBtn = $("#disconnect");
var breakBtn = $("#break");
var continueBtn = $("#continue");
var stepInBtn = $("#step-in");
var stepOverBtn = $("#step-over");
var stepOutBtn = $("#step-out");

var sourceTabs = $("#source-tabs");
var sourceView = $("#source-view");

var state = "disconnected";
var socket = null;

function resetInterface() {
    sourceTabs.html("");
    sourceView.html("");
}

function switchState(newState) {
    $("#menu > li").hide();
    state = newState;

    if (newState == "disconnected") {
        connectBtn.show();
        return;
    }

    disconnectBtn.show();

    switch (newState) {
        case "connected":
            break;

        case "running":
            breakBtn.show();
            break;

        case "break":
            continueBtn.show();
            stepInBtn.show();
            stepOverBtn.show();
            stepOutBtn.show();
            break;

        default:
            console.warn("unhandled state: " + newState);
            break;
    }
}

connectBtn.click(function () {
    if (socket !== null)
        return;

    socket = new WebSocket("ws://127.0.0.1:1597/");

    socket.onopen = function () {
        switchState("connected");
        console.log(state);
    };

    socket.onclose = function () {
        if (state == "disconnected")
            alert("failed to connect");

        socket = null;
        resetInterface();

        switchState("disconnected");
        console.log(state);
    };

    socket.onmessage = function (msg) {
        var obj = JSON.parse(msg.data);
        console.log(obj);

        switch (obj.Type) {
            case "InitialState":
                resetInterface();

                for (var i = 0; i < obj.Programs.length; i++) {
                    var program = obj.Programs[i];

                    sourceTabs.append($("<li/>").attr("data-id", i).text(program.FileName));
                    sourceView.append(generateSourceHtml(i, program.FirstLine, program.SourceCode));
                }

                switchState(obj.Running ? "running" : "break");

                if (!obj.Running)
                    highlightSourceBackground(0, obj.StartLine, obj.StartColumn, obj.EndLine, obj.EndColumn);

                break;

            case "NewProgram":
                sourceTabs.append($("<li/>").attr("data-id", obj.Id).text(obj.FileName));
                sourceView.append(generateSourceHtml(obj.Id, obj.FirstLine, obj.SourceCode));
                break;

            case "State":
                switchState(obj.Running ? "running" : "break");

                if (!obj.Running)
                    highlightSourceBackground(0, obj.StartLine, obj.StartColumn, obj.EndLine, obj.EndColumn);

                break;

            default:
                console.warn("unhandled message type: " + obj.Type);
                break;
        }
    };
});

disconnectBtn.click(function () {
    if (socket === null)
        return;

    socket.close();
});

function registerActionEvent(elem, action, requiredState) {
    elem.click(function () {
        if (state != requiredState || socket === null)
            return;

        socket.send(JSON.stringify({
            Type: "Action",
            Action: action
        }));
    });
}

registerActionEvent(breakBtn, "break", "running");
registerActionEvent(continueBtn, "run", "break");
registerActionEvent(stepInBtn, "step-in", "break");
registerActionEvent(stepOverBtn, "step-over", "break");
registerActionEvent(stepOutBtn, "step-out", "break");
