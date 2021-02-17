import type { RpcRequest } from './RpcRequests';
import type {
    RpcResponse,
    RpcSuccess,
    StackTraceResponse,
    SetBreakpointsResponse,
    GetBreakpointLocationsResponse,
    EvalResponse,
} from './RpcResponses';

type RequestTypeToResponseMap = {
    [key in RpcRequestType]: RpcResponse;
}

export type RpcRequestType = RpcRequest['type'];

export class RpcRequestTypeToResponse implements RequestTypeToResponseMap {
    public action!: RpcSuccess;
    public stackTrace!: StackTraceResponse;
    public setBreakpoints!: SetBreakpointsResponse;
    public getBreakpointLocations!: GetBreakpointLocationsResponse;
    public eval!: EvalResponse;
}
