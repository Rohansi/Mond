import { defineConfig } from '@vscode/test-cli';

export default defineConfig({
	files: 'out/test/**/*.test.js',
	workspaceFolder: './sampleWorkspace',
	// debugging is unavailable in a restricted workspace, and the prompt is modal, so there would be
	// nothing to answer it
	launchArgs: ['--disable-workspace-trust'],
	mocha: {
		ui: 'bdd',
		// comfortably longer than the waits inside the tests, so a failure reports what it was
		// waiting for instead of a bare mocha timeout
		timeout: 120000,
	},
});
