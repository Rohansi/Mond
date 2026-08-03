import {
	Logger, logger,
	LoggingDebugSession,
	InitializedEvent, TerminatedEvent, StoppedEvent, OutputEvent,
	Thread, StackFrame, Source, ContinuedEvent, Breakpoint, Scope,
} from '@vscode/debugadapter';
import { DebugProtocol } from '@vscode/debugprotocol';
import { basename } from 'path';
import { MondDebugRuntime } from './connector/MondDebugRuntime';
import { buildIndexerValue, errorMessage, isComplexType } from './utility';
import { VariableHandles } from './VariableHandles';

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

interface IAttachRequestArguments extends DebugProtocol.AttachRequestArguments {
	/** WebSocket endpoint to attach to. */
	endpoint: string;
	/** enable logging the Debug Adapter Protocol */
	trace?: boolean;
}

const genericErrorId = 1001;
const frameNotSupportedErrorId = 1002;

/**
 * Everything the client attached to a breakpoint that the Mond runtime does not know about. The
 * runtime always stops, and the adapter decides whether that stop is worth reporting.
 */
interface BreakpointOptions {
	readonly condition?: string;
	readonly hitCondition?: string;
	readonly logMessage?: string;
}

export class MondDebugSession extends LoggingDebugSession {
	// we don't support multiple threads, so we can use a hardcoded ID for the default thread
	private static threadID = 1;

	private _runtime: MondDebugRuntime;
	private _variableHandles = new VariableHandles();
	private _launchedScript = false;
	private _stopOnEntry = false;

	/** Frame ids handed out by the last `stackTrace` request, in call stack order. */
	private _frameIds: number[] = [];
	private _nextFrameId = 1;

	/** Assignable expression for each variable we reported, keyed by reference and display name. */
	private _variableExpressions = new Map<string, string>();

	/** Conditions and log messages, keyed by the resolved position of the breakpoint. */
	private _breakpointOptions = new Map<string, BreakpointOptions>();

