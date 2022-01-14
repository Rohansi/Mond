import { ChildProcessWithoutNullStreams, spawn } from 'child_process';
import { EventEmitter } from 'events';
import WebSocket from 'isomorphic-ws';
import { PendingCall } from './PendingCall';
import { RpcError } from './RpcError';
import { connect, delay, findMondAsync } from '../utility';

import type { DebuggerState } from './protocol/DebuggerState';
import type { RpcRequestTypeToResponse } from './protocol/RpcMapping';
import type { BreakpointTarget, RpcRequest } from './protocol/RpcRequests';
import type { BreakpointLocation, EvalResponse, RpcResponse, StackFrame } from './protocol/RpcResponses';

const protocolVersion = 1;

export class MondDebugRuntime extends EventEmitter {
	private _noDebug = false;
	private _socket: WebSocket | null = null;
    private _seq: number = 0;
	private _repl: ChildProcessWithoutNullStreams | null = null;
    private readonly _calls: Map<number, PendingCall> = new Map();

	constructor() {
		super();
	}

	public async getLaunchConfig(program: string, noDebug: boolean) {
		let mondPath: string | undefined = undefined;
		try {
			mondPath = await findMondAsync();
		} catch (e) {
			console.error(e);
			throw new Error(`Failed to locate Mond REPL: ${e}`);
		}

		if (!mondPath) {
			mondPath = undefined;
			throw new Error('Mond REPL not found on system - will not be able to run scripts');
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

		this._repl.on('error', e => {
			console.error('Mond REPL process error: ', e);
			this._repl?.kill();
			this.close();
		});

		this._repl.on('exit', (code, signal) => {
			console.log(`Mond REPL terminated (PID=${this._repl?.pid}, code=${code}, signal=${signal})`);
			this.close();
		});

		this._repl.stdout.on('data', data => {
			this.emit('output', 'stdout', data.toString());
		});

		this._repl.stderr.on('data', data => {
			this.emit('output', 'stderr', data.toString());
		});

		if (!noDebug) {
			for (let i = 0; i < 9; i++) {
				try {
					await this.attach();
					return;
				} catch {
					await delay(1000);
				}
			}

			await this.attach();
		}
	}

	public async attach(endpoint = 'ws://127.0.0.1:1597'): Promise<void> {
		const socket = await connect(endpoint);
		
		socket.onmessage = e => {
			if (typeof e.data === 'string') {
				this.handleMessage(e.data);
			}
		};

		socket.onclose = () => {
			this.close();
		};

		socket.onerror = e => {
			this.close();
		};

		this._socket = socket;
		this.emit('ready');
	}

	public close(terminate = false): void {
		this._socket?.close();
		this._socket = null;

		if (terminate) {
			this._repl?.kill();
		}
		this._repl = null;

		this.emit('end');
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
		if (this._noDebug) {
			return [];
		}

		const response = await this.call({ type: 'getBreakpointLocations', programPath, line, column, endLine, endColumn });
		return response.locations;
	}

	public async setBreakpoints(programPath: string, breakpoints: BreakpointTarget[]): Promise<[number, BreakpointLocation[]]> {
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

	private async handleMessage(data: string) {
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
					console.error(`Incompatible Mond debug protocol (expected ${protocolVersion}, got ${message.version})`);
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
