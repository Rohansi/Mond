import * as vscode from 'vscode';
import { activateMondDebug } from './activateMondDebug';
import { activateCompletionProvider } from "./activateCompletionProvider";
import { activateLanguageFeatures } from "./activateLanguageFeatures";

export function activate(context: vscode.ExtensionContext) {
	activateMondDebug(context);
	activateCompletionProvider(context);
	activateLanguageFeatures(context);
}

export function deactivate() {
	// nothing to do
}