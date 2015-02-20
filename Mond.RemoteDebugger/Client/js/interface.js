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

function switchRunningState(obj) {
    switchState(obj.Running ? "running" : "break");

    if (obj.Running)
        return;

    highlightSourceBackground(obj.Id, obj.StartLine, obj.StartColumn, obj.EndLine, obj.EndColumn);
    switchToTab(obj.Id);
    scrollToLine(obj.StartLine);
}

function generateSourceHtml(id, firstLine, code) {
    var lines = code.replace("\r", "").split("\n");

    var state = {
        state: "normal"
    };

    var backgroundHtml = "<ol class='background' start='" + firstLine + "'>";
    var sourceHtml = "<ol class='source' start='" + firstLine + "'>";

    for (var i = 0; i < lines.length; i++) {
        var line = lines[i];

        // fixes line number click issues
        if (line == "")
            line = " ";

        backgroundHtml += "<li><div>" + escapeHtml(line) + "</div></li>";
        sourceHtml += "<li><div>" + syntaxHighlight(line, state) + "</div></li>";
    }

    backgroundHtml += "</ol>";
    sourceHtml += "</ol>";

    var html = "<div data-id='" + id + "' start='" + firstLine + "'>";

    html += backgroundHtml;
    html += sourceHtml;

    html += "</div>";

    return html;
}

function addProgram(id, data) {
    var tab = $("<li/>").attr("data-id", id).text(data.FileName);

    tab.click(function () {
        switchToTab(id);
    });

    sourceTabs.append(tab);
    sourceView.append(generateSourceHtml(id, data.FirstLine, data.SourceCode));

    var sourceList = sourceView.find("> div[data-id='" + id + "'] > ol.source");
    var firstLine = sourceList.attr("start") | 0;

    function makeClickEvent(line, elem) {
        return function (ev) {
            if (ev.target.tagName !== "LI")
                return;

            requestSetBreakpoint(id, line, !elem.hasClass("breakpoint"));
        }
    }

    var sourceLines = sourceList.children();

    for (var i = 0, l = firstLine; i < sourceLines.length; i++, l++) {
        var e = $(sourceLines[i]);
        e.click(makeClickEvent(l, e));
    }

    for (i = 0; i < data.Breakpoints.length; i++) {
        setBreakpoint(id, data.Breakpoints[i], true);
    }
}

function switchToTab(id) {
    sourceTabs.find("> li").removeClass("selected");
    sourceTabs.find("> li[data-id='" + id + "']").addClass("selected");

    sourceView.find("> div").hide();
    sourceView.find("> div[data-id='" + id + "']").show();
}

function scrollToLine(line) {
    var sourceDiv = sourceView.find("> div").filter(":visible");
    var sourceList = sourceDiv.find("> ol.source");
    var lineElem = sourceList.children()[line];

    // TODO: support bad browsers

    lineElem.scrollIntoViewIfNeeded();
}

function setBreakpoint(id, line, value) {
    var sourceList = sourceView.find("> div[data-id='" + id + "'] > ol.source");
    var sourceLines = sourceList.children();
    var firstLine = sourceList.attr("start") | 0;

    line -= firstLine;

    if (value)
        $(sourceLines[line]).addClass("breakpoint");
    else
        $(sourceLines[line]).removeClass("breakpoint");
}
