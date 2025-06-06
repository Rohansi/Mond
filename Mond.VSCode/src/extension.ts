import * as vscode from 'vscode';
import { activateMondDebug } from './activateMondDebug';
import { activateCompletionProvider } from "./activateCompletionProvider";

export function activate(context: vscode.ExtensionContext) {
	activateMondDebug(context);
	activateCompletionProvider(context);
}

export function deactivate() {
	// nothing to do
}