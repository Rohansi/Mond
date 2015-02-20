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

var highlightOperators = {
    ";": [";"],
    ",": [","],
    ".": ["...", "."],
    "?": ["?"],
    ":": [":"],

    "(": ["("],
    ")": [")"],

    "{": ["{"],
    "}": ["}"],

    "[": ["["],
    "]": ["]"],

    "+": ["++", "+=", "+"],
    "-": ["->", "--", "-=", "-"],
    "*": ["*=", "*"],
    "/": ["/=", "/"],
    "%": ["%=", "%"],

    "=": ["==", "="],
    "!": ["!"],
    ">": [">=", ">"],
    "<": ["<=", "<"],
    "&": ["&&"],
    "|": ["||", "|>"]
};

var highlightKeywords = {
    "global": 1,
    "undefined": 1,
    "null": 1,
    "true": 1,
    "false": 1,
    "NaN": 1,
    "Infinity": 1,

    "var": 1,
    "const": 1,
    "fun": 1,
    "return": 1,
    "seq": 1,
    "yield": 1,
    "if": 1,
    "else": 1,
    "for": 1,
    "foreach": 1,
    "in": 1,
    "while": 1,
    "do": 1,
    "break": 1,
    "continue": 1,
    "switch": 1,
    "case": 1,
    "default": 1,
    "debugger": 1
};

function syntaxHighlight(line, state) {
    var i = 0;

    function isNext(value) {
        for (var j = 0; j < value.length; j++) {
            var index = i + j;
            if (index >= line.length)
                return false;

            if (line[index] != value[j])
                return false;
        }

        return true;
    }

    function highlightNormal() {
        while (i < line.length) {
            if (isNext("//")) {
                var rest = line.substr(i + 2);
                i = line.length;
                return "<span class='comment'>//" + escapeHtml(rest) + "</span>";
            }

            if (isNext("/*")) {
                state.state = "multiComment";
                state.first = true;
                state.commentDepth = 1;
                return null;
            }

            var ch = line[i];

            if (ch == "\"" || ch == "\'") {
                state.state = "string";
                state.first = true;
                state.stringTerminator = ch;
                return null;
            }

            var operators = highlightOperators[ch];
            if (operators != undefined) {
                for (var j = 0; j < operators.length; j++) {
                    var operator = operators[j];
                    if (isNext(operator)) {
                        i += operator.length;
                        return "<span class='operator'>" + escapeHtml(operator) + "</span>";
                    }
                }
            }

            var start = i;

            if (ch.match(/[a-z_]/i)) {
                while (i < line.length && line[i].match(/[a-z0-9_]/i)) {
                    i++;
                }

                var word = line.substring(start, i);
                var type = highlightKeywords.hasOwnProperty(word) ? "keyword" : "identifier";

                return "<span class='" + type + "'>" + word + "</span>";
            }

            if (ch.match(/[0-9]/)) {
                var format = "decimal";
                var hasDecimal = false;
                var hasExp = false;
                var justTake = false;

                if (ch == "0" && i + 1 < line.length) {
                    var nextChar = line[i + 1];

                    if (nextChar == "x" || nextChar == "X")
                        format = "hexadecimal";

                    if (nextChar == "b" || nextChar == "B")
                        format = "binary";

                    if (format != "decimal") {
                        if (i + 2 < line.length && line[i + 2] == "_") {
                            i++;
                            return "<span class='number'>0</span>";
                        }

                        i += 2;
                    }
                }

                function isDigit(value) {
                    return value.match(/[0-9]/) || (format == "hexadecimal" && value.match(/[a-f]/i));
                }

                while (i < line.length) {
                    var c = line[i];

                    if (justTake) {
                        i++;
                        continue;
                    }

                    if (c == "_" && (i + 1 < line.length && isDigit(line[i + 1]))) {
                        i++;
                        continue;
                    }

                    if (format == "decimal") {
                        if (c == "." && !hasDecimal && !hasExp) {
                            hasDecimal = true;

                            if (i + 1 >= line.length || !isDigit(line[i + 1]))
                                break;

                            i++;
                            continue;
                        }

                        if ((c == "e" || c == "E") && !hasExp) {
                            if (i + 1 < line.length) {
                                var next = line[i + 1];
                                if (next == "+" || next == "-")
                                    justTake = true;
                            }

                            hasExp = true;
                            i++;
                            continue;
                        }
                    }

                    if (!isDigit(c))
                        break;

                    i++;
                }

                var number = line.substring(start, i);
                return "<span class='number'>" + escapeHtml(number) + "</span>";
            }

            i++;
            return ch;
        }

        return null;
    }

    function highlightMultiComment() {
        var start = i;

        if (state.first) {
            state.first = false;
            i += 2;
        }

        while (state.commentDepth > 0) {
            if (i >= line.length)
                break;

            if (isNext("/*")) {
                i += 2;
                state.commentDepth++;
                continue;
            }

            if (isNext("*/")) {
                i += 2;
                state.commentDepth--;
                continue;
            }

            i++;
        }

        if (state.commentDepth <= 0)
            state.state = "normal";

        var text = line.substring(start, i);
        return "<span class='comment'>" + escapeHtml(text) + "</span>";
    }

    function highlightString() {
        var start = i;

        if (state.first) {
            state.first = false;
            i++;
        }

        while (i < line.length) {
            var ch = line[i];

            if (ch == "\\" && i + 1 < line.length && line[i + 1] == state.stringTerminator) {
                i += 2;
                continue;
            }

            i++;

            if (ch == state.stringTerminator) {
                state.state = "normal";
                break;
            }
        }

        var text = line.substring(start, i);
        return "<span class='string'>" + escapeHtml(text) + "</span>";
    }

    var result = "";

    while (i < line.length) {
        var element;

        switch (state.state) {
            case "normal":
                element = highlightNormal();
                break;

            case "multiComment":
                element = highlightMultiComment();
                break;

            case "string":
                element = highlightString();
                break;
        }

        if (element !== null)
            result += element;
    }

    return result;
}
