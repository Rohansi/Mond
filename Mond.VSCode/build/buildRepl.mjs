// @ts-check
// Builds the Mond REPL from the sibling C# project and points the sample workspace at it, so the
// debug adapter tests exercise the runtime in this repository rather than whatever happens to be
// installed. Everything here is best effort - the tests skip themselves when the REPL is missing.
import { spawnSync } from 'child_process';
import { existsSync, mkdirSync, readdirSync, statSync, writeFileSync } from 'fs';
import * as path from 'path';
import { fileURLToPath } from 'url';

const extensionRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const projectPath = path.join(extensionRoot, '..', 'Mond.Repl', 'Mond.Repl.csproj');
const outputRoot = path.join(extensionRoot, '..', 'Mond.Repl', 'bin', 'Debug');
const executableName = process.platform === 'win32' ? 'Mond.Repl.exe' : 'Mond.Repl';
const settingsPath = path.join(extensionRoot, 'sampleWorkspace', '.vscode', 'settings.json');

function skip(reason) {
	console.log(`Skipping the Mond REPL build: ${reason}`);
	console.log('Debug adapter tests will be skipped unless a REPL is on your PATH.');
	process.exit(0);
}

/**
 * The most recently built executable. There is one output directory per target framework and stale
 * ones stick around after a retarget, so go by build time rather than trying to rank framework names.
 */
function findExecutable() {
	if (!existsSync(outputRoot)) {
		return undefined;
	}

	return readdirSync(outputRoot, { withFileTypes: true })
		.filter(e => e.isDirectory())
		.map(e => path.join(outputRoot, e.name, executableName))
		.filter(existsSync)
		.map(p => ({ path: p, time: statSync(p).mtimeMs }))
		.sort((a, b) => a.time - b.time)
		.pop()?.path;
}

if (!existsSync(projectPath)) {
	skip(`${projectPath} does not exist`);
}

const build = spawnSync('dotnet', ['build', projectPath, '-c', 'Debug', '--nologo', '-v', 'quiet'], {
	stdio: 'inherit',
	shell: process.platform === 'win32',
});

if (build.error || build.status !== 0) {
	// a running REPL can hold a lock on the output, in which case the previous build is still usable
	console.warn(`Failed to build the Mond REPL${build.error ? `: ${build.error.message}` : ''}`);

	if (!findExecutable()) {
		skip('dotnet build failed and there is no previous build to fall back on');
	}

	console.warn('Falling back to the existing build.');
}

const executable = findExecutable();
if (!executable) {
	skip(`no ${executableName} was produced under ${outputRoot}`);
}

// this file is generated, not committed - the path is absolute and platform specific
mkdirSync(path.dirname(settingsPath), { recursive: true });
writeFileSync(settingsPath, `${JSON.stringify({
	'mond.replPath': executable,
	// other tests leave untitled documents behind, and starting a debug session saves dirty editors
	// by default - saving an untitled one means a Save As dialog that nothing is there to answer
	'debug.saveBeforeStart': 'none',
}, null, '\t')}\n`);

console.log(`Mond REPL built: ${executable}`);
