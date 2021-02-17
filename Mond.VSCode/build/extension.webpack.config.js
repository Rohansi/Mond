/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

//@ts-check
'use strict';

//@ts-check
/** @typedef {import('webpack').Configuration} WebpackConfig **/

const path = require('path');

module.exports = /** @type WebpackConfig */ {
	context: path.dirname(__dirname),
	mode: 'none', // this leaves the source code as close as possible to the original (when packaging we set this to 'production')
	target: 'node', // vscode extensions run in a Node.js-context
	entry: {
		extension: './src/extension.ts'
	},
	resolve: { // support reading TypeScript and JavaScript files
		extensions: ['.ts', '.js']
	},
	node: {
		__dirname: false, // leave the __dirname-behaviour intact
	},
	module: {
		rules: [{
			test: /\.ts$/,
			exclude: /node_modules/,
			use: [{
				// configure TypeScript loader:
				// * enable sources maps for end-to-end source maps
				loader: 'ts-loader',
				options: {
					compilerOptions: {
						'sourceMap': true,
						'declaration': false
					}
				}
			}]
		}]
	},
	externals: {
		vscode: "commonjs vscode" // the vscode-module is created on-the-fly and must be excluded. Add other modules that cannot be webpack'ed
	},
	output: {
		filename: 'extension.js',
		path: path.resolve(__dirname, '../dist/ext'),
		libraryTarget: 'commonjs2',
		devtoolModuleFilenameTemplate: "../../[resource-path]"
	},
	devtool: 'source-map'
}
