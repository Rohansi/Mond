import * as assert from 'assert';
import * as vscode from 'vscode';

async function openMond(content: string): Promise<vscode.TextDocument> {
	const document = await vscode.workspace.openTextDocument({ language: 'mond', content });
	// the providers are contributed by the extension, so make sure it is running first
	await vscode.extensions.getExtension('Rohansi.mond-vscode')?.activate();
	return document;
}

function labelOf(item: vscode.CompletionItem): string {
	return typeof item.label === 'string' ? item.label : item.label.label;
}

describe('completion', () => {
	it('offers keywords, globals and locals at statement level', async () => {
		const document = await openMond('fun helper() { }\nvar counter = 0;\n');
		const position = new vscode.Position(2, 0);

		const list = await vscode.commands.executeCommand<vscode.CompletionList>(
			'vscode.executeCompletionItemProvider', document.uri, position);

		const labels = list.items.map(labelOf);
		assert.ok(labels.includes('helper'), 'local function missing');
		assert.ok(labels.includes('counter'), 'local variable missing');
		assert.ok(labels.includes('foreach'), 'keyword missing');
		assert.ok(labels.includes('printLn'), 'global missing');
		assert.ok(labels.includes('Math'), 'module missing');
	});

	it('offers module members after a dot, without the module prefix', async () => {
		const document = await openMond('Math.');
		const position = new vscode.Position(0, 5);

		const list = await vscode.commands.executeCommand<vscode.CompletionList>(
			'vscode.executeCompletionItemProvider', document.uri, position, '.');

		const labels = list.items.map(labelOf);
		assert.ok(labels.includes('abs'), 'Math.abs missing');
		assert.ok(labels.includes('PI'), 'Math.PI missing');
		assert.ok(!labels.includes('Math.abs'), 'members should not be qualified');
		assert.ok(!labels.includes('serialize'), 'unrelated module members leaked in');
	});

	it('offers prototype methods after an unknown receiver', async () => {
		const document = await openMond('var text = "hello";\ntext.');
		const position = new vscode.Position(1, 5);

		const list = await vscode.commands.executeCommand<vscode.CompletionList>(
			'vscode.executeCompletionItemProvider', document.uri, position, '.');

		const labels = list.items.map(labelOf);
		assert.ok(labels.includes('charCodeAt'), 'String method missing');
		assert.ok(labels.includes('removeAt'), 'Array method missing');
		assert.ok(labels.includes('getType'), 'Value method missing');
	});

	it('stays quiet inside comments', async () => {
		const document = await openMond('printLn(1);\n// write something here');
		const position = new vscode.Position(1, 12);

		const list = await vscode.commands.executeCommand<vscode.CompletionList>(
			'vscode.executeCompletionItemProvider', document.uri, position);

		// snippets and word based suggestions still apply, but none of ours should - the names
		// checked here must not appear in any other test document, or they come back as words
		const labels = list.items.map(labelOf);
		assert.ok(!labels.includes('foreach'), 'keywords leaked into a comment');
		assert.ok(!labels.includes('parseHex'), 'globals leaked into a comment');
	});
});

describe('document symbols', () => {
	it('produces a nested outline', async () => {
		const document = await openMond([
			'fun outer(a) {',
			'    var local = 1;',
			'}',
			'const answer = 42;',
		].join('\n'));

		const symbols = await vscode.commands.executeCommand<vscode.DocumentSymbol[]>(
			'vscode.executeDocumentSymbolProvider', document.uri);

		assert.deepStrictEqual(symbols.map(s => s.name), ['outer', 'answer']);
		assert.strictEqual(symbols[0].kind, vscode.SymbolKind.Function);
		assert.strictEqual(symbols[0].detail, '(a)');
		assert.deepStrictEqual(symbols[0].children.map(s => s.name), ['local']);
		assert.strictEqual(symbols[1].kind, vscode.SymbolKind.Constant);
	});
});

describe('folding', () => {
	it('folds blocks and region markers', async () => {
		const document = await openMond([
			'// #region setup',
			'fun outer() {',
			'    var a = 1;',
			'}',
			'// #endregion',
		].join('\n'));

		const ranges = await vscode.commands.executeCommand<vscode.FoldingRange[]>(
			'vscode.executeFoldingRangeProvider', document.uri);

		assert.ok(ranges.some(r => r.start === 1 && r.end === 2), 'function body not folded');
		assert.ok(
			ranges.some(r => r.start === 0 && r.end === 4 && r.kind === vscode.FoldingRangeKind.Region),
			'region not folded');
	});
});

describe('hover', () => {
	it('describes standard library members', async () => {
		const document = await openMond('Math.atan2(1, 2);');

		const hovers = await vscode.commands.executeCommand<vscode.Hover[]>(
			'vscode.executeHoverProvider', document.uri, new vscode.Position(0, 6));

		const text = hovers.flatMap(h => h.contents)
			.map(c => (typeof c === 'string' ? c : c.value))
			.join('\n');
		assert.ok(text.includes('Math.atan2(y: number, x: number): number'), `unexpected hover: ${text}`);
	});

	it('lists every overload', async () => {
		const document = await openMond('Math.log(1);');

		const hovers = await vscode.commands.executeCommand<vscode.Hover[]>(
			'vscode.executeHoverProvider', document.uri, new vscode.Position(0, 6));

		const text = hovers.flatMap(h => h.contents)
			.map(c => (typeof c === 'string' ? c : c.value))
			.join('\n');
		assert.ok(text.includes('Math.log(d: number): number'), `unexpected hover: ${text}`);
		assert.ok(text.includes('Math.log(d: number, b: number): number'), `unexpected hover: ${text}`);
	});

	it('describes local declarations', async () => {
		const document = await openMond('fun helper(a, b) { }\nhelper(1, 2);');

		const hovers = await vscode.commands.executeCommand<vscode.Hover[]>(
			'vscode.executeHoverProvider', document.uri, new vscode.Position(1, 2));

		const text = hovers.flatMap(h => h.contents)
			.map(c => (typeof c === 'string' ? c : c.value))
			.join('\n');
		assert.ok(text.includes('fun helper(a, b)'), `unexpected hover: ${text}`);
	});
});
