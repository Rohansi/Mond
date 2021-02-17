export class RpcError extends Error {
    public readonly method: string;
    public readonly seq: number;
    public readonly message: string;

    constructor(method: string, seq: number, message: string = 'unknown') {
        super(message);

        this.method = method;
        this.seq = seq;
        this.message = message;
    }
}
