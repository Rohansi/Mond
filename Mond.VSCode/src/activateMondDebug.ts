import * as vscode from 'vscode';
import { WorkspaceFolder, DebugConfiguration, ProviderResult, CancellationToken } from 'vscode';
import { MondDebugSession } from './mondDebug';

export function activateMondDebug(context: vscode.ExtensionContext) {
	context.subscriptions.push(
		vscode.commands.registerCommand('extension.mond.runEditorContents', (resource: vscode.Uri) => {
			let targetResource = resource;
			if (!targetResource && vscode.window.activeTextEditor) {
				targetResource = vscode.window.activeTextEditor.document.uri;
			}
			if (targetResource) {
				vscode.debug.startDebugging(undefined, {
						type: 'mond',
						name: 'Run File',
						request: 'launch',
						program: targetResource.fsPath,
					},
					{ noDebug: true }
				);
			}
		}),
		vscode.commands.registerCommand('extension.mond.debugEditorContents', (resource: vscode.Uri) => {
			let targetResource = resource;
			if (!targetResource && vscode.window.activeTextEditor) {
				targetResource = vscode.window.activeTextEditor.document.uri;
			}
			if (targetResource) {
				vscode.debug.startDebugging(undefined, {
					type: 'mond',
					name: 'Debug File',
					request: 'launch',
					program: targetResource.fsPath
				});
			}
		}),
	);

	// register a configuration provider for 'mond' debug type
	const provider = new MondConfigurationProvider();
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider('mond', provider));

	const factory = new InlineDebugAdapterFactory();
	context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('mond', factory));
	if ('dispose' in factory) {
		context.subscriptions.push(factory);
	}
}

class MondConfigurationProvider implements vscode.DebugConfigurationProvider {

	/**
	 * Massage a debug configuration just before a debug session is being launched,
	 * e.g. add all missing attributes to the debug configuration.
	 */
	resolveDebugConfiguration(folder: WorkspaceFolder | undefined, config: DebugConfiguration, token?: CancellationToken): ProviderResult<DebugConfiguration> {
		
		const editor = vscode.window.activeTextEditor;
		if (editor && editor.document.languageId === 'mond') {
			// if launch.json is missing or empty
			if (!config.type) {
				config.type = 'mond';
			}
			if (!config.request) {
				config.request = 'launch';
				config.stopOnEntry = true;
			}
			if (!config.name) {
				config.name = config.request === 'launch' ? 'Launch' : 'Attach';
			}
			if (config.request === 'launch' && !config.program) {
				config.program = '${file}';
			}
			if (config.request === 'attach' && !config.endpoint) {
				config.endpoint = 'ws://127.0.0.1:1597';
			}
		}

		if (config.request === 'launch' && !config.program) {
			return vscode.window.showInformationMessage('Cannot find a program to debug').then(_ => {
				return undefined;	// abort launch
			});
		}

		return config;
	}
}

class InlineDebugAdapterFactory implements vscode.DebugAdapterDescriptorFactory {
	createDebugAdapterDescriptor(_session: vscode.DebugSession): ProviderResult<vscode.DebugAdapterDescriptor> {
		return new vscode.DebugAdapterInlineImplementation(new MondDebugSession());
	}
}
