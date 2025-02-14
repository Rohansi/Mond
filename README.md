![Mond Logo](https://i.imgur.com/mCgLTQN.png)

### Features
* [sequences](https://github.com/Rohansi/Mond/wiki/Sequences) that can also be used for async/await
* [prototype-based inheritance](https://github.com/Rohansi/Mond/wiki/Prototypes)
* [metamethods](https://github.com/Rohansi/Mond/wiki/Metamethods)
* [simple embedding](https://github.com/Rohansi/Mond/wiki/Basic-Usage) with a [great binding API](https://github.com/Rohansi/Mond/wiki/Automatic-Binding)
* a [useful debugger](https://github.com/Rohansi/Mond/wiki/Debugging) that integrates with [VS Code](https://marketplace.visualstudio.com/items?itemName=Rohansi.mond-vscode)
* fully compatible with Native AOT deployments (.NET 8+)

### Trying it
[You can try it in your browser!](https://mond.rohan.dev/)

![The Mond REPL in action](https://i.imgur.com/HBBMhGW.gif)

Alternatively, the Mond REPL is available as a `dotnet` tool:

```
dotnet tool install -g Mond.Repl
```

### Example
```kotlin
import Seq;

var random = Random();
var total = Seq.range(0, 100)
    |> Seq.select(() -> random.next(1, 10))
    |> Seq.sum();

printLn("average = {0}".format(total / 10));
```

### Install
Mond is [available on NuGet](https://www.nuget.org/packages/Mond/). To install it, use the following command in the Package Manager Console.
```
PM> Install-Package Mond
```

The remote debugger is [also available on NuGet](https://www.nuget.org/packages/Mond.RemoteDebugger/).
```
PM> Install-Package Mond.RemoteDebugger
```

Syntax highlighting and debugging functionality is provided in Visual Studio Code with [the Mond VSCode extension](https://marketplace.visualstudio.com/items?itemName=Rohansi.mond-vscode).

### Documentation
Please check the [wiki](https://github.com/Rohansi/Mond/wiki) for documentation.
