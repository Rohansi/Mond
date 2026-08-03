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

/** Turns a hang into a failure that says what it was waiting for, rather than a bare mocha timeout. */
function withTimeout<T>(promise: Thenable<T>, timeoutMs: number, what: string): Promise<T> {
	return new Promise<T>((resolve, reject) => {
		const timer = setTimeout(() => reject(new Error(`Timed out ${what}.`)), timeoutMs);

		promise.then(
			value => {
				clearTimeout(timer);
				resolve(value);
			},
			(error: unknown) => {
				clearTimeout(timer);
				reject(error instanceof Error ? error : new Error(String(error)));
			});
	});
}

function waitForTermination(session: vscode.DebugSession, timeoutMs = 15000): Promise<void> {
	return new Promise<void>((resolve, reject) => {
		const timer = setTimeout(() => {
			subscription.dispose();
			reject(new Error(`Timed out waiting for the '${session.name}' session to terminate.`));
		}, timeoutMs);

		const subscription = vscode.debug.onDidTerminateDebugSession(ended => {
			if (ended.id !== session.id) {
				return;
			}

			clearTimeout(timer);
			subscription.dispose();
			resolve();
		});
	});
}

describe('debug adapter', function () {
	/** `printLn('hello ' + i);` inside `fun test`, which the loop below it runs a hundred times. */
	const insideTest = new vscode.Position(1, 4);

	let program: string;
	let tracker: vscode.Disposable | undefined;
	let recorder = new MessageRecorder();

	before(function () {
		const folder = vscode.workspace.workspaceFolders?.[0];
		assert.ok(folder, 'tests must run with the sampleWorkspace folder open');
		program = path.join(folder.uri.fsPath, 'test.mnd');

		if (!mondReplAvailable()) {
			// the adapter shells out to a built Mond.Repl, which is not available everywhere
			this.skip();
		}

		tracker = vscode.debug.registerDebugAdapterTrackerFactory('mond', {
			createDebugAdapterTracker: () => ({ onDidSendMessage: m => recorder.record(m as DapMessage) }),
		});
	});

	after(() => {
		tracker?.dispose();
	});

	beforeEach(() => {
		recorder = new MessageRecorder();
	});

	afterEach(async () => {
		// the debugger listens on a fixed port, so the next test cannot start until this session and
		// the process behind it are really gone
		const session = vscode.debug.activeDebugSession;
		const terminated = session ? waitForTermination(session) : Promise.resolve();

		await vscode.debug.stopDebugging();
		await terminated;

		vscode.debug.removeBreakpoints(vscode.debug.breakpoints);
	});

	/** Launches `test.mnd` and resolves once the debugger reports that it stopped. */
	async function debugUntilStopped(name: string, config: Record<string, unknown> = {}): Promise<vscode.DebugSession> {
		const started = await withTimeout(
			vscode.debug.startDebugging(vscode.workspace.workspaceFolders?.[0], {
				type: 'mond',
				request: 'launch',
				name,
				program,
				stopOnEntry: false,
				trace: false,
				...config,
			}),
			30000,
			'starting the debug session - VS Code may be waiting on a dialog');

		assert.ok(started, 'the debug session failed to start');

		await recorder.wait('stopped');

		const session = vscode.debug.activeDebugSession;
		assert.ok(session, 'there is no active debug session');
		return session;
	}

	function breakOn(position: vscode.Position, condition?: string): void {
		const location = new vscode.Location(vscode.Uri.file(program), position);
		vscode.debug.addBreakpoints([new vscode.SourceBreakpoint(location, true, condition)]);
	}

	async function evaluate(session: vscode.DebugSession, expression: string): Promise<string> {
		const { result } = await session.customRequest('evaluate', { expression, context: 'watch' }) as { result: string };
		return result;
	}

	async function readScopes(session: vscode.DebugSession) {
		const { stackFrames } = await session.customRequest('stackTrace', { threadId: 1 }) as {
			stackFrames: { id: number }[];
		};
		const { scopes } = await session.customRequest('scopes', { frameId: stackFrames[0].id }) as {
			scopes: { name: string; variablesReference: number }[];
		};
		return scopes;
	}

	it('stops on entry and reports a stack frame', async () => {
		const session = await debugUntilStopped('Mond debug adapter test', { stopOnEntry: true });

		const stack = await session.customRequest('stackTrace', { threadId: 1 }) as {
			stackFrames: { name: string; line: number; source?: { name?: string } }[];
		};

		assert.ok(stack.stackFrames.length > 0, 'no stack frames were reported');
		assert.strictEqual(stack.stackFrames[0].source?.name, 'test.mnd');
		assert.strictEqual(stack.stackFrames[0].line, 1);
	});

	it('binds a breakpoint and hits it', async () => {
		breakOn(insideTest);

		const session = await debugUntilStopped('Mond breakpoint test');

		const stack = await session.customRequest('stackTrace', { threadId: 1 }) as {
			stackFrames: { line: number }[];
		};
		assert.strictEqual(stack.stackFrames[0].line, 2);
		assert.strictEqual(await evaluate(session, 'i'), '0');
	});

	it('keeps standard library globals out of the local scope', async () => {
		const session = await debugUntilStopped('Mond scope test', { stopOnEntry: true });
		const scopes = await readScopes(session);

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
	});

	it('skips a conditional breakpoint until the condition holds', async () => {
		breakOn(insideTest, 'i == 5');

		const session = await debugUntilStopped('Mond conditional breakpoint test');

		assert.strictEqual(await evaluate(session, 'i'), '5', 'the debugger stopped on the wrong iteration');
	});

	it('assigns a new value to a local', async () => {
		breakOn(insideTest);

		const session = await debugUntilStopped('Mond set variable test');
		const locals = (await readScopes(session)).find(s => s.name === 'Local');
		assert.ok(locals, 'there is no Local scope');

		// the variables have to be listed first - that is what teaches the adapter how to get back
		// to the value behind a display name
		await session.customRequest('variables', { variablesReference: locals.variablesReference });

		const assigned = await session.customRequest('setVariable', {
			variablesReference: locals.variablesReference,
			name: 'i',
			value: '42',
		}) as { value: string };
		assert.strictEqual(assigned.value, '42');
		assert.strictEqual(await evaluate(session, 'i'), '42', 'the assignment did not stick');
	});
});
