import * as assert from 'assert';
import { mkdtempSync, rmSync, writeFileSync } from 'fs';
import { tmpdir } from 'os';
import * as path from 'path';
import * as vscode from 'vscode';

/** Extra definitions are read from disk, so the fixtures have to be real files. */
function writeDefinitions(contents: unknown): string {
	const directory = mkdtempSync(path.join(tmpdir(), 'mond-definitions-'));
	const file = path.join(directory, 'definitions.json');
	writeFileSync(file, JSON.stringify(contents), 'utf8');
	return file;
}

async function updateSetting(key: string, value: unknown): Promise<void> {
	await vscode.workspace.getConfiguration('mond.definitions')
		.update(key, value, vscode.ConfigurationTarget.Workspace);
}

async function completionsFor(content: string, position: vscode.Position, trigger?: string): Promise<string[]> {
	const document = await vscode.workspace.openTextDocument({ language: 'mond', content });
	await vscode.extensions.getExtension('Rohansi.mond-vscode')?.activate();

	const list = await vscode.commands.executeCommand<vscode.CompletionList>(
		'vscode.executeCompletionItemProvider', document.uri, position, trigger);

	return list.items.map(i => (typeof i.label === 'string' ? i.label : i.label.label));
}

describe('definitions', () => {
	const created: string[] = [];

	afterEach(async () => {
		await updateSetting('paths', undefined);
		await updateSetting('includeStandardLibrary', undefined);

		for (const file of created.splice(0)) {
			rmSync(path.dirname(file), { recursive: true, force: true });
		}
	});

	it('picks up globals from an extra file', async () => {
		const file = writeDefinitions({
			version: 1,
			globals: [
				{ name: 'hostLog', kind: 'function', signatures: ['hostLog(message: string)'] },
				{
					name: 'Host', kind: 'module', members: [
						{ name: 'quit', kind: 'method', signatures: ['quit(code: number)'] },
					],
				},
			],
		});
		created.push(file);
		await updateSetting('paths', [file]);

		const labels = await completionsFor('', new vscode.Position(0, 0));
		assert.ok(labels.includes('hostLog'), 'custom global missing');
		assert.ok(labels.includes('Host'), 'custom module missing');

		const members = await completionsFor('Host.', new vscode.Position(0, 5), '.');
		assert.ok(members.includes('quit'), 'custom module member missing');
	});

	it('lets an extra file override the bundled definitions', async () => {
		const file = writeDefinitions({
			version: 1,
			globals: [
				{
					name: 'Math', kind: 'module', members: [
						{ name: 'tau', kind: 'constant' },
					],
				},
			],
		});
		created.push(file);
		await updateSetting('paths', [file]);

		const members = await completionsFor('Math.', new vscode.Position(0, 5), '.');
		assert.ok(members.includes('tau'), 'added member missing');
		assert.ok(members.includes('abs'), 'members should be merged, not replaced');
	});

	it('drops the standard library when it is disabled', async () => {
		await updateSetting('includeStandardLibrary', false);

		// the names checked here must not appear in any other test document, or word based
		// suggestions bring them back regardless of what we provide
		const labels = await completionsFor('', new vscode.Position(0, 0));
		assert.ok(!labels.includes('proxyCreate'), 'standard library global still offered');
		assert.ok(labels.includes('foreach'), 'keywords should not be affected');
	});

	it('ignores a file that claims a newer format', async () => {
		const file = writeDefinitions({
			version: 999,
			globals: [{ name: 'fromTheFuture', kind: 'function' }],
		});
		created.push(file);
		await updateSetting('paths', [file]);

		const labels = await completionsFor('', new vscode.Position(0, 0));
		assert.ok(!labels.includes('fromTheFuture'), 'unsupported format was loaded anyway');
	});
});
