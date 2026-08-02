import { exec } from 'child_process';
import { accessSync, constants, statSync } from 'fs';
import { homedir } from 'os';
import * as path from 'path';
import { platform } from 'process';
import * as vscode from 'vscode';
import { errorMessage, resolveVariables } from './utility';

/** NuGet package id of the REPL, which is packed as a dotnet tool. */
const toolPackageId = 'Mond.Repl';

/** `ToolCommandName` of the REPL, which is also what it is called when installed manually. */
const toolCommandName = 'mond';

const dotnetDownloadUrl = 'https://dotnet.microsoft.com/download';

const isWindows = platform === 'win32';

/** Thrown when the REPL could not be found and the user did not install it. */
export class MondNotFoundError extends Error {}

/** Only one install prompt at a time, no matter how many sessions start at once. */
let pendingInstall: Promise<string | undefined> | undefined;

/**
 * Finds the Mond REPL without any user interaction.
 *
 * Looks at the `mond.replPath` setting, then the `PATH`, then the place `dotnet tool install
 * --global` puts its shims - the last one matters because that directory is only added to `PATH`
 * for new logins, so a freshly installed tool is often invisible to the running extension host.
 */
export function locateMond(): string | undefined {
	const configured = getConfiguredPath();
	if (configured) {
		return isExecutable(configured) ? configured : undefined;
	}

	return findOnPath(toolCommandName) ?? findGlobalTool();
}

/**
 * Finds the Mond REPL, offering to install the dotnet tool when it is missing.
 *
 * @throws {MondNotFoundError} when the REPL is not available and could not be installed.
 */
export async function findMondAsync(): Promise<string> {
	const configured = getConfiguredPath();
	if (configured) {
		// an explicit setting is never second guessed - silently falling back would just hide a typo
		if (!isExecutable(configured)) {
			throw new MondNotFoundError(
				`The mond.replPath setting points at '${configured}', which is not an executable file.`);
		}

		return configured;
	}

	const found = findOnPath(toolCommandName) ?? findGlobalTool();
	if (found) {
		return found;
	}

	const installed = await promptToInstall();
	if (installed) {
		return installed;
	}

	throw new MondNotFoundError(
		`The Mond REPL was not found. Install it with 'dotnet tool install --global ${toolPackageId}', ` +
		'or set mond.replPath to the executable.');
}

function getConfiguredPath(): string | undefined {
	const configured = vscode.workspace.getConfiguration('mond').get<string>('replPath')?.trim();
	if (!configured) {
		return undefined;
	}

	return path.normalize(resolveVariables(configured));
}

function isExecutable(filePath: string): boolean {
	try {
		if (!statSync(filePath).isFile()) {
			return false;
		}

		// Windows has no execute bit, so X_OK there is just an existence check
		accessSync(filePath, isWindows ? constants.F_OK : constants.X_OK);
		return true;
	} catch {
		return false;
	}
}

function findOnPath(command: string): string | undefined {
	const searchPath = process.env.PATH;
	if (!searchPath) {
		return undefined;
	}

	const names = isWindows ? [`${command}.exe`, `${command}.cmd`, `${command}.bat`] : [command];

	for (const dir of searchPath.split(path.delimiter)) {
		if (!dir) {
			continue;
		}

		for (const name of names) {
			const candidate = path.join(dir, name);
			if (isExecutable(candidate)) {
				return candidate;
			}
		}
	}

	return undefined;
}

/** The default shim directory used by `dotnet tool install --global`. */
function findGlobalTool(): string | undefined {
	const toolsDir = process.env.DOTNET_TOOLS_PATH ?? path.join(homedir(), '.dotnet', 'tools');
	const candidate = path.join(toolsDir, isWindows ? `${toolCommandName}.exe` : toolCommandName);
	return isExecutable(candidate) ? candidate : undefined;
}

async function promptToInstall(): Promise<string | undefined> {
	pendingInstall ??= promptToInstallCore().finally(() => { pendingInstall = undefined; });
	return pendingInstall;
}

async function promptToInstallCore(): Promise<string | undefined> {
	if (!findOnPath('dotnet')) {
		const getDotnet = 'Get .NET';
		const choice = await vscode.window.showErrorMessage(
			`The Mond REPL was not found, and neither was the .NET SDK needed to install it. ` +
			`Install the .NET SDK, then run 'dotnet tool install --global ${toolPackageId}'.`,
			getDotnet);

		if (choice === getDotnet) {
			await vscode.env.openExternal(vscode.Uri.parse(dotnetDownloadUrl));
		}

		return undefined;
	}

	const install = 'Install';
	const setPath = 'Set Path...';
	const choice = await vscode.window.showWarningMessage(
		'The Mond REPL is required to run and debug scripts, but it was not found on your PATH.',
		{ modal: false },
		install,
		setPath);

	if (choice === setPath) {
		await vscode.commands.executeCommand('workbench.action.openSettings', 'mond.replPath');
		return undefined;
	}

	if (choice !== install) {
		return undefined;
	}

	return vscode.window.withProgress({
		location: vscode.ProgressLocation.Notification,
		title: `Installing the ${toolPackageId} dotnet tool...`,
	}, async () => {
		try {
			await run(`dotnet tool install --global ${toolPackageId}`);
		} catch (e) {
			await vscode.window.showErrorMessage(`Failed to install ${toolPackageId}: ${errorMessage(e)}`);
			return undefined;
		}

		// the tool directory is not on this process's PATH yet, so look for the shim directly
		const installed = findGlobalTool();
		if (!installed) {
			await vscode.window.showErrorMessage(
				`${toolPackageId} was installed but its '${toolCommandName}' command could not be found. ` +
				'Set mond.replPath to the executable.');
		}

		return installed;
	});
}

function run(command: string): Promise<string> {
	return new Promise((resolve, reject) => {
		exec(command, { windowsHide: true }, (error, stdout, stderr) => {
			if (error) {
				reject(new Error(stderr.trim() || stdout.trim() || error.message));
			} else {
				resolve(stdout);
			}
		});
	});
}
