function generateSourceHtml(id, firstLine, code) {
    var lines = code.replace("\r", "").split("\n");

    var state = {
        state: "normal"
    };

    var backgroundHtml = "<ol class='background' start='" + firstLine + "'>";
    var sourceHtml = "<ol class='source' start='" + firstLine + "'>";

    for (var i = 0; i < lines.length; i++) {
        backgroundHtml += "<li><span class='red'>" + escapeHtml(lines[i]) + "</span></li>";
        sourceHtml += "<li><span>" + syntaxHighlight(lines[i], state) + "</span></li>";
    }

    backgroundHtml += "</ol>";
    sourceHtml += "</ol>";

    var html = "<div data-id='" + id + "' start='" + firstLine + "'>";

    html += backgroundHtml;
    html += sourceHtml;

    html += "</div>";

    return html;
}

function highlightSourceBackground(id, startLine, startColumn, endLine, endColumn) {
    var background = $("#source-view > div[data-id='" + id + "'] > ol.background");
    var lines = background.find("> li");

    lines.find("> span").replaceWith(function () {
        return $(this).text();
    });

    if (startColumn === undefined || startColumn < 0)
        return;

    var firstLine = background.attr("start") | 0;

    startLine -= firstLine;
    startColumn -= 1;
    endLine -= firstLine;
    endColumn -= 1;

    var line, lineText;

    if (startLine == endLine) {
        line = $(lines[startLine]);
        lineText = line.text();

        var left = lineText.substr(0, startColumn);
        var mid = lineText.substring(startColumn, endColumn + 1);
        var right = lineText.substr(endColumn, lineText.length - endColumn);

        line.html(left + "<span class='break'>" + mid + "</span>" + right);
        return;
    }

    for (var i = startLine; i <= endLine; i++) {
        line = $(lines[i]);
        lineText = line.text();

        var normal;
        var red;

        if (i == startLine) {
            normal = lineText.substr(0, startColumn);
            red = lineText.substr(startColumn, lineText.length - startColumn);
            line.html(normal + "<span class='break'>" + red + "</span>");
        } else if (i == endLine) {
            red = lineText.substr(0, endColumn + 1);
            normal = lineText.substr(endColumn, lineText.length - endColumn);
            line.html("<span class='break'>" + red + "</span>" + normal);
        } else {
            line.html("<span class='break'>" + lineText + "</span>");
        }
    }
}
