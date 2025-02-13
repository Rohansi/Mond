import { platform } from 'process';
import { exec } from 'child_process';
import WebSocket from 'isomorphic-ws';
import { ValueType } from './connector/protocol/RpcResponses';

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

export function findMondAsync() {
	//return 'C:\\Git\\Mond\\Mond.Repl\\bin\\Debug\\net8.0\\Mond.Repl.exe';

	const command = platform === 'win32'
		? 'where'
		: 'which';

	return new Promise<string>((resolve, reject) => {
		exec(`${command} mond`, (error, stdout) => {
			if (error) {
				reject(error);
			} else {
				resolve(stdout.trim());
			}
		});
	});
}

export function delay(ms: number) {
	return new Promise<void>(resolve => setTimeout(resolve, ms));
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
