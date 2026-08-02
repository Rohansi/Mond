import * as vscode from "vscode";
import { findKeyword } from "./completionItems";
import { MondSymbol, findClass, findGlobal, findModule, getLibrary } from "./definitions";
import { Declaration, TokenKind, flattenDeclarations, forgetDocument, scan } from "./mondScanner";

const regionStart = /^\/\/\s*#?region\b/;
const regionEnd = /^\/\/\s*#?endregion\b/;

function toSymbolKind(declaration: Declaration): vscode.SymbolKind {
    switch (declaration.kind) {
        case 'fun':
            return vscode.SymbolKind.Function;
        case 'seq':
            return vscode.SymbolKind.Method;
        case 'const':
            return vscode.SymbolKind.Constant;
        default:
            return vscode.SymbolKind.Variable;
    }
}

function toDocumentSymbol(document: vscode.TextDocument, declaration: Declaration): vscode.DocumentSymbol {
    const range = new vscode.Range(document.positionAt(declaration.start), document.positionAt(declaration.end));
    const selectionRange = new vscode.Range(
        document.positionAt(declaration.nameStart),
        document.positionAt(declaration.nameEnd)
    );

    const detail = declaration.parameters ? `(${declaration.parameters.join(', ')})` : '';
    const symbol = new vscode.DocumentSymbol(
        declaration.name,
        detail,
        toSymbolKind(declaration),
        range,
        selectionRange
    );

    symbol.children = declaration.children.map(child => toDocumentSymbol(document, child));
    return symbol;
}

const documentSymbolProvider: vscode.DocumentSymbolProvider = {
    provideDocumentSymbols(document) {
        return scan(document).declarations.map(declaration => toDocumentSymbol(document, declaration));
    },
};

const foldingRangeProvider: vscode.FoldingRangeProvider = {
    provideFoldingRanges(document) {
        const { tokens, brackets } = scan(document);
        const ranges: vscode.FoldingRange[] = [];

        for (const [open, close] of brackets) {
            if (open > close) {
                continue;
            }

            const startLine = document.positionAt(tokens[open].start).line;
            const endLine = document.positionAt(tokens[close].start).line;
            if (endLine > startLine) {
                ranges.push(new vscode.FoldingRange(startLine, endLine - 1));
            }
        }

        const regionStack: number[] = [];
        let commentRunStart: number | undefined;
        let commentRunEnd = -1;

        const flushCommentRun = () => {
            if (commentRunStart !== undefined && commentRunEnd > commentRunStart) {
                ranges.push(new vscode.FoldingRange(commentRunStart, commentRunEnd, vscode.FoldingRangeKind.Comment));
            }
            commentRunStart = undefined;
        };

        for (const token of tokens) {
            if (token.kind !== TokenKind.Comment) {
                continue;
            }

            const startLine = document.positionAt(token.start).line;
            const endLine = document.positionAt(token.end).line;

            if (token.text.startsWith('/*')) {
                flushCommentRun();
                if (endLine > startLine) {
                    ranges.push(new vscode.FoldingRange(startLine, endLine, vscode.FoldingRangeKind.Comment));
                }
                continue;
            }

            if (regionStart.test(token.text)) {
                flushCommentRun();
                regionStack.push(startLine);
                continue;
            }

            if (regionEnd.test(token.text)) {
                flushCommentRun();
                const start = regionStack.pop();
                if (start !== undefined && startLine > start) {
                    ranges.push(new vscode.FoldingRange(start, startLine, vscode.FoldingRangeKind.Region));
                }
                continue;
            }

            // consecutive line comments fold as one block
            if (commentRunStart !== undefined && startLine === commentRunEnd + 1) {
                commentRunEnd = startLine;
                continue;
            }

            flushCommentRun();
            commentRunStart = startLine;
            commentRunEnd = startLine;
        }

        flushCommentRun();
        return ranges;
    },
};

function describe(symbol: MondSymbol, owner?: string): vscode.MarkdownString {
    const prefix = owner ? `${owner}.` : '';
    const signatures = symbol.signatures?.length ? symbol.signatures : [symbol.name];

    const markdown = new vscode.MarkdownString();
    markdown.appendCodeblock(signatures.map(s => `${prefix}${s}`).join('\n'), 'mond');

    if (symbol.documentation) {
        markdown.appendMarkdown(symbol.documentation);
    }

    return markdown;
}

function describeDeclaration(declaration: Declaration): vscode.MarkdownString {
    const markdown = new vscode.MarkdownString();
    const signature = declaration.parameters
        ? `${declaration.kind} ${declaration.name}(${declaration.parameters.join(', ')})`
        : `${declaration.kind} ${declaration.name}`;

    markdown.appendCodeblock(signature, 'mond');
    return markdown;
}

const hoverProvider: vscode.HoverProvider = {
    provideHover(document, position) {
        const wordRange = document.getWordRangeAtPosition(position, /[A-Za-z_][0-9A-Za-z_]*/);
        if (!wordRange) {
            return undefined;
        }

        const { tokens } = scan(document);
        const offset = document.offsetAt(wordRange.start);
        const index = tokens.findIndex(t => t.start === offset);
        if (index < 0 || tokens[index].kind === TokenKind.Comment || tokens[index].kind === TokenKind.String) {
            return undefined;
        }

        const word = tokens[index].text;
        const isMember = tokens[index - 1]?.text === '.';

        if (isMember) {
            const receiver = tokens[index - 2];
            const owner = receiver?.kind === TokenKind.Identifier ? findModule(receiver.text) : undefined;

            const member = owner?.members.find(m => m.name === word)
                ?? getLibrary().instanceMembers.find(m => m.name === word);
            if (!member) {
                return undefined;
            }

            return new vscode.Hover(describe(member, owner?.name), wordRange);
        }

        // a local declaration shadows anything from the standard library
        const declaration = flattenDeclarations(scan(document).declarations).find(d => d.name === word);
        if (declaration) {
            return new vscode.Hover(describeDeclaration(declaration), wordRange);
        }

        const known = findKeyword(word) ?? findGlobal(word);
        if (!known) {
            return undefined;
        }

        const hover = describe(known);
        const container = findModule(word) ?? findClass(word);
        if (container) {
            hover.appendMarkdown(`\n\nMembers: ${container.members.map(m => `\`${m.name}\``).join(', ')}`);
        }

        return new vscode.Hover(hover, wordRange);
    },
};

export function activateLanguageFeatures(context: vscode.ExtensionContext) {
    const selector: vscode.DocumentSelector = { language: 'mond' };

    context.subscriptions.push(
        vscode.languages.registerDocumentSymbolProvider(selector, documentSymbolProvider),
        vscode.languages.registerFoldingRangeProvider(selector, foldingRangeProvider),
        vscode.languages.registerHoverProvider(selector, hoverProvider),
        vscode.workspace.onDidCloseTextDocument(forgetDocument)
    );
}
