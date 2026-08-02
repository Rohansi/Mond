const startHandle = 1000;

export interface VariableReference {
	/** The stack frame the expression should be evaluated in. */
	readonly frameId: number;
	/** The expression that produces the value, or an empty string for the frame's locals. */
	readonly expression: string;
}

export class VariableHandles {
	private _nextHandle: number;
	private _handleToReferenceMap: Map<number, VariableReference>;
	private _keyToHandleMap: Map<string, number>;

	public constructor() {
		this._nextHandle = startHandle;
		this._handleToReferenceMap = new Map<number, VariableReference>();
		this._keyToHandleMap = new Map<string, number>();
	}

	public reset(): void {
		this._nextHandle = startHandle;
		this._handleToReferenceMap.clear();
		this._keyToHandleMap.clear();
	}

	public create(frameId: number, expression: string): number {
		const key = `${frameId}\u0000${expression}`;

		const existingHandle = this._keyToHandleMap.get(key);
		if (typeof existingHandle === 'number') {
			return existingHandle;
		}

		const newHandle = this._nextHandle++;
		this._handleToReferenceMap.set(newHandle, { frameId, expression });
		this._keyToHandleMap.set(key, newHandle);
		return newHandle;
	}

	public get(handle: number): VariableReference | undefined {
		return this._handleToReferenceMap.get(handle);
	}
}
