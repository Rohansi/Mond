import * as vscode from 'vscode';
import { keywordNames } from './completionItems';

export enum TokenKind {
	Comment,
	String,
	Number,
	Identifier,
	Keyword,
	Punctuation,
}
export interface Token {
	readonly kind: TokenKind;
	readonly text: string;
	readonly start: number;
	readonly end: number;
}

export type DeclarationKind = 'fun' | 'seq' | 'var' | 'const' | 'parameter';

export interface Declaration {
	readonly kind: DeclarationKind;
	readonly name: string;
	readonly parameters?: string[];
	/** Range of the whole declaration, including a function body. */
	readonly start: number;
	readonly end: number;
	/** Range of just the name, for reveal-on-click. */
	readonly nameStart: number;
	readonly nameEnd: number;
	readonly children: Declaration[];
}

export interface ScanResult {
	readonly tokens: Token[];
	/** Index of the token closing the bracket opened at this index, and vice versa. */
	readonly brackets: Map<number, number>;
	readonly declarations: Declaration[];
}

const keywordSet = new Set<string>(keywordNames);
const openBrackets = '([{';
const closeBrackets = ')]}';

function isIdentifierStart(c: string) {
	return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c === '_';
}

function isIdentifierPart(c: string) {
	return isIdentifierStart(c) || (c >= '0' && c <= '9');
}

function isDigit(c: string) {
	return c >= '0' && c <= '9';
}

/**
 * Splits Mond source into tokens. This is deliberately forgiving - it never throws on malformed
 * input, because it runs against whatever half-written code is in the editor.
 */
export function tokenize(text: string): Token[] {
	const tokens: Token[] = [];
	let i = 0;

	while (i < text.length) {
		const c = text[i];

		if (c === ' ' || c === '\t' || c === '\r' || c === '\n') {
			i++;
			continue;
		}

		if (c === '/' && text[i + 1] === '/') {
			const start = i;
			while (i < text.length && text[i] !== '\n') {
				i++;
			}
			tokens.push({ kind: TokenKind.Comment, text: text.slice(start, i), start, end: i });
			continue;
		}

		if (c === '/' && text[i + 1] === '*') {
			const start = i;
			let depth = 0;
			while (i < text.length) {
				if (text[i] === '/' && text[i + 1] === '*') {
					depth++;
					i += 2;
					continue;
				}
				if (text[i] === '*' && text[i + 1] === '/') {
					depth--;
					i += 2;
					if (depth === 0) {
						break;
					}
					continue;
				}
				i++;
			}
			tokens.push({ kind: TokenKind.Comment, text: text.slice(start, i), start, end: i });
			continue;
		}

		if (c === '"' || c === '\'') {
			const start = i;
			i++;
			while (i < text.length) {
				if (text[i] === '\\') {
					i += 2;
					continue;
				}
				if (text[i] === c) {
					i++;
					break;
				}
				i++;
			}
			tokens.push({ kind: TokenKind.String, text: text.slice(start, i), start, end: i });
			continue;
		}

		if (isDigit(c)) {
			const start = i;
			i++;
			while (i < text.length && (isIdentifierPart(text[i]) || text[i] === '.')) {
				// only consume a dot that continues the number, not a member access
				if (text[i] === '.' && !isDigit(text[i + 1])) {
					break;
				}
				// an exponent sign is part of the literal
				if ((text[i] === 'e' || text[i] === 'E') && (text[i + 1] === '+' || text[i + 1] === '-')) {
					i++;
				}
				i++;
			}
			tokens.push({ kind: TokenKind.Number, text: text.slice(start, i), start, end: i });
			continue;
		}

		if (isIdentifierStart(c)) {
			const start = i;
			while (i < text.length && isIdentifierPart(text[i])) {
				i++;
			}
			const word = text.slice(start, i);
			tokens.push({
				kind: keywordSet.has(word) ? TokenKind.Keyword : TokenKind.Identifier,
				text: word,
				start,
				end: i,
			});
			continue;
		}

		// multi character operators only matter to us as a unit for '->' and '...'
		if (text.startsWith('->', i) || text.startsWith('...', i)) {
			const length = c === '-' ? 2 : 3;
			tokens.push({ kind: TokenKind.Punctuation, text: text.substr(i, length), start: i, end: i + length });
			i += length;
			continue;
		}

		tokens.push({ kind: TokenKind.Punctuation, text: c, start: i, end: i + 1 });
		i++;
	}

	return tokens;
}

function matchBrackets(tokens: Token[]): Map<number, number> {
	const result = new Map<number, number>();
	const stack: number[] = [];

	for (let i = 0; i < tokens.length; i++) {
		const token = tokens[i];
		if (token.kind !== TokenKind.Punctuation || token.text.length !== 1) {
			continue;
		}

		if (openBrackets.includes(token.text)) {
			stack.push(i);
			continue;
		}

		const closeIndex = closeBrackets.indexOf(token.text);
		if (closeIndex < 0) {
			continue;
		}

		// discard any unclosed brackets opened since the matching one
		for (let j = stack.length - 1; j >= 0; j--) {
			if (tokens[stack[j]].text === openBrackets[closeIndex]) {
				result.set(stack[j], i);
				result.set(i, stack[j]);
				stack.length = j;
				break;
			}
		}
	}

	return result;
}

