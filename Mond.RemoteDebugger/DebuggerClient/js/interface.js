var connectBtn = $("#connect");
var disconnectBtn = $("#disconnect");
var breakBtn = $("#break");
var continueBtn = $("#continue");
var stepInBtn = $("#step-in");
var stepOverBtn = $("#step-over");
var stepOutBtn = $("#step-out");

var sourceTabs = $("#source-tabs");
var sourceView = $("#source-view");
var watchList = $("#watch-list");
var watchInput = $("#watch-input");
var stackList = $("#stack-list");

var state = "disconnected";

function resetInterface() {
    sourceTabs.html("");
    sourceView.html("");
    watchList.find("tr.watch").remove();
    stackList.html("");
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

    if (obj.Running) {
        highlightSourceBackground(0, -1, -1, -1, -1);
        return;
    }

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

    var html = "<div data-id='" + id + "'>";

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
    var firstLine = sourceList.attr("start") | 0;
    var lineElem = sourceList.children()[line - firstLine];

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

function addWatch(obj) {
    var headers = watchList.find("tr.input");
    headers.before("<tr class='watch' data-id='" + obj.Id + "'><td><a>✕</a><span>" + escapeHtml(obj.Expression) + "</span></td><td>" + escapeHtml(obj.Value) + "</td></tr>");

    var remove = headers.prev().find("a");
    remove.click(function () {
        requestRemoveWatch(obj.Id);
    });
}

function removeWatch(id) {
    watchList.find("tr[data-id='" + id + "']").remove();
}

function updateWatches(watches) {
    watchList.find("tr.watch").remove();

    for (var i = 0; i < watches.length; i++) {
        addWatch(watches[i]);
    }
}

function updateCallStack(callStack) {
    stackList.html("");

    for (var i = 0; i < callStack.length; i++) {
        var entry = callStack[i];

        var str = "";

        if (entry.Function != null)
            str += entry.Function !== "" ? entry.Function : "main";

        if (entry.FileName != null)
            str += " [" + entry.FileName;

        if (entry.ColumnNumber > 0)
            str += " line " + entry.LineNumber + ":" + entry.ColumnNumber;

        if (entry.FileName != null)
            str += "]";

        stackList.append("<li>" + escapeHtml(str) + "</li>");
    }
}

// https://gist.github.com/hsablonniere/2581101
if (!Element.prototype.scrollIntoViewIfNeeded) {
    Element.prototype.scrollIntoViewIfNeeded = function (centerIfNeeded) {
        centerIfNeeded = arguments.length === 0 ? true : !!centerIfNeeded;

        var parent = this.parentNode.parentNode.parentNode, // TODO: don't hardcode the parent!
            parentComputedStyle = window.getComputedStyle(parent, null),
            parentBorderTopWidth = parseInt(parentComputedStyle.getPropertyValue('border-top-width')),
            parentBorderLeftWidth = parseInt(parentComputedStyle.getPropertyValue('border-left-width')),
            overTop = this.offsetTop - parent.offsetTop < parent.scrollTop,
            overBottom = (this.offsetTop - parent.offsetTop + this.clientHeight - parentBorderTopWidth) > (parent.scrollTop + parent.clientHeight),
            overLeft = this.offsetLeft - parent.offsetLeft < parent.scrollLeft,
            overRight = (this.offsetLeft - parent.offsetLeft + this.clientWidth - parentBorderLeftWidth) > (parent.scrollLeft + parent.clientWidth),
            alignWithTop = overTop && !overBottom;

        if ((overTop || overBottom) && centerIfNeeded) {
            parent.scrollTop = this.offsetTop - parent.offsetTop - parent.clientHeight / 2 - parentBorderTopWidth + this.clientHeight / 2;
        }

        if ((overLeft || overRight) && centerIfNeeded) {
            parent.scrollLeft = this.offsetLeft - parent.offsetLeft - parent.clientWidth / 2 - parentBorderLeftWidth + this.clientWidth / 2;
        }

        if ((overTop || overBottom || overLeft || overRight) && !centerIfNeeded) {
            this.scrollIntoView(alignWithTop);
        }
    };
}
