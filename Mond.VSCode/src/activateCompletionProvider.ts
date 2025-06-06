import * as vscode from "vscode";
import { keywords, methods, constants } from "./completionItems";

const keywordCompletionItems = keywords.map((keyword) => {
    const item = new vscode.CompletionItem(
        keyword,
        vscode.CompletionItemKind.Keyword,
    );
    return item;
});

const constantCompletionItems = constants.map((method) => {
    const item = new vscode.CompletionItem(
        method,
        vscode.CompletionItemKind.Constant,
    );

    return item;
});

const methodCompletionItems = methods.map((method) => {
    const item = new vscode.CompletionItem(
        method,
        vscode.CompletionItemKind.Method,
    );

    return item;
});

const allCompletionItems = [...keywordCompletionItems, ...methodCompletionItems, ...constantCompletionItems];

export function activateCompletionProvider(context: vscode.ExtensionContext) {
    context.subscriptions.push(        
        vscode.languages.registerCompletionItemProvider(
            { language: "mond" },
            {
                provideCompletionItems(
                    document: vscode.TextDocument,
                    position: vscode.Position
                ) {
                    const config = vscode.workspace.getConfiguration('mond.standardLibraries');
                    const stlEnableCompletion = config.get<boolean>('enableCompletion');

                    const wordRange = document.getWordRangeAtPosition(position, /(?:(?:[A-Z]\w+\.)(?:[A-Za-z_]\w*)?)|(?:(?<!\.)\b[A-Za-z_]\w*)/g);
                    if (!wordRange) {
                        return [];
                    }
                    
                    const word = document.getText(wordRange);
                    
                    const completionItems = stlEnableCompletion
                        ? allCompletionItems
                        : keywordCompletionItems;
                    
                    // VSCode filters the completion items for us in all cases except manually triggering completion after typing something like "Char."
                    // It filters fine when automatically triggered but manually triggering after text like that makes it not filter at all
                    // Probably because the text ends with a dot so it assumes it doesn't matter
                    if (word && typeof word === "string") {
                        const dotIndex = word.indexOf(".");
                        if (dotIndex > 0) {
                            const moduleName = word.substring(0, dotIndex);
                            return completionItems.filter(i => typeof i.label === "string" && i.label.startsWith(moduleName));
                        }
                    }
                    
                    return completionItems;
                },
            }
        )
    );
}