	/** Hit counts survive re-sent breakpoints so editing one does not reset the others. */
	private _breakpointHits = new Map<string, number>();

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
			this.invalidateStopState();
		});
		this._runtime.on('stopOnEntry', () => {
			this.invalidateStopState();
			if (!this._launchedScript || this._stopOnEntry) {
				this.sendEvent(new StoppedEvent('entry', MondDebugSession.threadID));
			}
		});
		this._runtime.on('stopOnStep', () => {
			this.invalidateStopState();
			this.sendEvent(new StoppedEvent('step', MondDebugSession.threadID));
		});
		this._runtime.on('stopOnBreakpoint', () => {
			this.invalidateStopState();
			void this.breakpointStop();
		});
		this._runtime.on('output', (type: string, data: string) => {
			this.sendEvent(new OutputEvent(data, type));
		});
	}

	protected initializeRequest(response: DebugProtocol.InitializeResponse, _args: DebugProtocol.InitializeRequestArguments): void {
		// build and return the capabilities of this debug adapter:
		response.body = response.body ?? {};

		response.body.supportsConfigurationDoneRequest = true;
		response.body.supportsTerminateRequest = true;
		response.body.supportTerminateDebuggee = true;
		response.body.supportsBreakpointLocationsRequest = true;
		response.body.supportsEvaluateForHovers = true;
		response.body.supportsSetVariable = true;
		response.body.supportsSetExpression = true;
		response.body.supportsDelayedStackTraceLoading = true;

		// the runtime has no notion of a condition, so the adapter evaluates them while it is stopped
		response.body.supportsConditionalBreakpoints = true;
		response.body.supportsHitConditionalBreakpoints = true;
		response.body.supportsLogPoints = true;
		
		this.sendResponse(response);
	}

	protected async configurationDoneRequest(
		response: DebugProtocol.ConfigurationDoneResponse,
		args: DebugProtocol.ConfigurationDoneArguments,
		request?: DebugProtocol.Request
	): Promise<void> {
		if (this._launchedScript && !this._stopOnEntry) {
			try {
				await this._runtime.continue();
			} catch (e) {
				console.error(e);
				this.sendEvent(new OutputEvent(`Failed to start the script: ${errorMessage(e)}\n`, 'stderr'));
			}
		}

		super.configurationDoneRequest(response, args, request);
	}

	protected async launchRequest(response: DebugProtocol.LaunchResponse, args: ILaunchRequestArguments) {
		try {
			// make sure to 'Stop' the buffered logging if 'trace' is not set
			logger.setup(args.trace ? Logger.LogLevel.Verbose : Logger.LogLevel.Stop, false);

			// start the program in the runtime
			this._launchedScript = true;
			this._stopOnEntry = !!args.stopOnEntry;
			await this._runtime.start(args.program, !!args.noDebug);
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async attachRequest(response: DebugProtocol.AttachResponse, args: IAttachRequestArguments): Promise<void> {
		try {
			// make sure to 'Stop' the buffered logging if 'trace' is not set
			logger.setup(args.trace ? Logger.LogLevel.Verbose : Logger.LogLevel.Stop, false);
			
			this._launchedScript = false;
			this._stopOnEntry = true;
			await this._runtime.attach(args.endpoint);
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async terminateRequest(response: DebugProtocol.TerminateResponse): Promise<void> {
		try {
			this._runtime.close(true);

			// the client treats this response as the end of the session, so do not report back until
			// the debuggee is really gone
			await this._runtime.waitForExit();
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async disconnectRequest(response: DebugProtocol.DisconnectResponse, args: DebugProtocol.DisconnectArguments): Promise<void> {
		try {
			this._runtime.close(args.terminateDebuggee);

			if (args.terminateDebuggee) {
				await this._runtime.waitForExit();
			}

			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async setBreakPointsRequest(
		response: DebugProtocol.SetBreakpointsResponse,
		args: DebugProtocol.SetBreakpointsArguments
	): Promise<void> {
		try {
			const path = this.convertClientPathToDebugger(args.source.path as string);

			const requested = args.breakpoints
				?? args.lines?.map(line => ({ line } as DebugProtocol.SourceBreakpoint))
				?? [];
			const breakpointRequests = requested.map(b => ({ line: b.line, column: b.column }));
			const [programId, createdBreakpoints] = await this._runtime.setBreakpoints(path, breakpointRequests);
			const source = this.createSource(programId, path);

			this.forgetBreakpointOptions(programId);

			// the runtime snaps breakpoints onto the nearest statement it can actually stop at, so report
			// the resolved position back instead of the requested one and let VS Code move the marker
			const breakpointResponses = breakpointRequests.map((req, i) => {
				const created = createdBreakpoints[i];
				if (!created) {
					return new Breakpoint(false, req.line, req.column, source);
				}

				// conditions are matched against where the runtime actually stops, not where the
				// breakpoint was requested, because those can be different lines
				this.rememberBreakpointOptions(programId, created.line, requested[i]);

				const breakpoint = new Breakpoint(true, created.line, created.column, source) as DebugProtocol.Breakpoint;
				breakpoint.endLine = created.endLine;
				breakpoint.endColumn = created.endColumn;
				return breakpoint;
			});

			response.body = {
				breakpoints: breakpointResponses,
			};
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async breakpointLocationsRequest(
		response: DebugProtocol.BreakpointLocationsResponse,
		args: DebugProtocol.BreakpointLocationsArguments,
	): Promise<void> {
		if (args.source.path) {
			try {
				const path = this.convertClientPathToDebugger(args.source.path);
	
				const locations = await this._runtime.getBreakpointLocations(path, args.line, args.column, args.endLine, args.endColumn);

				response.body = {
					breakpoints: locations,
				};
				this.sendResponse(response);
			} catch (e) {
				this.sendError(response, e);
			}
		} else {
			response.body = {
				breakpoints: []
			};
			this.sendResponse(response);
		}
	}

	protected async stackTraceRequest(
		response: DebugProtocol.StackTraceResponse,
		args: DebugProtocol.StackTraceArguments
	): Promise<void> {
		try {
			const stack = await this._runtime.stack();

			// ids must stay stable for as long as we are stopped - the client hands them back in
			// `scopes` and `evaluate`, and it may ask for the stack more than once per stop
			if (this._frameIds.length !== stack.length) {
				this._frameIds = stack.map(() => this._nextFrameId++);
			}

			// the runtime always hands over the whole stack, but the client asks for it a page at a
			// time so it can render the top frame before the rest arrives
			const startFrame = Math.max(0, args.startFrame ?? 0);
			const endFrame = args.levels ? startFrame + args.levels : stack.length;
			const page = stack.slice(startFrame, endFrame);

			response.body = {
				stackFrames: page.map((f, i) => {
					const sf = new StackFrame(this._frameIds[startFrame + i], f.function, this.createSource(f.programId, f.fileName), this.convertDebuggerLineToClient(f.line));
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
			this.sendError(response, e);
		}
	}

	protected async pauseRequest(response: DebugProtocol.PauseResponse): Promise<void> {
		try {
			await this._runtime.pause();
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async continueRequest(response: DebugProtocol.ContinueResponse): Promise<void> {
		try {
			await this._runtime.continue();
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async nextRequest(response: DebugProtocol.NextResponse): Promise<void> {
		try {
			await this._runtime.step();
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async stepInRequest(response: DebugProtocol.StepInResponse): Promise<void> {
		try {
			await this._runtime.stepIn();
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async stepOutRequest(response: DebugProtocol.StepOutResponse): Promise<void> {
		try {
			await this._runtime.stepOut();
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async evaluateRequest(response: DebugProtocol.EvaluateResponse, args: DebugProtocol.EvaluateArguments): Promise<void> {
		try {
			if (!this.isTopFrame(args.frameId)) {
				this.sendFrameNotSupported(response);
				return;
			}

			const result = await this._runtime.eval(args.expression);
			const hasChildren = isComplexType(result.type);

			response.body = {
				result: result.value,
				type: result.type,
				variablesReference: hasChildren
					? this._variableHandles.create(this.topFrameId, args.expression)
					: 0,
			};
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async variablesRequest(response: DebugProtocol.VariablesResponse, args: DebugProtocol.VariablesArguments) {
		try {
			const reference = this._variableHandles.get(args.variablesReference);

			if (!reference || !this.isTopFrame(reference.frameId)) {
				response.body = { variables: [] };
				this.sendResponse(response);
				return;
			}

			const { frameId, expression } = reference;
			const result = await this._runtime.eval(expression);
			const variables: DebugProtocol.Variable[] = result.properties.map(p => {
				const hasChildren = isComplexType(p.valueType);
				const subExpr = expression.length === 0 ? p.name : `(${expression})[${buildIndexerValue(p.name, p.nameType)}]`;
				const name = result.type === 'array' ? `[${p.name}]` : p.name;

				// the display name is not always the key, so remember how to get back to the value
				this._variableExpressions.set(`${args.variablesReference}\u0000${name}`, subExpr);

				return {
					name,
					value: p.value,
					type: p.valueType,
					variablesReference: hasChildren
						? this._variableHandles.create(frameId, subExpr)
						: 0,
				};
			});

			response.body = { variables };
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async setVariableRequest(
		response: DebugProtocol.SetVariableResponse,
		args: DebugProtocol.SetVariableArguments
	): Promise<void> {
		try {
			const reference = this._variableHandles.get(args.variablesReference);
			const expression = this._variableExpressions.get(`${args.variablesReference}\u0000${args.name}`);

			if (!reference || !expression) {
				this.sendErrorResponse(response, {
					id: genericErrorId,
					format: 'This variable can no longer be assigned to.',
				});
				return;
			}

			if (!this.isTopFrame(reference.frameId)) {
				this.sendFrameNotSupported(response);
				return;
			}

			response.body = await this.assign(expression, args.value);
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected async setExpressionRequest(
		response: DebugProtocol.SetExpressionResponse,
		args: DebugProtocol.SetExpressionArguments
	): Promise<void> {
		try {
			if (!this.isTopFrame(args.frameId)) {
				this.sendFrameNotSupported(response);
				return;
			}

			response.body = await this.assign(args.expression, args.value);
			this.sendResponse(response);
		} catch (e) {
			this.sendError(response, e);
		}
	}

	protected scopesRequest(response: DebugProtocol.ScopesResponse, args: DebugProtocol.ScopesArguments): void {
		const scopes: Scope[] = [];

		// the Mond debugger can only resolve locals for the frame it broke in, so callers get globals only
		if (this.isTopFrame(args.frameId)) {
			scopes.push(new Scope('Local', this._variableHandles.create(args.frameId, ''), false));
		}

		scopes.push(new Scope('Global', this._variableHandles.create(this.topFrameId, 'global'), true));

		response.body = { scopes };
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

	private get topFrameId(): number {
		return this._frameIds.length > 0 ? this._frameIds[0] : 0;
	}

	/** Frame ids are only handed out by `stackTrace`; an unset id means "wherever the debugger is". */
	private isTopFrame(frameId: number | undefined): boolean {
		return frameId === undefined || this._frameIds.length === 0 || frameId === this._frameIds[0];
	}

	/**
	 * Assignment is an expression in Mond, so writing a value and reading the result back is a
	 * single round trip through the same `eval` the Watch and Variables views already use.
	 */
	private async assign(expression: string, value: string): Promise<{ value: string; type: string; variablesReference: number }> {
		const result = await this._runtime.eval(`${expression} = (${value})`);

		return {
			value: result.value,
			type: result.type,
			variablesReference: isComplexType(result.type)
				? this._variableHandles.create(this.topFrameId, expression)
				: 0,
		};
	}

	/** Discards frame and variable handles that are no longer valid because execution moved. */
	private invalidateStopState(): void {
		this._frameIds = [];
		this._variableHandles.reset();
		this._variableExpressions.clear();
	}

	private breakpointKey(programId: number, line: number): string {
		return `${programId}:${line}`;
	}

	private forgetBreakpointOptions(programId: number): void {
		// the client re-sends every breakpoint in a source whenever any of them changes
		for (const key of [...this._breakpointOptions.keys()]) {
			if (key.startsWith(`${programId}:`)) {
				this._breakpointOptions.delete(key);
			}
		}
	}

	private rememberBreakpointOptions(programId: number, line: number, requested: DebugProtocol.SourceBreakpoint | undefined): void {
		if (!requested?.condition && !requested?.hitCondition && !requested?.logMessage) {
			return;
		}

		this._breakpointOptions.set(this.breakpointKey(programId, line), {
			condition: requested.condition,
			hitCondition: requested.hitCondition,
			logMessage: requested.logMessage,
		});
	}

	/**
	 * The runtime stops on every breakpoint. Conditions and log messages are applied here, while we
	 * are stopped and `eval` can see the locals, and execution resumes if the stop was not wanted.
	 */
	private async breakpointStop(): Promise<void> {
		try {
			if (await this.shouldReportStop()) {
				this.sendEvent(new StoppedEvent('breakpoint', MondDebugSession.threadID));
			} else {
				await this._runtime.continue();
			}
		} catch (e) {
			// failing to evaluate a condition is not a reason to silently skip the breakpoint
			this.sendEvent(new OutputEvent(`Breakpoint condition failed: ${errorMessage(e)}\n`, 'stderr'));
			this.sendEvent(new StoppedEvent('breakpoint', MondDebugSession.threadID));
		}
	}

	private async shouldReportStop(): Promise<boolean> {
		const stack = await this._runtime.stack();
		const top = stack[0];
		if (!top) {
			return true;
		}

		const key = this.breakpointKey(top.programId, top.line);
		const options = this._breakpointOptions.get(key);
		if (!options) {
			return true;
		}

		if (options.condition && !await this.isTruthy(options.condition)) {
			return false;
		}

		if (options.hitCondition) {
			const hits = (this._breakpointHits.get(key) ?? 0) + 1;
			this._breakpointHits.set(key, hits);

			if (!hitConditionMet(options.hitCondition, hits)) {
				return false;
			}
		}

		if (options.logMessage) {
			this.sendEvent(new OutputEvent(`${await this.formatLogMessage(options.logMessage)}\n`, 'stdout'));
			return false;
		}

		return true;
	}

	/** Lets the runtime decide what counts as true instead of guessing from the printed value. */
	private async isTruthy(condition: string): Promise<boolean> {
		const result = await this._runtime.eval(`!(!(${condition}))`);
		return result.value === 'true';
	}

	private async formatLogMessage(message: string): Promise<string> {
		let result = '';
		let i = 0;

		while (i < message.length) {
			const open = message.indexOf('{', i);
			if (open < 0) {
				result += message.slice(i);
				break;
			}

			result += message.slice(i, open);

			const close = findClosingBrace(message, open);
			if (close < 0) {
				result += message.slice(open);
				break;
			}

			const expression = message.slice(open + 1, close).trim();
			if (expression.length > 0) {
				try {
					result += (await this._runtime.eval(expression)).value;
				} catch (e) {
					// a broken interpolation should not cost you the rest of the message
					result += `{${errorMessage(e)}}`;
				}
			}

			i = close + 1;
		}

		return result;
	}

	private sendFrameNotSupported(response: DebugProtocol.Response): void {
		this.sendErrorResponse(response, {
			id: frameNotSupportedErrorId,
			format: 'Mond can only evaluate expressions in the topmost stack frame.',
		});
	}

	private sendError(response: DebugProtocol.Response, e: unknown): void {
		console.error(e);

		// the message is passed as a variable because sendErrorResponse treats the format as a template
		this.sendErrorResponse(response, {
			id: genericErrorId,
			format: '{_error}',
			variables: { _error: errorMessage(e) },
		});
	}

	private createSource(fileId: number, filePath: string): Source {
		return new Source(basename(filePath), this.convertDebuggerPathToClient(filePath), fileId, undefined, 'mond-adapter-data');
	}
}

const hitConditionPattern = /^\s*(>=|<=|==|=|>|<|%)?\s*(\d+)\s*$/;

/** Implements the `> 5`, `% 3`, `== 10` syntax the client offers for hit counts. */
function hitConditionMet(hitCondition: string, hits: number): boolean {
	const match = hitConditionPattern.exec(hitCondition);
	if (!match) {
		return true; // an unparseable condition should not swallow the breakpoint
	}

	const count = parseInt(match[2], 10);

	switch (match[1]) {
		case '<':
			return hits < count;
		case '<=':
			return hits <= count;
		case '>':
			return hits > count;
		case '==':
		case '=':
			return hits === count;
		case '%':
			return count > 0 && hits % count === 0;
		default:
			// a bare number means "stop once this many hits have happened, and every time after"
			return hits >= count;
	}
}

function findClosingBrace(text: string, open: number): number {
	let depth = 0;

	for (let i = open; i < text.length; i++) {
		if (text[i] === '{') {
			depth++;
		} else if (text[i] === '}' && --depth === 0) {
			return i;
		}
	}

	return -1;
}
