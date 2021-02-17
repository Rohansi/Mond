import * as vscode from 'vscode';
import { WorkspaceFolder, DebugConfiguration, ProviderResult, CancellationToken } from 'vscode';
import { MondDebugSession } from './mondDebug';

export function activateMondDebug(context: vscode.ExtensionContext, factory?: vscode.DebugAdapterDescriptorFactory) {
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
		vscode.commands.registerCommand('extension.mond.getProgramName', config => {
			return vscode.window.showInputBox({
				placeHolder: 'Please enter the name of a Mond file in the workspace folder',
				value: 'program.mnd'
			});
		}),
	);

	// register a configuration provider for 'mond' debug type
	const provider = new MockConfigurationProvider();
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider('mond', provider));

	// register a dynamic configuration provider for 'mond' debug type
	context.subscriptions.push(vscode.debug.registerDebugConfigurationProvider('mond', {
		provideDebugConfigurations(folder: WorkspaceFolder | undefined): ProviderResult<DebugConfiguration[]> {
			return [
				{
					name: 'Dynamic Launch',
					request: 'launch',
					type: 'mond',
					program: '${file}',
				},
			];
		}
	}, vscode.DebugConfigurationProviderTriggerKind.Dynamic));

	if (!factory) {
		factory = new InlineDebugAdapterFactory();
	}

	context.subscriptions.push(vscode.debug.registerDebugAdapterDescriptorFactory('mond', factory));
	if ('dispose' in factory) {
		context.subscriptions.push(factory);
	}
}

class MockConfigurationProvider implements vscode.DebugConfigurationProvider {

	/**
	 * Massage a debug configuration just before a debug session is being launched,
	 * e.g. add all missing attributes to the debug configuration.
	 */
	resolveDebugConfiguration(folder: WorkspaceFolder | undefined, config: DebugConfiguration, token?: CancellationToken): ProviderResult<DebugConfiguration> {
		// if launch.json is missing or empty
		if (!config.type && !config.request && !config.name) {
			const editor = vscode.window.activeTextEditor;
			if (editor && editor.document.languageId === 'mond') {
				config.type = 'mond';
				config.name = 'Launch';
				config.request = 'launch';
				config.program = '${file}';
				config.stopOnEntry = true;
			}
		}

		if (!config.program) {
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
