import { ChildProcessWithoutNullStreams, spawn } from 'child_process';
import { EventEmitter } from 'events';
import WebSocket from 'isomorphic-ws';
import { PendingCall } from './PendingCall';
import { RpcError } from './RpcError';
import { connect, delay, errorMessage } from '../utility';
import { findMondAsync, MondNotFoundError } from '../mondLocator';

import type { DebuggerState } from './protocol/DebuggerState';
import type { RpcRequestTypeToResponse } from './protocol/RpcMapping';
import type { BreakpointTarget, RpcRequest } from './protocol/RpcRequests';
import type { BreakpointLocation, EvalResponse, RpcResponse, StackFrame } from './protocol/RpcResponses';

const protocolVersion = 2;

const defaultEndpoint = 'ws://127.0.0.1:1597';

/** How long to keep trying to reach the debugger inside a REPL we just spawned. */
const attachTimeoutMs = 20000;

/** Budget for one connection attempt, so a slow start does not stall the whole thing. */
const attachConnectTimeoutMs = 2000;

const attachRetryDelayMs = 250;

export class MondDebugRuntime extends EventEmitter {
	private _noDebug = false;
	private _closed = false;
	private _socket: WebSocket | null = null;
    private _seq: number = 0;
	private _repl: ChildProcessWithoutNullStreams | null = null;
	private _replExited: Promise<void> | null = null;
    private readonly _calls: Map<number, PendingCall> = new Map();

	constructor() {
		super();
	}

	/** True between attaching to the debugger and the connection dropping. */
	public get isConnected(): boolean {
		return this._socket !== null;
	}

	public async getLaunchConfig(program: string, noDebug: boolean) {
		let mondPath: string;
		try {
			mondPath = await findMondAsync();
		} catch (e) {
			console.error(e);

			// the locator already explained itself, so do not bury its message in a generic one
			if (e instanceof MondNotFoundError) {
				throw e;
			}

			throw new Error(`Failed to locate the Mond REPL: ${errorMessage(e)}`);
		}

		console.log(`Mond REPL found: ${mondPath}`);

		const args: string[] = [];

		if (!noDebug) {
			args.push('--debug');
			args.push('--wait'); // always wait so we can set breakpoints etc - we will resume after initialize
		}

		args.push(program);

		return {
			command: mondPath,
			args,
		};
	}

	public async start(program: string, noDebug: boolean): Promise<void> {
		this._noDebug = noDebug;

		const { command, args } = await this.getLaunchConfig(program, noDebug);
		this._repl = spawn(command, args, { windowsHide: true });
		console.log(`Spawned Mond REPL (PID=${this._repl.pid})`, command, args);

		this._replExited = new Promise<void>(resolve => this._repl?.once('exit', () => resolve()));

		this._repl.on('error', e => {
			console.error('Mond REPL process error: ', e);
			this.emit('output', 'stderr', `Failed to run Mond REPL: ${e.message}\n`);
			this._repl?.kill();
			this.close();
		});

		this._repl.on('exit', (code, signal) => {
			console.log(`Mond REPL terminated (PID=${this._repl?.pid}, code=${code}, signal=${signal})`);
			this.close();
		});

		this._repl.stdout.on('data', (data: Buffer) => {
			this.emit('output', 'stdout', data.toString());
		});

		this._repl.stderr.on('data', (data: Buffer) => {
			this.emit('output', 'stderr', data.toString());
		});

		if (!noDebug) {
			await this.attachToSpawnedRepl();
		}
	}

	/**
	 * The REPL needs a moment to start listening, and on a cold or loaded machine that moment can be
	 * several seconds, so keep retrying until it answers, dies, or we run out of patience.
	 */
	private async attachToSpawnedRepl(): Promise<void> {
		const deadline = Date.now() + attachTimeoutMs;
		let lastError: unknown;

		for (;;) {
			if (this._closed) {
				throw new Error('The Mond REPL exited before the debugger could attach.');
			}

			try {
				await this.attach(defaultEndpoint, attachConnectTimeoutMs);
				return;
			} catch (e) {
				// the early attempts are expected to fail, so only the last one is worth reporting
				lastError = e;
			}

			if (Date.now() >= deadline) {
				throw new Error(`Timed out waiting for the Mond REPL debugger: ${errorMessage(lastError)}`);
			}

			await delay(attachRetryDelayMs);
		}
	}

	public async attach(endpoint = defaultEndpoint, connectTimeoutMs?: number): Promise<void> {
		const socket = await connect(endpoint, connectTimeoutMs);
		
		socket.onmessage = e => {
			if (typeof e.data === 'string') {
				this.handleMessage(e.data);
			}
		};

		socket.onclose = () => {
			this.close();
		};

		socket.onerror = e => {
			console.error('Mond debugger connection error: ', e);
			this.emit('output', 'stderr', `Mond debugger connection error: ${e.message}\n`);
			this.close();
		};

		this._socket = socket;
		this.emit('ready');
	}

