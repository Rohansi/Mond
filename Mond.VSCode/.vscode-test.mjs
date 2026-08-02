import { defineConfig } from '@vscode/test-cli';

export default defineConfig({
	files: 'out/test/**/*.test.js',
	workspaceFolder: './sampleWorkspace',
	mocha: {
		ui: 'bdd',
		timeout: 60000,
	},
});
