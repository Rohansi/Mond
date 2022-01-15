export type RpcResponse =
    RpcFailure |
    RpcSuccess |
    StackTraceResponse |
    SetBreakpointsResponse |
    GetBreakpointLocationsResponse |
    EvalResponse;

export interface RpcSuccess {
    readonly status: 'ok';
    readonly seq: number;
}

export interface RpcFailure {
    readonly status: 'error';
    readonly error: string;
    readonly seq: number;
}

export interface SetBreakpointsResponse extends RpcSuccess {
    readonly programId: number;
    readonly breakpoints: BreakpointLocation[];
}

export interface GetBreakpointLocationsResponse extends RpcSuccess {
    readonly programId: number;
    readonly locations: BreakpointLocation[];
}

export interface BreakpointLocation {
    readonly address: number;
    readonly line: number;
    readonly column?: number;
    readonly endLine?: number;
    readonly endColumn?: number;
}

export interface StackTraceResponse extends RpcSuccess {
    readonly stackFrames: StackFrame[];
}

export interface StackFrame {
    readonly programId: number;
    readonly address: number;
    readonly fileName: string;
    readonly function: string;
    readonly line: number;
    readonly column: number;
    readonly endLine?: number;
    readonly endColumn?: number;
}

export interface EvalResponse extends RpcSuccess {
    readonly value: string;
    readonly type: ValueType;
    readonly properties: EvalProperty[];
}

export interface EvalProperty {
    readonly name: string;
    readonly nameType: ValueType;
    readonly value: string;
    readonly valueType: ValueType;
}

export type ValueType = 'undefined' | 'null' | 'bool' | 'object' | 'array' | 'number' | 'string' | 'function';
