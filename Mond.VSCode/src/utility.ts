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

export function connect(endpoint: string): Promise<WebSocket> {
	return new Promise<WebSocket>((resolve, reject) => {
		const socket = new WebSocket(endpoint);

		socket.onopen = () => {
			socket.onopen = () => {};
			socket.onclose = () => {};
			socket.onerror = () => {};
			resolve(socket);
		};

		socket.onerror = e => {
			reject(new Error(`Failed to connect to WebSocket at ${endpoint} (${e.message})`));
			socket.close();
		};

		socket.onclose = () => reject(new Error(`Failed to connect to WebSocket at ${endpoint}`));
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
