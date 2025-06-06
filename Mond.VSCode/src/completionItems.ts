export const keywords: string[] = [
    "var",
    "const",
    "fun",
    "return",
    "seq",
    "yield",
    "if",
    "else",
    "for",
    "foreach",
    "in",
    "while",
    "do",
    "break",
    "continue",
    "switch",
    "case",
    "default",
    "debugger",
    "global",
    "undefined",
    "null",
    "true",
    "false",
    "NaN",
    "Infinity"
];

const moduleConstants: { [module: string]: string[] } = {
    "Math": [
        "PI",
        "E",
    ],
};

export const constants = Object.entries(moduleConstants).flatMap(([module, members]) => members.map(m => `${module}.${m}`));

const standaloneMethods: string[] = [
    // Require module
    "require",

    // Proxy module
    "proxyCreate",

    // Error module
    "error",
    "try",

    // Parse module
    "parseFloat",
    "parseInt",
    "parseHex",

    // Console Output module
    "print",
    "printLn",

    // Console Input module
    "readLn",

    // Random module
    "Random",
];

const moduleMethods: { [module: string]: string[] } = {
    "Char": [
        "toNumber",
        "fromNumber",
        "convertFromUtf32",
        "convertToUtf32",
        "getNumericValue",
        "getUnicodeCategory",
        "isControl",
        "isDigit",
        "isHighSurrogate",
        "isLetter",
        "isLetterOrDigit",
        "isLower",
        "isLowSurrogate",
        "isNumber",
        "isPunctuation",
        "isSeparator",
        "isSurrogate",
        "isSurrogatePair",
        "isSymbol",
        "isUpper",
        "isWhiteSpace",
    ],
    "Math": [
        "abs",
        "acos",
        "asin",
        "atan",
        "atan2",
        "ceiling",
        "clamp",
        "cos",
        "cosh",
        "exp",
        "floor",
        "log",
        "log10",
        "max",
        "min",
        "pow",
        "round",
        "sign",
        "sin",
        "sinh",
        "sqrt",
        "tan",
        "tanh",
        "truncate",
    ],
    "Json": [
        "serialize",
        "deserialize",
    ],
    "Async": [
        "start",
        "run",
        "runToCompletion",
        "delay",
        "whenAll",
        "whenAny",
        "getTask",
        "setCanceled",
        "setException",
        "setResult",
        "isCancellationRequested",
        "getToken",
        "cancel",
        "cancelAfter",
        "isCancellationRequested",
        "register",
        "throwIfCancellationRequested",
    ],
};

const moduleMethodsFlattened = Object.entries(moduleMethods).flatMap(([module, members]) => members.map(m => `${module}.${m}`));
export const methods = standaloneMethods.concat(moduleMethodsFlattened);
