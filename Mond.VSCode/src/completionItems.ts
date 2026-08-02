export type MondSymbolKind = 'keyword' | 'constant' | 'function' | 'method' | 'property' | 'module' | 'class';

export interface MondSymbol {
    readonly name: string;
    readonly kind: MondSymbolKind;
    /** Call signature without the owner, eg. `atan2(y, x)`. Absent for values. */
    readonly signature?: string;
    readonly documentation?: string;
}

export interface MondContainer extends MondSymbol {
    readonly kind: 'module' | 'class';
    readonly members: readonly MondSymbol[];
}

/**
 * Keywords are the only part of the standard library we can describe accurately by hand, so they
 * are the only ones with written documentation. Everything else is generated from the C# sources
 * and carries no doc comments.
 */
export const keywords: readonly MondSymbol[] = [
    { name: "var", kind: 'keyword', documentation: "Declares a mutable variable." },
    { name: "const", kind: 'keyword', documentation: "Declares a variable that cannot be reassigned." },
    { name: "fun", kind: 'keyword', documentation: "Declares a function. Use `-> expression;` for an expression bodied function." },
    { name: "return", kind: 'keyword', documentation: "Returns a value from the current function." },
    { name: "seq", kind: 'keyword', documentation: "Declares a sequence, a function that produces values with `yield`." },
    { name: "yield", kind: 'keyword', documentation: "Produces the next value of a sequence." },
    { name: "if", kind: 'keyword' },
    { name: "else", kind: 'keyword' },
    { name: "for", kind: 'keyword', documentation: "`for (init; condition; step) { }`" },
    { name: "foreach", kind: 'keyword', documentation: "`foreach (var item in sequence) { }`" },
    { name: "in", kind: 'keyword', documentation: "Tests whether a key exists in an object or array. Negate it with `!in`." },
    { name: "while", kind: 'keyword' },
    { name: "do", kind: 'keyword' },
    { name: "break", kind: 'keyword' },
    { name: "continue", kind: 'keyword' },
    { name: "switch", kind: 'keyword' },
    { name: "case", kind: 'keyword' },
    { name: "default", kind: 'keyword' },
    { name: "debugger", kind: 'keyword', documentation: "Breaks into the attached debugger." },
    { name: "export", kind: 'keyword', documentation: "Makes a declaration visible to modules that import this one." },
    { name: "import", kind: 'keyword', documentation: "`import Module;` or `from Module import { name };`" },
    { name: "from", kind: 'keyword', documentation: "`from Module import { name };`" },
    { name: "global", kind: 'keyword', documentation: "The global object." },
    { name: "undefined", kind: 'constant' },
    { name: "null", kind: 'constant' },
    { name: "true", kind: 'constant' },
    { name: "false", kind: 'constant' },
    { name: "NaN", kind: 'constant' },
    { name: "Infinity", kind: 'constant' },
    { name: "__declare_globals", kind: 'keyword', documentation: "Declares names that exist on the global object so the compiler accepts them." },
];

/** Every reserved word, including the ones that are never worth suggesting. */
export const keywordNames: readonly string[] = keywords.map(k => k.name);

function fn(name: string, signature: string): MondSymbol {
    return { name, kind: 'function', signature };
}

function method(name: string, signature: string): MondSymbol {
    return { name, kind: 'method', signature };
}

export const globalFunctions: readonly MondSymbol[] = [
    fn("require", "require(fileName)"),
    fn("proxyCreate", "proxyCreate(target, handler)"),
    fn("error", "error(message)"),
    fn("try", "try(function, ...arguments)"),
    fn("parseFloat", "parseFloat(str)"),
    fn("parseInt", "parseInt(str)"),
    fn("parseHex", "parseHex(str)"),
    fn("print", "print(...arguments)"),
    fn("printLn", "printLn(...arguments)"),
    fn("readLn", "readLn()"),
];

