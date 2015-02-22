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

                if (obj.Watches)
                    updateWatches(obj.Watches);

                if (obj.CallStack)
                    updateCallStack(obj.CallStack);

                break;

            case "NewProgram":
                addProgram(obj.Id, obj);
                break;

            case "State":
                switchRunningState(obj);

                if (obj.Watches)
                    updateWatches(obj.Watches);

                if (obj.CallStack)
                    updateCallStack(obj.CallStack);

                break;

            case "Breakpoint":
                setBreakpoint(obj.Id, obj.Line, obj.Value);
                break;

            case "AddedWatch":
                addWatch(obj);
                break;

            case "RemovedWatch":
                removeWatch(obj.Id);
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

watchInput.keydown(function (ev) {
    if (socket === null || ev.keyCode !== 13)
        return true;

    var value = watchInput.val();
    watchInput.val("");

    socket.send(JSON.stringify({
        Type: "AddWatch",
        Expression: value
    }));

    return false;
});

function requestSetBreakpoint(id, line, value) {
    if (socket === null)
        return;

    socket.send(JSON.stringify({
        Type: "SetBreakpoint",
        Id: id,
        Line: line,
        Value: value
    }));
}

function requestRemoveWatch(id) {
    if (socket === null)
        return;

    socket.send(JSON.stringify({
        Type: "RemoveWatch",
        Id: id
    }));
}