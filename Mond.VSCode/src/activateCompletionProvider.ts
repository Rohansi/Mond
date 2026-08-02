import * as vscode from "vscode";
import { keywords } from "./completionItems";
import { MondLibrary, MondSymbol, getLibrary } from "./definitions";
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
    item.detail = detail ?? symbol.signatures?.[0];

    if (symbol.documentation) {
        item.documentation = new vscode.MarkdownString(symbol.documentation);
    }

    return item;
}

const keywordItems = keywords
    .filter(k => k.name !== '__declare_globals')
    .map(k => createItem(k, SortGroup.Keyword, k.kind === 'keyword' ? 'keyword' : undefined));

interface LibraryItems {
    readonly globals: vscode.CompletionItem[];
    readonly instanceMembers: vscode.CompletionItem[];
    readonly moduleMembers: Map<string, vscode.CompletionItem[]>;
    readonly classNames: Set<string>;
}

// building the items is cheap but not free, so they are rebuilt only when the definitions reload
let itemCache: { revision: number; items: LibraryItems } | undefined;

function libraryItems(): LibraryItems {
    const library = getLibrary();
    if (itemCache?.revision === library.revision) {
        return itemCache.items;
    }

    const items = buildLibraryItems(library);
    itemCache = { revision: library.revision, items };
    return items;
}

function buildLibraryItems(library: MondLibrary): LibraryItems {
    return {
        globals: library.globals.map(g => createItem(g, SortGroup.Global)),
        instanceMembers: library.instanceMembers.map(m => {
            const item = createItem(m, SortGroup.Member);
            item.documentation = new vscode.MarkdownString(
                `${m.documentation ? m.documentation + '\n\n' : ''}Defined on ${m.owners.join(', ')}.`);
            return item;
        }),
        moduleMembers: new Map(
            library.modules.map(container => [
                container.name,
                container.members.map(m => createItem(m, SortGroup.Member)),
            ])
        ),
        classNames: new Set(library.classes.map(c => c.name)),
    };
}

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

                    const library = libraryItems();
                    const linePrefix = document.getText(
                        new vscode.Range(position.with({ character: 0 }), position)
                    );

                    const memberAccess = memberAccessPattern.exec(linePrefix);
                    if (memberAccess) {
                        const receiver = memberAccess[1];
                        const moduleMembers = receiver ? library.moduleMembers.get(receiver) : undefined;
                        if (moduleMembers) {
                            return moduleMembers;
                        }

                        // constructors only expose instance methods through their instances, so
                        // there is nothing sensible to offer for `TaskCompletionSource.`
                        if (receiver && library.classNames.has(receiver)) {
                            return undefined;
                        }

                        return library.instanceMembers;
                    }

                    return [
                        ...localItems(document, offset),
                        ...keywordItems,
                        ...library.globals,
                    ];
                },
            },
            "."
        )
    );
}