export const modules: readonly MondContainer[] = [
    {
        name: "Math", kind: 'module', members: [
            { name: "PI", kind: 'constant' },
            { name: "E", kind: 'constant' },
            method("abs", "abs(value)"),
            method("acos", "acos(d)"),
            method("asin", "asin(d)"),
            method("atan", "atan(d)"),
            method("atan2", "atan2(y, x)"),
            method("ceiling", "ceiling(d)"),
            method("clamp", "clamp(value, min, max)"),
            method("cos", "cos(d)"),
            method("cosh", "cosh(d)"),
            method("exp", "exp(d)"),
            method("floor", "floor(d)"),
            method("log", "log(d, [b])"),
            method("log10", "log10(d)"),
            method("max", "max(x, y)"),
            method("min", "min(x, y)"),
            method("pow", "pow(x, y)"),
            method("round", "round(d)"),
            method("sign", "sign(d)"),
            method("sin", "sin(d)"),
            method("sinh", "sinh(d)"),
            method("sqrt", "sqrt(d)"),
            method("tan", "tan(d)"),
            method("tanh", "tanh(d)"),
            method("truncate", "truncate(d)"),
        ]
    },
    {
        name: "Char", kind: 'module', members: [
            method("toNumber", "toNumber(s, [index])"),
            method("fromNumber", "fromNumber(num)"),
            method("convertFromUtf32", "convertFromUtf32(utf32)"),
            method("convertToUtf32", "convertToUtf32(s, [index])"),
            method("getNumericValue", "getNumericValue(s, [index])"),
            method("getUnicodeCategory", "getUnicodeCategory(s, [index])"),
            method("isControl", "isControl(s, [index])"),
            method("isDigit", "isDigit(s, [index])"),
            method("isHighSurrogate", "isHighSurrogate(s, [index])"),
            method("isLetter", "isLetter(s, [index])"),
            method("isLetterOrDigit", "isLetterOrDigit(s, [index])"),
            method("isLower", "isLower(s, [index])"),
            method("isLowSurrogate", "isLowSurrogate(s, [index])"),
            method("isNumber", "isNumber(s, [index])"),
            method("isPunctuation", "isPunctuation(s, [index])"),
            method("isSeparator", "isSeparator(s, [index])"),
            method("isSurrogate", "isSurrogate(s, [index])"),
            method("isSurrogatePair", "isSurrogatePair(s, [index])"),
            method("isSymbol", "isSymbol(s, [index])"),
            method("isUpper", "isUpper(s, [index])"),
            method("isWhiteSpace", "isWhiteSpace(s, [index])"),
        ]
    },
    {
        name: "Json", kind: 'module', members: [
            method("serialize", "serialize(value)"),
            method("deserialize", "deserialize(text)"),
        ]
    },
    {
        name: "Async", kind: 'module', members: [
            method("start", "start(value)"),
            method("run", "run()"),
            method("runToCompletion", "runToCompletion()"),
        ]
    },
    {
        name: "Task", kind: 'module', members: [
            method("delay", "delay(seconds, [cancellationToken])"),
            method("whenAll", "whenAll(...tasks)"),
            method("whenAny", "whenAny(...tasks)"),
        ]
    },
];

/** Constructors. Their members are instance methods, so they are not offered after `Name.`. */
export const classes: readonly MondContainer[] = [
    {
        name: "Random", kind: 'class', signature: "Random([seed])", members: [
            method("next", "next([minValue], [maxValue])"),
            method("nextDouble", "nextDouble()"),
        ]
    },
    {
        name: "TaskCompletionSource", kind: 'class', signature: "TaskCompletionSource()", members: [
            method("getTask", "getTask()"),
            method("setCanceled", "setCanceled()"),
            method("setException", "setException(message)"),
            method("setResult", "setResult(result)"),
        ]
    },
    {
        name: "CancellationTokenSource", kind: 'class', signature: "CancellationTokenSource([seconds])", members: [
            method("isCancellationRequested", "isCancellationRequested()"),
            method("getToken", "getToken()"),
            method("cancel", "cancel()"),
            method("cancelAfter", "cancelAfter(seconds)"),
        ]
    },
    {
        name: "CancellationToken", kind: 'class', signature: "CancellationToken(canceled)", members: [
            method("isCancellationRequested", "isCancellationRequested()"),
            method("register", "register(function)"),
            method("throwIfCancellationRequested", "throwIfCancellationRequested()"),
        ]
    },
];

