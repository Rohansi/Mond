import * as assert from 'assert';
import * as path from 'path';
import * as vscode from 'vscode';
import { locateMond } from '../mondLocator';

interface DapMessage {
	type?: string;
	event?: string;
	body?: unknown;
}

/**
 * Records the DAP traffic of a session so tests can wait for events the vscode.debug API does not
 * expose, like `stopped`.
 */
class MessageRecorder {
	private readonly _events: string[] = [];
	private readonly _waiters = new Map<string, () => void>();

	public record(message: DapMessage): void {
		if (message.type !== 'event' || !message.event) {
			return;
		}

		this._events.push(message.event);
		this._waiters.get(message.event)?.();
	}

	public wait(event: string, timeoutMs = 30000): Promise<void> {
		if (this._events.includes(event)) {
			return Promise.resolve();
		}

		return new Promise<void>((resolve, reject) => {
			const timer = setTimeout(() => {
				this._waiters.delete(event);
				reject(new Error(`Timed out waiting for the '${event}' event, saw: ${this._events.join(', ')}`));
			}, timeoutMs);

			this._waiters.set(event, () => {
				clearTimeout(timer);
				this._waiters.delete(event);
				resolve();
			});
		});
	}
}

function mondReplAvailable(): boolean {
	try {
		return !!locateMond();
	} catch {
		return false;
	}
}

describe('debug adapter', function () {
	let program: string;
	let available = false;

	before(function () {
		const folder = vscode.workspace.workspaceFolders?.[0];
		assert.ok(folder, 'tests must run with the sampleWorkspace folder open');
		program = path.join(folder.uri.fsPath, 'test.mnd');

		available = mondReplAvailable();
		if (!available) {
			// the adapter shells out to a built Mond.Repl, which is not available everywhere
			this.skip();
		}
	});

	afterEach(async () => {
		await vscode.debug.stopDebugging();
		vscode.debug.removeBreakpoints(vscode.debug.breakpoints);
	});

	it('stops on entry and reports a stack frame', async () => {
		const recorder = new MessageRecorder();
		const tracker = vscode.debug.registerDebugAdapterTrackerFactory('mond', {
			createDebugAdapterTracker: () => ({ onDidSendMessage: m => recorder.record(m as DapMessage) }),
		});

		try {
			const started = await vscode.debug.startDebugging(vscode.workspace.workspaceFolders?.[0], {
				type: 'mond',
				request: 'launch',
				name: 'Mond debug adapter test',
				program,
				stopOnEntry: true,
				trace: false,
			});
			assert.ok(started, 'the debug session failed to start');

			await recorder.wait('stopped');

			const session = vscode.debug.activeDebugSession;
			assert.ok(session, 'there is no active debug session');

			const stack = await session.customRequest('stackTrace', { threadId: 1 }) as {
				stackFrames: { name: string; line: number; source?: { name?: string } }[];
			};

			assert.ok(stack.stackFrames.length > 0, 'no stack frames were reported');
			assert.strictEqual(stack.stackFrames[0].source?.name, 'test.mnd');
			assert.strictEqual(stack.stackFrames[0].line, 1);
		} finally {
			tracker.dispose();
		}
	});

	it('binds a breakpoint and hits it', async () => {
		const recorder = new MessageRecorder();
		const tracker = vscode.debug.registerDebugAdapterTrackerFactory('mond', {
			createDebugAdapterTracker: () => ({ onDidSendMessage: m => recorder.record(m as DapMessage) }),
		});

		try {
			// `printLn('hello ' + i);` inside fun test
			const location = new vscode.Location(vscode.Uri.file(program), new vscode.Position(1, 4));
			vscode.debug.addBreakpoints([new vscode.SourceBreakpoint(location)]);

			const started = await vscode.debug.startDebugging(vscode.workspace.workspaceFolders?.[0], {
				type: 'mond',
				request: 'launch',
				name: 'Mond breakpoint test',
				program,
				stopOnEntry: false,
				trace: false,
			});
			assert.ok(started, 'the debug session failed to start');

			await recorder.wait('stopped');

			const session = vscode.debug.activeDebugSession;
			assert.ok(session, 'there is no active debug session');

			const stack = await session.customRequest('stackTrace', { threadId: 1 }) as {
				stackFrames: { name: string; line: number }[];
			};
			assert.strictEqual(stack.stackFrames[0].line, 2);

			const evaluated = await session.customRequest('evaluate', {
				expression: 'i',
				frameId: undefined,
				context: 'watch',
			}) as { result: string };
			assert.strictEqual(evaluated.result, '0');
		} finally {
			tracker.dispose();
		}
	});

	it('keeps standard library globals out of the local scope', async () => {
		const recorder = new MessageRecorder();
		const tracker = vscode.debug.registerDebugAdapterTrackerFactory('mond', {
			createDebugAdapterTracker: () => ({ onDidSendMessage: m => recorder.record(m as DapMessage) }),
		});

		try {
			const started = await vscode.debug.startDebugging(vscode.workspace.workspaceFolders?.[0], {
				type: 'mond',
				request: 'launch',
				name: 'Mond scope test',
				program,
				stopOnEntry: true,
				trace: false,
			});
			assert.ok(started, 'the debug session failed to start');

			await recorder.wait('stopped');

			const session = vscode.debug.activeDebugSession;
			assert.ok(session, 'there is no active debug session');

			const stack = await session.customRequest('stackTrace', { threadId: 1 }) as {
				stackFrames: { id: number }[];
			};
			const { scopes } = await session.customRequest('scopes', { frameId: stack.stackFrames[0].id }) as {
				scopes: { name: string; variablesReference: number }[];
			};

			const readScope = async (name: string) => {
				const scope = scopes.find(s => s.name === name);
				assert.ok(scope, `there is no ${name} scope`);
				const { variables } = await session.customRequest('variables', {
					variablesReference: scope.variablesReference,
				}) as { variables: { name: string; value: string; type?: string; variablesReference: number }[] };
				return variables;
			};

			// the standard libraries are declared globals, they are not slots in the frame - listing
			// them as locals used to show every one of them as `undefined`
			const locals = await readScope('Local');
			for (const name of ['Math', 'Json', 'Random', 'printLn']) {
				assert.ok(!locals.some(v => v.name === name), `${name} was reported as a local`);
			}

			const globals = await readScope('Global');
			const math = globals.find(v => v.name === 'Math');
			assert.ok(math, 'Math is missing from the Global scope');
			assert.notStrictEqual(math.value, 'undefined');
			assert.ok(math.variablesReference > 0, 'Math should be expandable');
		} finally {
			tracker.dispose();
		}
	});
});
