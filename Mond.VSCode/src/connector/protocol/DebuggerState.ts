export interface InitialState {
    readonly type: 'initialState';
    readonly version: number;
    readonly isRunning: boolean;
}

export interface RunningState {
    readonly type: 'state';
    readonly isRunning: true;
}

export interface PausedState {
    readonly type: 'state';
    readonly isRunning: false;
    readonly stoppedOnBreakpoint: boolean;
}

export type DebuggerState = InitialState | RunningState | PausedState;
