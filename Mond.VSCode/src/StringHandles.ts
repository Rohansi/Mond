const startHandle = 1000;

export class StringHandles {
	private _nextHandle: number;
	private _handleToStringMap: Map<number, string>;
	private _stringToHandleMap: Map<string, number>;

	public constructor() {
		this._nextHandle = startHandle;
        this._handleToStringMap = new Map<number, string>();
		this._stringToHandleMap = new Map<string, number>();
	}

	public reset(): void {
		this._nextHandle = startHandle;
		this._handleToStringMap.clear();
		this._stringToHandleMap.clear();
	}

	public create(value: string): number {
		const existingHandle = this._stringToHandleMap.get(value);
		if (typeof existingHandle === 'number') {
			return existingHandle;
		}

		const newHandle = this._nextHandle++;
		this._handleToStringMap.set(newHandle, value);
		this._stringToHandleMap.set(value, newHandle);
		return newHandle;
	}

	public get(handle: number): string | undefined {
		return this._handleToStringMap.get(handle);
	}
}
