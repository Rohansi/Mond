import { RpcError } from './RpcError';
import type { RpcResponse } from './protocol/RpcResponses';
import { TaskCompletionSource } from './TaskCompletionSource';
import { delay } from '../utility';

export class PendingCall extends TaskCompletionSource<RpcResponse> {
    constructor(public readonly method: string, public readonly seq: number) {
        super();
        this.method = method;
        this.seq = seq;
    }

    public async wait() {
        const result = await Promise.race([
            this.task,
            delay(10000),
        ]);

        if (!result) {
            throw new RpcError(this.method, this.seq, 'RPC timed out.');
        }

        if (result.status !== 'ok') {
            throw new RpcError(this.method, this.seq, result.error);
        }

        return result;
    }
}