	/**
	 * Waits for a REPL we spawned to actually exit. The debugger listens on a fixed port, so the next
	 * session would otherwise connect to a process that is on its way out.
	 */
	public async waitForExit(timeoutMs = 5000): Promise<void> {
		if (this._replExited) {
			await Promise.race([this._replExited, delay(timeoutMs)]);
		}
	}

	public close(terminate = false): void {
		const socket = this._socket;
		this._socket = null;
		socket?.close();

		if (terminate && this._repl) {
			this._repl.kill();
			this._repl = null;
		}

		this.failPendingCalls('Debug session ended before a response was received.');

		if (this._closed) {
			return;
		}

		this._closed = true;
		this.emit('end');
	}

	public async pause() {
		if (this._noDebug) {
			return;
		}

		await this.call({ type: 'action', action: 'break' });
	}

	public async continue() {
		if (this._noDebug) {
			return;
		}
		
		await this.call({ type: 'action', action: 'continue' });
	}

	public async step() {
		if (this._noDebug) {
			return;
		}
		
		await this.call({ type: 'action', action: 'stepOver' });
	}

	public async stepIn() {
		if (this._noDebug) {
			return;
		}
		
		await this.call({ type: 'action', action: 'stepIn' });
	}

	public async stepOut() {
		if (this._noDebug) {
			return;
		}

		await this.call({ type: 'action', action: 'stepOut' });
	}

	public async stack(): Promise<StackFrame[]> {
		const stack = await this.call({ type: 'stackTrace' });
		return stack.stackFrames;
	}

	public async getBreakpointLocations(
		programPath: string,
		line: number,
		column?: number,
		endLine?: number,
		endColumn?: number,
	): Promise<BreakpointLocation[]> {
		// VS Code asks about breakpoints whenever the editor changes, which includes before we have
		// attached and after the session ended - there is nothing to report, and it is not an error
		if (this._noDebug || !this.isConnected) {
			return [];
		}

		const response = await this.call({ type: 'getBreakpointLocations', programPath, line, column, endLine, endColumn });
		return response.locations;
	}

	public async setBreakpoints(programPath: string, breakpoints: BreakpointTarget[]): Promise<[number, (BreakpointLocation | null)[]]> {
		// same as above - report them as unverified rather than failing the request
		if (this._noDebug || !this.isConnected) {
			return [-1, breakpoints.map(() => null)];
		}

		const response = await this.call({ type: 'setBreakpoints', programPath, breakpoints });
		return [response.programId, response.breakpoints];
	}

	public async eval(expression: string): Promise<EvalResponse> {
		return await this.call({ type: 'eval', expression });
	}

	private async call<TRequest extends RpcRequest>(
		request: TRequest,
	): Promise<RpcRequestTypeToResponse[TRequest['type']]> {
		const seq = this._seq++;

		if (!this._socket) {
			throw new RpcError(request.type, seq, 'Socket is not open');
		}

		try {
			const call = new PendingCall(request.type, seq);
			this._calls.set(seq, call);

			const requestWithSeq = { ...request, seq };
			const json = JSON.stringify(requestWithSeq);
			this._socket?.send(json);

			const response = await call.wait();
			return response as RpcRequestTypeToResponse[TRequest['type']];
		} finally {
			this._calls.delete(seq);
		}
	}

	private failPendingCalls(reason: string): void {
		if (this._calls.size === 0) {
			return;
		}

		const calls = [...this._calls.values()];
		this._calls.clear();

		for (const call of calls) {
			call.fail(new RpcError(call.method, call.seq, reason));
		}
	}

	private handleMessage(data: string): void {
		try {
			console.log(data);
			const message = JSON.parse(data) as (DebuggerState | RpcResponse);

			if ('seq' in message) {
				const call = this._calls.get(message.seq);
				if (call) {
					call.complete(message);
				} else {
					console.warn(`RPC response received for seq=${message.seq} but call was not found - did it time out?`);
				}

				return;
			}

			if (message.type === 'initialState') {
				if (message.version !== protocolVersion) {
					const error = `Incompatible Mond debug protocol (expected ${protocolVersion}, got ${message.version}). Update the Mond extension or the Mond runtime.`;
					console.error(error);
					this.emit('output', 'stderr', `${error}\n`);
					this.close();
					return;
				}

				if (!message.isRunning) {
					this.emit('stopOnEntry');
				} else {
					this.emit('continue');
				}

				return;
			}

			if (message.type === 'state') {
				if (!message.isRunning) {
					const event = message.stoppedOnBreakpoint ? 'stopOnBreakpoint' : 'stopOnStep';
					this.emit(event);
				} else {
					this.emit('continue');
				}

				return;
			}
			
			console.error('Mond debugger: unknown message:', message);
		} catch (e) {
			console.error('Mond debugger: error handling message:', data, e);
		}
	}
}
