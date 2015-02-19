var entityMap = {
    "&": "&amp;",
    "<": "&lt;",
    ">": "&gt;",
    '"': "&quot;",
    "'": "&#39;",
    "/": "&#x2F;"
};

function escapeHtml(string) {
    return String(string).replace(/[&<>"'\/]/g, function (s) {
        return entityMap[s];
    });
}

function lineToSpaces(line) {
    var result = "";

    for (var i = 0; i < line.length; i++) {
        result += line[i] == "\t" ? "\t" : " ";
    }

    return result;
}

function generateSourceHtml(id, firstLine, code) {
    var lines = code.replace("\r", "").split("\n");

    var backgroundHtml = "<ol class='background' start='" + firstLine + "'>";
    var sourceHtml = "<ol class='source' start='" + firstLine + "'>";

    for (var i = 0; i < lines.length; i++) {
        backgroundHtml += "<li><span class='red'>" + escapeHtml(lines[i]) + "</span></li>";
        sourceHtml += "<li><span>" + escapeHtml(lines[i]) + "</span></li>";
    }

    backgroundHtml += "</ol>";
    sourceHtml += "</ol>";

    var html = "<div data-id='" + id + "' start='" + firstLine + "'>";

    html += backgroundHtml;
    html += sourceHtml;

    html += "</div>";

    return html;
}

function highlight(id, startLine, startColumn, endLine, endColumn) {
    var background = $("#source-view > div[data-id='" + id + "'] > ol.background");
    var lines = background.find("> li");

    lines.find("> span").replaceWith(function () {
        return $(this).text();
    });

    var firstLine = background.attr("start") | 0;

    startLine -= firstLine;
    startColumn -= 1;
    endLine -= firstLine;
    endColumn -= 1;

    for (var i = startLine; i <= endLine; i++) {
        var line = $(lines[i]);
        var lineText = line.html();
        var normal;
        var red;

        if (i == startLine) {
            normal = lineText.substr(0, startColumn);
            red = lineText.substr(startColumn, lineText.length - startColumn);
            line.html(normal + "<span class='red'>" + red + "</span>");
        } else if (i == endLine) {
            red = lineText.substr(0, endColumn + 1);
            normal = lineText.substr(endColumn, lineText.length - endColumn);
            line.html("<span class='red'>" + red + "</span>" + normal);
        } else {
            line.html("<span class='red'>" + lineText + "</span>");
        }
    }
}
