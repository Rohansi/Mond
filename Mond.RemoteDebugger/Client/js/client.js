var socket = null;

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
                    addProgram(i, obj.Programs[i]);
                }

                switchRunningState(obj);
                break;

            case "NewProgram":
                addProgram(obj.Id, obj);
                break;

            case "State":
                switchRunningState(obj);
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
