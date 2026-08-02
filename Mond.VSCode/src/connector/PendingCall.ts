import { RpcError } from './RpcError';
import type { RpcResponse } from './protocol/RpcResponses';
import { TaskCompletionSource } from './TaskCompletionSource';

const defaultTimeoutMs = 10000;

export class PendingCall extends TaskCompletionSource<RpcResponse> {
    constructor(public readonly method: string, public readonly seq: number) {
        super();
        this.method = method;
        this.seq = seq;
    }

    public async wait(timeoutMs = defaultTimeoutMs) {
        let timer: ReturnType<typeof setTimeout> | undefined;

        const timeout = new Promise<never>((_, reject) => {
            timer = setTimeout(() => reject(new RpcError(this.method, this.seq, 'RPC timed out.')), timeoutMs);
        });

        try {
            const result = await Promise.race([this.task, timeout]);

            if (result.status !== 'ok') {
                throw new RpcError(this.method, this.seq, result.error);
            }

            return result;
        } finally {
            clearTimeout(timer);
        }
    }
}
