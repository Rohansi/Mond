import * as vscode from 'vscode';
import { activateMondDebug } from './activateMondDebug';
import { activateCompletionProvider } from "./activateCompletionProvider";
import { activateLanguageFeatures } from "./activateLanguageFeatures";
import { activateDebugFeatures } from "./activateDebugFeatures";
import { activateDefinitions } from "./definitions";

export function activate(context: vscode.ExtensionContext) {
	activateDefinitions(context);
	activateMondDebug(context);
	activateCompletionProvider(context);
	activateLanguageFeatures(context);
	activateDebugFeatures(context);
}

export function deactivate() {
	// nothing to do
}