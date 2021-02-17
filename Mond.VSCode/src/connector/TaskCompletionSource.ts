export class TaskCompletionSource<T> {
    public readonly task: Promise<T>;
    private _resolve: (value: T) => void;
    private _reject: (error: Error) => void;

    constructor() {
        this._resolve = () => {};
        this._reject = () => {};
        
        this.task = new Promise<T>((resolve, reject) => {
            this._resolve = resolve;
            this._reject = reject;
        });
    }

    public complete(value: T) {
        this._resolve(value);
    }

    public fail(error: Error) {
        this._reject(error);
    }
}
