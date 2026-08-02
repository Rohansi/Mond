# Change Log

## Unreleased

### Added

- Completion for identifiers declared in the current file, with `.` as a trigger character and real member items for standard library modules.
- Hover showing signatures for standard library symbols and for declarations in the current file.
- Outline, breadcrumbs and folding ranges for `fun`, `seq`, `var` and `const` declarations.
- Snippets are now contributed to the editor. They shipped with the extension before but were never wired up.
- The Mond REPL is looked up through the `mond.replPath` setting, then your `PATH`, then the `Mond.Repl` dotnet tool. When nothing is found the extension offers to install the tool, and points at the .NET download when the SDK is missing too.
- Indentation rules, comment continuation, auto-closing pair guards inside strings and comments, and `// #region` folding markers.
- Pause support while a script is running.
- Extension tests, including a debug adapter integration test.

### Changed

- Grammar now highlights arrow lambdas, varargs, decorators, object literal keys, and function names and parameters.
- Completion items carry a kind, signature and sort order, and are suppressed inside strings and comments.
- Standard library metadata corrected: `Task` is its own module, and the task and cancellation members moved onto `TaskCompletionSource`, `CancellationTokenSource` and `CancellationToken`. `Random` methods and the `String`, `Array`, `Object`, `Number` and `Function` prototypes were added.
- Bundling moved from webpack to esbuild, and `vscode-debugadapter` was replaced by `@vscode/debugadapter`.

### Fixed

- Breakpoints could not be bound or hit against recent Mond builds. Requires Mond debug protocol version 2.
- Breakpoints on blank lines no longer stack onto the next line of code, and unbound breakpoints are now reported as unverified.
- Selecting a caller in the Call Stack no longer shows the top frame's variables.
- Standard library values such as `Math` and `printLn` were listed in the Local scope as `undefined`. They are globals, so they only appear in the Global scope now, with their real values. Requires an updated Mond runtime.
- Stack frame ids stay stable while stopped, so the Local scope no longer disappears when the stack is requested more than once.
- Setting or querying a breakpoint no longer fails with `Socket is not open` when no debugger is attached - while running without debugging, before the session connects, or after it ends. Breakpoints are reported as unverified instead.
- The debug session no longer emits multiple terminated events, and in-flight requests are rejected when the connection drops instead of hanging.
- `0x1` and `0b1` are highlighted as numbers.

## 0.0.5

- Initial published release.
