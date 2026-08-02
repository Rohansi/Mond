// @ts-check
import * as esbuild from 'esbuild';

const production = process.argv.includes('--production');
const watch = process.argv.includes('--watch');

/**
 * esbuild reports problems on stderr in its own format, so this prints them in a shape the
 * `$esbuild-watch` problem matcher understands.
 * @type {import('esbuild').Plugin}
 */
const problemMatcherPlugin = {
	name: 'problem-matcher',
	setup(build) {
		build.onStart(() => {
			console.log('[watch] build started');
		});

		build.onEnd(result => {
			for (const { text, location } of result.errors) {
				console.error(`✘ [ERROR] ${text}`);
				if (location) {
					console.error(`    ${location.file}:${location.line}:${location.column}:`);
				}
			}
			console.log('[watch] build finished');
		});
	},
};

/** @type {import('esbuild').BuildOptions} */
const options = {
	entryPoints: ['src/extension.ts'],
	bundle: true,
	format: 'cjs',
	platform: 'node',
	// matches the Electron version shipped with the minimum supported VS Code
	target: 'node20',
	outfile: 'dist/ext/extension.js',
	// vscode is provided by the host, and ws only loads the native accelerators opportunistically
	external: ['vscode', 'bufferutil', 'utf-8-validate'],
	minify: production,
	sourcemap: !production,
	sourcesContent: false,
	logLevel: 'silent',
	plugins: [problemMatcherPlugin],
};

if (watch) {
	const context = await esbuild.context(options);
	await context.watch();
} else {
	await esbuild.build(options);
}
