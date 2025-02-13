// CodeMirror, copyright (c) by Marijn Haverbeke and others
// Distributed under an MIT license: http://codemirror.net/LICENSE

// LUA mode. Ported to CodeMirror 2 from Franciszek Wawrzak's
// CodeMirror 1 mode.
// highlights keywords, strings, comments (no leveling supported! ("[==[")), tokens, basic indenting

(function(mod) {
  if (typeof exports == "object" && typeof module == "object") // CommonJS
    mod(require("../../lib/codemirror"));
  else if (typeof define == "function" && define.amd) // AMD
    define(["../../lib/codemirror"], mod);
  else // Plain browser env
    mod(CodeMirror);
})(function(CodeMirror) {
"use strict";

CodeMirror.defineMode("mond", function(config, parserConfig) {
  var indentUnit = config.indentUnit;

  function prefixRE(words) {
    return new RegExp("^(?:" + words.join("|") + ")", "");
  }
  function wordRE(words) {
    return new RegExp("^(?:" + words.join("|") + ")$", "");
  }
  var specials = wordRE(parserConfig.specials || []);

  // long list of standard functions from lua manual
  var builtins = wordRE([
    "global", "undefined", "null", "true", "false", "NaN", "Infinity"
  ]);
  var keywords = wordRE([ "var", "const", "fun", "return", "seq", "yield", "if", "else",
                          "for", "foreach", "in", "while", "do", "break", "continue",
                          "switch", "case", "default", "from", "import", "export" ]);

  function normal(stream, state) {
    var ch = stream.next();
    if (ch == "/") {
      if (stream.eat("/")) {
        stream.skipToEnd();
        return "comment";
      } else if (stream.eat("*")) {
        state.curlev = 1;
        return (state.cur = bracketed())(stream, state);
      }
    }
    if (ch == "\"" || ch == "'")
      return (state.cur = string(ch))(stream, state);
    if (/\d/.test(ch)) {
      stream.match(/^(?:[xX][\da-fA-F]*|[bB]\d+|\d*(?:\.\d+)?(?:[eE][+-]?\d+)?)/, true);
      return "number";
    }
    if (/[\w_]/.test(ch)) {
      stream.eatWhile(/[\w_]/);
      return "variable";
    }
    return "other";
  }

  function bracketed() {
    return function(stream, state) {
      var ch;
      while ((ch = stream.next()) != null && state.curlev > 0) {
        if (ch == "*" && stream.eat("/")) {
          state.curlev--;
        } else if (ch == "/" && stream.eat("*")) {
          state.curlev++;
        }
        
        if (state.curlev == 0) {
          state.cur = normal;
          break;
        }
      }
      
      return "comment";
    };
  }

  function string(quote) {
    return function(stream, state) {
      var escaped = false, ch;
      while ((ch = stream.next()) != null) {
        if (ch == quote && !escaped) {
          state.cur = normal
          break;
        }
        escaped = !escaped && ch == "\\";
      }
      return "string";
    };
  }

  return {
    startState: function(basecol) {
      return {basecol: basecol || 0, indentDepth: 0, cur: normal};
    },

    token: function(stream, state) {
      if (stream.eatSpace()) return null;
      var style = state.cur(stream, state);
      var word = stream.current();
      if (style == "variable") {
        if (keywords.test(word)) style = "keyword";
        else if (builtins.test(word)) style = "builtin";
        else if (specials.test(word)) style = "variable-2";
      }
      if (style == "other") {
        if (word == "{") ++state.indentDepth;
        else if (word == "}") --state.indentDepth;
      }
      return style;
    },

    indent: function(state, textAfter) {
      console.log(textAfter);
      var closing = /}/g.test(textAfter);
      return state.basecol + indentUnit * (state.indentDepth - (closing ? 1 : 0));
    },

    lineComment: "//",
    blockCommentStart: "/*",
    blockCommentEnd: "*/"
  };
});

CodeMirror.defineMIME("text/x-mond", "mond");

});
