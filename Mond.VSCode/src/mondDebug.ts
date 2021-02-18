import {
	Logger, logger,
	LoggingDebugSession,
	InitializedEvent, TerminatedEvent, StoppedEvent, OutputEvent,
	Thread, StackFrame, Source, ContinuedEvent, Breakpoint, Scope,
} from 'vscode-debugadapter';
import { DebugProtocol } from 'vscode-debugprotocol';
import { basename } from 'path';
import { MondDebugRuntime } from './connector/MondDebugRuntime';
import { buildIndexerValue, isComplexType } from './utility';
import { StringHandles } from './StringHandles';

/**
 * This interface describes the mock-debug specific launch attributes
 * (which are not part of the Debug Adapter Protocol).
 * The schema for these attributes lives in the package.json of the mock-debug extension.
 * The interface should always match this schema.
 */
interface ILaunchRequestArguments extends DebugProtocol.LaunchRequestArguments {
	/** An absolute path to the 'program' to debug. */
	program: string;
	/** Automatically stop target after launch. If not specified, target does not stop. */
	stopOnEntry?: boolean;
	/** enable logging the Debug Adapter Protocol */
	trace?: boolean;
	/** run without debugging */
	noDebug?: boolean;
}

export class MondDebugSession extends LoggingDebugSession {
	// we don't support multiple threads, so we can use a hardcoded ID for the default thread
	private static threadID = 1;

	private _runtime: MondDebugRuntime;

	private _variableHandles = new StringHandles();

	public constructor() {
		super();

		this.setDebuggerLinesStartAt1(true);
		this.setDebuggerColumnsStartAt1(true);
		
		this._runtime = new MondDebugRuntime();

		// setup event handlers
		this._runtime.on('ready', () => {
			this.sendEvent(new InitializedEvent());
		});
		this._runtime.on('end', () => {
			this.sendEvent(new TerminatedEvent());
		});
		this._runtime.on('continue', () => {
			this.sendEvent(new ContinuedEvent(MondDebugSession.threadID));
			this._variableHandles.reset();
		});
		this._runtime.on('stopOnEntry', () => {
			this.sendEvent(new StoppedEvent('entry', MondDebugSession.threadID));
		});
		this._runtime.on('stopOnStep', () => {
			this.sendEvent(new StoppedEvent('step', MondDebugSession.threadID));
		});
		this._runtime.on('stopOnBreakpoint', () => {
			this.sendEvent(new StoppedEvent('breakpoint', MondDebugSession.threadID));
		});
		this._runtime.on('output', data => {
			this.sendEvent(new OutputEvent(data, 'stdout'));
		});
	}

	protected initializeRequest(response: DebugProtocol.InitializeResponse, args: DebugProtocol.InitializeRequestArguments): void {
		// build and return the capabilities of this debug adapter:
		response.body = response.body ?? {};

		response.body.supportsConfigurationDoneRequest = true;
		response.body.supportsTerminateRequest = true;
		response.body.supportTerminateDebuggee = true;
		response.body.supportsBreakpointLocationsRequest = true;
		response.body.supportsEvaluateForHovers = true;

		// make VS Code support completion in REPL
		//response.body.supportsCompletionsRequest = true;
		//response.body.completionTriggerCharacters = [ '.', '[' ];

		this.sendResponse(response);
	}

	protected async launchRequest(response: DebugProtocol.LaunchResponse, args: ILaunchRequestArguments) {
		// make sure to 'Stop' the buffered logging if 'trace' is not set
		logger.setup(args.trace ? Logger.LogLevel.Verbose : Logger.LogLevel.Stop, false);

		// start the program in the runtime
		await this._runtime.start(args.program, !!args.stopOnEntry, !!args.noDebug);

		this.sendResponse(response);
	}

	protected terminateRequest(response: DebugProtocol.TerminateResponse, args: DebugProtocol.TerminateArguments, request?: DebugProtocol.Request): void {
		this._runtime.close(true);
		this.sendResponse(response);
	}

	protected disconnectRequest(response: DebugProtocol.DisconnectResponse, args: DebugProtocol.DisconnectArguments, request?: DebugProtocol.Request): void {
		this._runtime.close(args.terminateDebuggee);
		this.sendResponse(response);
	}