function collectDeclarations(tokens: Token[], brackets: Map<number, number>, from: number, to: number): Declaration[] {
	const result: Declaration[] = [];
	let i = from;

	const skip = (index: number) => {
		const close = brackets.get(index);
		return close !== undefined && close < to ? close + 1 : index + 1;
	};

	while (i < to) {
		const token = tokens[i];

		if (token.kind === TokenKind.Keyword && (token.text === 'fun' || token.text === 'seq')) {
			let j = i + 1;

			const nameToken = tokens[j]?.kind === TokenKind.Identifier ? tokens[j] : undefined;
			if (nameToken) {
				j++;
			}

			let parameters: string[] = [];
			if (tokens[j]?.text === '(') {
				const close = brackets.get(j);
				if (close === undefined || close >= to) {
					i = j + 1;
					continue;
				}
				parameters = tokens
					.slice(j + 1, close)
					.filter(t => t.kind === TokenKind.Identifier)
					.map(t => t.text);
				j = close + 1;
			}

			if (!nameToken) {
				i = j;
				continue;
			}

			let end: number;
			let children: Declaration[] = [];

			if (tokens[j]?.text === '{') {
				const close = brackets.get(j);
				if (close !== undefined && close < to) {
					end = tokens[close].end;
					children = collectDeclarations(tokens, brackets, j + 1, close);
					i = close + 1;
				} else {
					end = tokens[to - 1]?.end ?? token.end;
					i = to;
				}
			} else {
				// expression bodied function - runs until the terminating semicolon
				let k = j;
				while (k < to && tokens[k].text !== ';') {
					k = skip(k);
				}
				end = tokens[Math.min(k, to - 1)]?.end ?? token.end;
				i = k + 1;
			}

			result.push({
				kind: token.text === 'seq' ? 'seq' : 'fun',
				name: nameToken.text,
				parameters,
				start: token.start,
				end,
				nameStart: nameToken.start,
				nameEnd: nameToken.end,
				children,
			});
			continue;
		}

		if (token.kind === TokenKind.Keyword && (token.text === 'var' || token.text === 'const')) {
			let k = i + 1;
			let expectName = true;

			while (k < to && tokens[k].text !== ';') {
				if (brackets.has(k)) {
					k = skip(k);
					expectName = false;
					continue;
				}

				if (expectName && tokens[k].kind === TokenKind.Identifier) {
					result.push({
						kind: token.text === 'const' ? 'const' : 'var',
						name: tokens[k].text,
						start: tokens[k].start,
						end: tokens[k].end,
						nameStart: tokens[k].start,
						nameEnd: tokens[k].end,
						children: [],
					});
					expectName = false;
				} else if (tokens[k].text === ',') {
					expectName = true;
				}

				k++;
			}

			i = k + 1;
			continue;
		}

		if (brackets.has(i)) {
			const close = brackets.get(i) as number;
			if (close < to) {
				// nested blocks aren't scopes we model, so their declarations are hoisted up
				result.push(...collectDeclarations(tokens, brackets, i + 1, close));
				i = close + 1;
				continue;
			}
		}

		i++;
	}

	return result;
}

const cache = new Map<string, { version: number; result: ScanResult }>();

export function scan(document: vscode.TextDocument): ScanResult {
	const key = document.uri.toString();
	const cached = cache.get(key);
	if (cached && cached.version === document.version) {
		return cached.result;
	}

	const tokens = tokenize(document.getText());
	const brackets = matchBrackets(tokens);
	const result: ScanResult = {
		tokens,
		brackets,
		declarations: collectDeclarations(tokens, brackets, 0, tokens.length),
	};

	cache.set(key, { version: document.version, result });
	return result;
}

export function forgetDocument(document: vscode.TextDocument) {
	cache.delete(document.uri.toString());
}

/** True when the offset is inside a comment or a string, where suggestions are just noise. */
export function isInCommentOrString(document: vscode.TextDocument, offset: number): boolean {
	const { tokens } = scan(document);

	let low = 0;
	let high = tokens.length - 1;

	while (low <= high) {
		const mid = (low + high) >> 1;
		const token = tokens[mid];

		if (offset <= token.start) {
			high = mid - 1;
		} else if (offset > token.end) {
			low = mid + 1;
		} else {
			return token.kind === TokenKind.Comment || token.kind === TokenKind.String;
		}
	}

	return false;
}

export function flattenDeclarations(declarations: readonly Declaration[], result: Declaration[] = []): Declaration[] {
	for (const declaration of declarations) {
		result.push(declaration);
		flattenDeclarations(declaration.children, result);
	}
	return result;
}
