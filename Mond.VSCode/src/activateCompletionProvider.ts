import * as vscode from "vscode";
import {
    MondSymbol,
    classes,
    globals,
    instanceMembers,
    keywords,
    modules,
} from "./completionItems";
import { Declaration, flattenDeclarations, isInCommentOrString, scan } from "./mondScanner";

// completions are grouped by how likely they are to be what you want, closest scope first
enum SortGroup {
    Local = '1',
    Member = '2',
    Keyword = '3',
    Global = '4',
}

function symbolKindToCompletionKind(symbol: MondSymbol): vscode.CompletionItemKind {
    switch (symbol.kind) {
        case 'keyword':
            return vscode.CompletionItemKind.Keyword;
        case 'constant':
            return vscode.CompletionItemKind.Constant;
        case 'function':
            return vscode.CompletionItemKind.Function;
        case 'method':
            return vscode.CompletionItemKind.Method;
        case 'property':
            return vscode.CompletionItemKind.Property;
        case 'module':
            return vscode.CompletionItemKind.Module;
        case 'class':
            return vscode.CompletionItemKind.Class;
    }
}

function createItem(symbol: MondSymbol, group: SortGroup, detail?: string): vscode.CompletionItem {
    const item = new vscode.CompletionItem(symbol.name, symbolKindToCompletionKind(symbol));
    item.sortText = group + symbol.name;
    item.detail = detail ?? symbol.signature;

    if (symbol.documentation) {
        item.documentation = new vscode.MarkdownString(symbol.documentation);
    }

    return item;
}

const keywordItems = keywords
    .filter(k => k.name !== '__declare_globals')
    .map(k => createItem(k, SortGroup.Keyword, k.kind === 'keyword' ? 'keyword' : undefined));

const globalItems = globals.map(g => createItem(g, SortGroup.Global));

const instanceMemberItems = instanceMembers.map(m => {
    const item = createItem(m, SortGroup.Member);
    item.documentation = new vscode.MarkdownString(`Defined on ${m.owners.join(', ')}.`);
    return item;
});

const moduleMemberItems = new Map(
    modules.map(container => [
        container.name,
        container.members.map(m => createItem(m, SortGroup.Member)),
    ])
);

const classNames = new Set(classes.map(c => c.name));

function declarationKind(declaration: Declaration): vscode.CompletionItemKind {
    switch (declaration.kind) {
        case 'fun':
        case 'seq':
            return vscode.CompletionItemKind.Function;
        case 'const':
            return vscode.CompletionItemKind.Constant;
        default:
            return vscode.CompletionItemKind.Variable;
    }
}

function localItems(document: vscode.TextDocument, offset: number): vscode.CompletionItem[] {
    const declarations = flattenDeclarations(scan(document).declarations);
    const items = new Map<string, vscode.CompletionItem>();

    const add = (name: string, kind: vscode.CompletionItemKind, detail: string) => {
        if (items.has(name)) {
            return;
        }
        const item = new vscode.CompletionItem(name, kind);
        item.sortText = SortGroup.Local + name;
        item.detail = detail;
        items.set(name, item);
    };

    for (const declaration of declarations) {
        // don't suggest the identifier that is currently being typed
        if (offset < declaration.nameStart || offset > declaration.nameEnd) {
            const detail = declaration.parameters
                ? `${declaration.kind} ${declaration.name}(${declaration.parameters.join(', ')})`
                : `${declaration.kind} ${declaration.name}`;
            add(declaration.name, declarationKind(declaration), detail);
        }

        // parameters are only in scope inside the function, so only offer them there
        if (declaration.parameters && offset > declaration.start && offset < declaration.end) {
            for (const parameter of declaration.parameters) {
                add(parameter, vscode.CompletionItemKind.Variable, `parameter of ${declaration.name}`);
            }
        }
    }

    return [...items.values()];
}

const memberAccessPattern = /([A-Za-z_][0-9A-Za-z_]*)?\s*\.\s*(?:[A-Za-z_][0-9A-Za-z_]*)?$/;

export function activateCompletionProvider(context: vscode.ExtensionContext) {
    context.subscriptions.push(
        vscode.languages.registerCompletionItemProvider(
            { language: "mond" },
            {
                provideCompletionItems(
                    document: vscode.TextDocument,
                    position: vscode.Position
                ) {
                    const offset = document.offsetAt(position);
                    if (isInCommentOrString(document, offset)) {
                        return undefined;
                    }

                    const config = vscode.workspace.getConfiguration('mond.standardLibraries');
                    const standardLibrariesEnabled = config.get<boolean>('enableCompletion') ?? true;

                    const linePrefix = document.getText(
                        new vscode.Range(position.with({ character: 0 }), position)
                    );

                    const memberAccess = memberAccessPattern.exec(linePrefix);
                    if (memberAccess) {
                        if (!standardLibrariesEnabled) {
                            return undefined;
                        }

                        const receiver = memberAccess[1];
                        const moduleMembers = receiver ? moduleMemberItems.get(receiver) : undefined;
                        if (moduleMembers) {
                            return moduleMembers;
                        }

                        // constructors only expose instance methods through their instances, so
                        // there is nothing sensible to offer for `TaskCompletionSource.`
                        if (receiver && classNames.has(receiver)) {
                            return undefined;
                        }

                        return instanceMemberItems;
                    }

                    const items = [...localItems(document, offset), ...keywordItems];
                    if (standardLibrariesEnabled) {
                        items.push(...globalItems);
                    }

                    return items;
                },
            },
            "."
        )
    );
}