/**
 * Prototype methods, offered after any `.` we cannot resolve to a module. Grouped by the type they
 * belong to so hover can say where a method comes from.
 */
export const prototypes: readonly MondContainer[] = [
    {
        name: "Value", kind: 'class', members: [
            method("getType", "getType()"),
            method("toString", "toString()"),
            method("serialize", "serialize()"),
            method("getPrototype", "getPrototype()"),
        ]
    },
    {
        name: "String", kind: 'class', members: [
            method("charAt", "charAt(index)"),
            method("charCodeAt", "charCodeAt(index)"),
            method("contains", "contains(value)"),
            method("endsWith", "endsWith(value)"),
            method("format", "format(...arguments)"),
            method("getEnumerator", "getEnumerator()"),
            method("indexOf", "indexOf(value)"),
            method("insert", "insert(index, value)"),
            method("lastIndexOf", "lastIndexOf(value)"),
            method("length", "length()"),
            method("normalize", "normalize()"),
            method("replace", "replace(oldValue, newValue)"),
            method("split", "split(separator)"),
            method("startsWith", "startsWith(value)"),
            method("substring", "substring(startIndex, [length])"),
            method("toLower", "toLower()"),
            method("toUpper", "toUpper()"),
            method("trim", "trim()"),
        ]
    },
    {
        name: "Array", kind: 'class', members: [
            method("add", "add(item)"),
            method("clear", "clear()"),
            method("contains", "contains(item)"),
            method("getEnumerator", "getEnumerator()"),
            method("indexOf", "indexOf(item)"),
            method("insert", "insert(index, item)"),
            method("lastIndexOf", "lastIndexOf(item)"),
            method("length", "length()"),
            method("remove", "remove(item)"),
            method("removeAt", "removeAt(index)"),
            method("sort", "sort([index], [count])"),
            method("sortDescending", "sortDescending([index], [count])"),
        ]
    },
    {
        name: "Object", kind: 'class', members: [
            method("add", "add(key, value)"),
            method("clear", "clear()"),
            method("containsKey", "containsKey(key)"),
            method("containsValue", "containsValue(value)"),
            method("get", "get(key)"),
            method("getEnumerator", "getEnumerator()"),
            method("length", "length()"),
            method("lock", "lock()"),
            method("remove", "remove(key)"),
            method("setPrototype", "setPrototype(value)"),
            method("setPrototypeAndLock", "setPrototypeAndLock(value)"),
        ]
    },
    {
        name: "Number", kind: 'class', members: [
            method("isNaN", "isNaN()"),
        ]
    },
    {
        name: "Function", kind: 'class', members: [
            method("getName", "getName()"),
        ]
    },
];

/** Everything reachable from the global scope without a prefix. */
export const globals: readonly MondSymbol[] = [
    ...globalFunctions,
    ...modules,
    ...classes,
];

const moduleMap = new Map(modules.map(m => [m.name, m]));
const classMap = new Map(classes.map(c => [c.name, c]));
const globalMap = new Map(globals.map(g => [g.name, g]));
const keywordMap = new Map(keywords.map(k => [k.name, k]));

export function findModule(name: string): MondContainer | undefined {
    return moduleMap.get(name);
}

export function findClass(name: string): MondContainer | undefined {
    return classMap.get(name);
}

export function findGlobal(name: string): MondSymbol | undefined {
    return globalMap.get(name);
}

export function findKeyword(name: string): MondSymbol | undefined {
    return keywordMap.get(name);
}

/**
 * Members that could exist on an unknown receiver - prototype methods plus the instance methods of
 * the standard library classes, deduplicated by name.
 */
export const instanceMembers: readonly (MondSymbol & { owners: string[] })[] = (() => {
    const byName = new Map<string, MondSymbol & { owners: string[] }>();

    for (const container of [...prototypes, ...classes]) {
        for (const member of container.members) {
            const existing = byName.get(member.name);
            if (existing) {
                existing.owners.push(container.name);
                continue;
            }
            byName.set(member.name, { ...member, owners: [container.name] });
        }
    }

    return [...byName.values()];
})();