	protected async setBreakPointsRequest(
		response: DebugProtocol.SetBreakpointsResponse,
		args: DebugProtocol.SetBreakpointsArguments
	): Promise<void> {
		try {
			const path = this.convertClientPathToDebugger(args.source.path as string);

			const breakpointRequests = args.breakpoints?.map(b => ({ line: b.line, column: b.column }))
				?? args.lines?.map(l => ({ line: l }))
				?? [];

			const [programId, createdBreakpoints] = await this._runtime.setBreakpoints(path, breakpointRequests);

			const breakpointResponses = createdBreakpoints
				.map(b => new Breakpoint(true, b.line, b.column, this.createSource(programId, path)));

			response.body = {
				breakpoints: breakpointResponses,
			};
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected async breakpointLocationsRequest(
		response: DebugProtocol.BreakpointLocationsResponse,
		args: DebugProtocol.BreakpointLocationsArguments,
		request?: DebugProtocol.Request,
	): Promise<void> {
		if (args.source.path) {
			try {
				const path = this.convertClientPathToDebugger(args.source.path as string);
	
				const locations = await this._runtime.getBreakpointLocations(path, args.line, args.column, args.endLine, args.endColumn);

				response.body = {
					breakpoints: locations,
				};
				this.sendResponse(response);
			} catch (e) {
				this.sendErrorResponse(response, 0, e.message);
			}
		} else {
			response.body = {
				breakpoints: []
			};
			this.sendResponse(response);
		}
	}

	protected async stackTraceRequest(response: DebugProtocol.StackTraceResponse, args: DebugProtocol.StackTraceArguments): Promise<void> {
		try {
			const stack = await this._runtime.stack();

			response.body = {
				stackFrames: stack.map((f, i) => {
					const sf = new StackFrame(i, f.function, this.createSource(f.programId, f.fileName), this.convertDebuggerLineToClient(f.line));
					if (typeof f.column === 'number') {
						sf.column = this.convertDebuggerColumnToClient(f.column);
					}
					if (typeof f.endLine === 'number') {
						sf['endLine'] = this.convertDebuggerLineToClient(f.endLine);
					}
					if (typeof f.endColumn === 'number') {
						sf['endColumn'] = this.convertDebuggerColumnToClient(f.endColumn) + 1;
					}
					return sf;
				}),
				totalFrames: stack.length,
			};
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected async continueRequest(response: DebugProtocol.ContinueResponse, args: DebugProtocol.ContinueArguments): Promise<void> {
		try {
			await this._runtime.continue();
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected async nextRequest(response: DebugProtocol.NextResponse, args: DebugProtocol.NextArguments): Promise<void> {
		try {
			await this._runtime.step();
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected async stepInRequest(response: DebugProtocol.StepInResponse, args: DebugProtocol.StepInArguments): Promise<void> {
		try {
			await this._runtime.stepIn();
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected async stepOutRequest(response: DebugProtocol.StepOutResponse, args: DebugProtocol.StepOutArguments): Promise<void> {
		try {
			await this._runtime.stepOut();
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected async evaluateRequest(response: DebugProtocol.EvaluateResponse, args: DebugProtocol.EvaluateArguments): Promise<void> {
		try {
			const result = await this._runtime.eval(args.expression);
			const hasChildren = isComplexType(result.type);

			response.body = {
				result: result.value,
				type: result.type,
				variablesReference: hasChildren
					? this._variableHandles.create(args.expression)
					: 0,
			};
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected async variablesRequest(response: DebugProtocol.VariablesResponse, args: DebugProtocol.VariablesArguments, request?: DebugProtocol.Request) {
		try {
			const expression = this._variableHandles.get(args.variablesReference);

			if (typeof expression !== 'string') {
				response.body = { variables: [] };
				this.sendResponse(response);
				return;
			}

			const result = await this._runtime.eval(expression);
			const variables: DebugProtocol.Variable[] = result.properties.map(p => {
				const hasChildren = isComplexType(p.valueType);
				const subExpr = expression.length === 0 ? p.name : `(${expression})[${buildIndexerValue(p.name, p.nameType)}]`;
				const name = result.type === 'array' ? `[${p.name}]` : p.name;

				return {
					name,
					value: p.value,
					type: p.valueType,
					variablesReference: hasChildren
						? this._variableHandles.create(subExpr)
						: 0,
				};
			});

			response.body = { variables };
			this.sendResponse(response);
		} catch (e) {
			this.sendErrorResponse(response, 0, e.message);
		}
	}

	protected scopesRequest(response: DebugProtocol.ScopesResponse, args: DebugProtocol.ScopesArguments): void {
		response.body = {
			scopes: [
				new Scope('Local', this._variableHandles.create(''), false),
				new Scope('Global', this._variableHandles.create('global'), true)
			],
		};
		this.sendResponse(response);
	}

	protected threadsRequest(response: DebugProtocol.ThreadsResponse): void {
		response.body = {
			threads: [
				new Thread(MondDebugSession.threadID, 'Mond Thread')
			]
		};
		this.sendResponse(response);
	}

	//---- helpers

	private createSource(fileId: number, filePath: string): Source {
		return new Source(basename(filePath), this.convertDebuggerPathToClient(filePath), fileId, undefined, 'mond-adapter-data');
	}
}
