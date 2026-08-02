import * as vscode from 'vscode';
import { keywordNames } from './completionItems';
import { Token, TokenKind, findTokenIndex, flattenDeclarations, scan } from './mondScanner';

const keywordSet = new Set<string>(keywordNames);

/**
 * Expands the identifier under the cursor into the whole member chain, so hovering over `c` in
 * `a.b.c` evaluates `a.b.c` rather than a bare `c` that means nothing on its own.
 */
function memberChain(tokens: readonly Token[], index: number): { start: number; end: number } | undefined {
	if (tokens[index]?.kind !== TokenKind.Identifier) {
		return undefined;
	}

	let first = index;
	while (first >= 2 && tokens[first - 1].text === '.' && tokens[first - 2].kind === TokenKind.Identifier) {
		first -= 2;
	}

	// a chain that starts with a keyword is not something we can evaluate
	if (keywordSet.has(tokens[first].text)) {
		return undefined;
	}

	// only walk right while the chain stays simple - an indexer could have side effects, and a call
	// definitely does, neither of which belong in a hover
	let last = index;
	while (last + 2 < tokens.length && tokens[last + 1].text === '.' && tokens[last + 2].kind === TokenKind.Identifier) {
		last += 2;
	}

	return { start: tokens[first].start, end: tokens[last].end };
}

const evaluatableExpressionProvider: vscode.EvaluatableExpressionProvider = {
	provideEvaluatableExpression(document, position) {
		const { tokens } = scan(document);
		const index = findTokenIndex(tokens, document.offsetAt(position));
		if (index < 0) {
			return undefined;
		}

		const chain = memberChain(tokens, index);
		if (!chain) {
			return undefined;
		}

		const range = new vscode.Range(document.positionAt(chain.start), document.positionAt(chain.end));
		return new vscode.EvaluatableExpression(range, document.getText(range));
	},
};

const inlineValuesProvider: vscode.InlineValuesProvider = {
	provideInlineValues(document, viewPort, context) {
		const { tokens, declarations } = scan(document);

		const declared = new Set<string>();
		for (const declaration of flattenDeclarations(declarations)) {
			declared.add(declaration.name);
			for (const parameter of declaration.parameters ?? []) {
				declared.add(parameter);
			}
		}

		// nothing below the stopped line has run yet, so its values would be misleading
		const lastLine = Math.min(viewPort.end.line, context.stoppedLocation.end.line);
		const values: vscode.InlineValue[] = [];
		const seen = new Set<string>();

		for (let i = 0; i < tokens.length; i++) {
			const token = tokens[i];
			if (token.kind !== TokenKind.Identifier || !declared.has(token.text)) {
				continue;
			}

			// members are looked up on a value we would have to evaluate, and the debugger only
			// resolves plain names, so only offer identifiers that stand on their own
			if (tokens[i - 1]?.text === '.') {
				continue;
			}

			const start = document.positionAt(token.start);
			if (start.line < viewPort.start.line || start.line > lastLine) {
				continue;
			}

			// one value per name per line is enough, more just clutters the line
			const key = `${start.line}\u0000${token.text}`;
			if (seen.has(key)) {
				continue;
			}
			seen.add(key);

			const range = new vscode.Range(start, document.positionAt(token.end));
			values.push(new vscode.InlineValueVariableLookup(range, token.text, false));
		}

		return values;
	},
};

export function activateDebugFeatures(context: vscode.ExtensionContext) {
	const selector: vscode.DocumentSelector = { language: 'mond' };

	context.subscriptions.push(
		vscode.languages.registerEvaluatableExpressionProvider(selector, evaluatableExpressionProvider),
		vscode.languages.registerInlineValuesProvider(selector, inlineValuesProvider)
	);
}
