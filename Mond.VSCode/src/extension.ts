import * as vscode from 'vscode';
import { activateMondDebug } from './activateMondDebug';

export function activate(context: vscode.ExtensionContext) {
	activateMondDebug(context);
}

export function deactivate() {
	// nothing to do
}
