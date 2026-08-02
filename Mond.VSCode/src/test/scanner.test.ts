import * as assert from 'assert';
import * as vscode from 'vscode';
import { TokenKind, flattenDeclarations, isInCommentOrString, scan, tokenize } from '../mondScanner';

function openMond(content: string): Thenable<vscode.TextDocument> {
	return vscode.workspace.openTextDocument({ language: 'mond', content });
}

describe('tokenize', () => {
	it('keeps nested block comments together', () => {
		const tokens = tokenize('/* a /* b */ c */ x');
		assert.strictEqual(tokens.length, 2);
		assert.strictEqual(tokens[0].kind, TokenKind.Comment);
		assert.strictEqual(tokens[0].text, '/* a /* b */ c */');
		assert.strictEqual(tokens[1].text, 'x');
	});

	it('handles escaped quotes inside strings', () => {
		const tokens = tokenize('"a\\"b" \'c\\\'d\'');
		assert.deepStrictEqual(tokens.map(t => t.text), ['"a\\"b"', "'c\\'d'"]);
		assert.ok(tokens.every(t => t.kind === TokenKind.String));
	});

	it('does not swallow member access after a number', () => {
		const tokens = tokenize('1.5 + x.length()');
		assert.deepStrictEqual(tokens.map(t => t.text), ['1.5', '+', 'x', '.', 'length', '(', ')']);
	});

	it('reads hex, binary and exponent literals as one token', () => {
		const tokens = tokenize('0xFF 0b1010 1e-7');
		assert.deepStrictEqual(tokens.map(t => t.text), ['0xFF', '0b1010', '1e-7']);
		assert.ok(tokens.every(t => t.kind === TokenKind.Number));
	});

	it('separates keywords from identifiers', () => {
		const tokens = tokenize('var variable = fun');
		assert.deepStrictEqual(tokens.map(t => t.kind), [
			TokenKind.Keyword,
			TokenKind.Identifier,
			TokenKind.Punctuation,
			TokenKind.Keyword,
		]);
	});

	it('reads arrow and spread as single tokens', () => {
		const tokens = tokenize('(a, ...rest) -> a');
		assert.ok(tokens.some(t => t.text === '...'));
		assert.ok(tokens.some(t => t.text === '->'));
	});
});

describe('scan', () => {
	it('finds nested declarations', async () => {
		const document = await openMond([
			'fun outer(a, b) {',
			'    var local = 1;',
			'    seq inner() { yield 1; }',
			'}',
			'const top = 2;',
		].join('\n'));

		const { declarations } = scan(document);
		assert.deepStrictEqual(declarations.map(d => d.name), ['outer', 'top']);
		assert.deepStrictEqual(declarations[0].parameters, ['a', 'b']);
		assert.deepStrictEqual(declarations[0].children.map(d => d.name), ['local', 'inner']);
		assert.strictEqual(declarations[1].kind, 'const');
	});

	it('handles expression bodied functions', async () => {
		const document = await openMond('fun add(a, b) -> a + b;\nvar x = 1;');

		const declarations = flattenDeclarations(scan(document).declarations);
		assert.deepStrictEqual(declarations.map(d => d.name), ['add', 'x']);
	});

	it('does not treat object literal keys as declarations', async () => {
		const document = await openMond('var obj = { a: 1, b: 2 };');

		const declarations = flattenDeclarations(scan(document).declarations);
		assert.deepStrictEqual(declarations.map(d => d.name), ['obj']);
	});

	it('reports offsets inside comments and strings', async () => {
		const content = 'var a = "text"; // note';
		const document = await openMond(content);

		assert.strictEqual(isInCommentOrString(document, content.indexOf('a')), false);
		assert.strictEqual(isInCommentOrString(document, content.indexOf('text')), true);
		assert.strictEqual(isInCommentOrString(document, content.indexOf('note')), true);
	});
});
