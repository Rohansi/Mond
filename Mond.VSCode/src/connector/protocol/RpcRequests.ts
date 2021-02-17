export type RpcRequest =
    ActionRequest |
    StackTraceRequest |
    SetBreakpointsRequest |
    GetBreakpointLocationsRequest |
    EvalRequest;

export interface ActionRequest {
    readonly type: 'action';
    readonly action: 'break' | 'continue' | 'stepIn' | 'stepOver' | 'stepOut';
}

export interface StackTraceRequest {
    readonly type: 'stackTrace';
}

export interface SetBreakpointsRequest {
    readonly type: 'setBreakpoints';
    readonly programId?: number;
    readonly programPath?: string;
    readonly breakpoints: BreakpointTarget[];
}

export interface BreakpointTarget {
    readonly line: number;
    readonly column?: number;
}

export interface GetBreakpointLocationsRequest {
    readonly type: 'getBreakpointLocations';
    readonly programId?: number;
    readonly programPath?: string;
    readonly line: number;
    readonly column?: number;
    readonly endLine?: number;
    readonly endColumn?: number;
}

export interface EvalRequest {
    readonly type: 'eval';
    readonly expression: string;
}
