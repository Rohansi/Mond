import { MondSymbol } from "./definitions";

/**
 * Keywords are part of the language rather than the standard library, so they are written by hand
 * here instead of coming out of the generated definition files.
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

const keywordMap = new Map(keywords.map(k => [k.name, k]));

export function findKeyword(name: string): MondSymbol | undefined {
    return keywordMap.get(name);
}
