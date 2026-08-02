# Mond for Visual Studio Code

Language support and debugging for the [Mond scripting language](https://github.com/Rohansi/Mond).

## Features

- **Syntax highlighting** for `.mnd` files, including nested block comments, arrow lambdas, decorators and object literals.
- **Editing support** — bracket matching and colorization, auto-closing pairs, comment continuation on <kbd>Enter</kbd>, indentation rules and `// #region` folding markers.
- **Completion** for keywords, standard library globals and modules, and identifiers declared in the current file. Typing `.` after a module such as `Math` lists its members.
- **Hover** showing signatures for standard library symbols and for functions and variables declared in the file.
- **Outline and breadcrumbs** listing `fun`, `seq`, `var` and `const` declarations, including nested ones.
- **Folding** for blocks, comment blocks and regions.
- **Snippets** for the common declarations and control flow statements.
- **Debugging** — launch a script or attach to a running Mond process, set breakpoints, step, inspect the call stack and evaluate expressions in the Debug Console.

## Requirements

Debugging shells out to the Mond REPL. The extension looks for it in this order:

1. The `mond.replPath` setting, if you set one.
2. `mond` on your `PATH`.
3. The [`Mond.Repl`](https://www.nuget.org/packages/Mond.Repl) dotnet tool, which you can install with `dotnet tool install --global Mond.Repl`. The extension offers to do this for you the first time it comes up empty.

Everything other than debugging works without it.

## Debugging

Add a configuration to `.vscode/launch.json`, or press <kbd>F5</kbd> with a `.mnd` file open to debug it directly.

```json
{
    "name": "Run Mond script",
    "type": "mond",
    "request": "launch",
    "program": "${workspaceFolder}/${file}",
    "stopOnEntry": true
}
```

To debug a process that is already running with the Mond remote debugger enabled:

```json
{
    "name": "Attach to Mond process",
    "type": "mond",
    "request": "attach",
    "endpoint": "ws://127.0.0.1:1597"
}
```

Mond can only resolve local variables for the frame it stopped in, so selecting a caller in the Call Stack shows globals only. Conditional breakpoints and logpoints are not supported yet.

## Settings

| Setting | Default | Description |
| --- | --- | --- |
| `mond.replPath` | _(empty)_ | Path to the Mond REPL executable. Supports `${workspaceFolder}` and `${userHome}`. |
| `mond.standardLibraries.enableCompletion` | `true` | Suggest symbols from the Mond standard library. |

## Contributing

The extension lives in the [`Mond.VSCode`](https://github.com/Rohansi/Mond/tree/master/Mond.VSCode) folder of the Mond repository.

```
npm install
npm run watch     # rebuild the bundle on change
npm run lint
npm test
```

`npm test` builds `Mond.Repl` from the sibling project and points the sample workspace at it, so the debug adapter tests run against the runtime in this repository. It needs the .NET SDK; without it those tests skip and the rest still run.

Press <kbd>F5</kbd> to launch a development host with the extension loaded against `sampleWorkspace`.
