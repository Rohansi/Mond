import WebSocket from 'isomorphic-ws';
import { homedir } from 'os';
import * as vscode from 'vscode';
import { ValueType } from './connector/protocol/RpcResponses';

/** Supports the `${workspaceFolder}` and `${userHome}` variables so settings can be committed. */
export function resolveVariables(value: string, folder?: vscode.WorkspaceFolder): string {
	const target = folder ?? vscode.workspace.workspaceFolders?.[0];

	return value
		.replace(/\$\{workspaceFolder\}/g, target?.uri.fsPath ?? '')
		.replace(/\$\{userHome\}/g, homedir());
}

export function connect(endpoint: string, timeoutMs = 5000): Promise<WebSocket> {
	return new Promise<WebSocket>((resolve, reject) => {
		const socket = new WebSocket(endpoint);
		let settled = false;

		const settle = (action: () => void) => {
			if (settled) {
				return;
			}

			settled = true;
			clearTimeout(timer);
			socket.onopen = () => {};
			socket.onclose = () => {};
			socket.onerror = () => {};
			action();
		};

		// a listener that accepts the connection but never finishes the upgrade would otherwise leave
		// us waiting forever - the client has no handshake timeout of its own
		const timer = setTimeout(() => {
			settle(() => reject(new Error(`Timed out connecting to WebSocket at ${endpoint}`)));
			socket.close();
		}, timeoutMs);

		socket.onopen = () => settle(() => resolve(socket));

		socket.onerror = e => {
			settle(() => reject(new Error(`Failed to connect to WebSocket at ${endpoint} (${e.message})`)));
			socket.close();
		};

		socket.onclose = () => settle(() => reject(new Error(`Failed to connect to WebSocket at ${endpoint}`)));
	});
}

export function delay(ms: number) {
	return new Promise<void>(resolve => setTimeout(resolve, ms));
}

/** Turns whatever a `catch` produced into something safe to show a user. */
export function errorMessage(e: unknown): string {
	if (e instanceof Error) {
		return e.message;
	}

	if (typeof e === 'string') {
		return e;
	}

	return JSON.stringify(e) ?? 'unknown error';
}

export function buildIndexerValue(value: string, valueType: ValueType) {
	if (valueType === 'string') {
		return quoteString(value);
	} else {
		return value;
	}
}

export function quoteString(str: string) {
	const escaped = str.replace(/\\/g, '\\\\').replace(/"/g, '\\"');
	return `"${escaped}"`;
}

export function isComplexType(type: ValueType) {
	return type === 'object' || type === 'array';
}
