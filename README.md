<p align="center"><img src="https://i.imgur.com/As4LMO6.png" alt="Mond Logo"/></p>

### Features
* [sequences](https://github.com/Rohansi/Mond/wiki/Sequences) that can also be used for async/await
* [prototype-based inheritance](https://github.com/Rohansi/Mond/wiki/Prototypes)
* [metamethods](https://github.com/Rohansi/Mond/wiki/Metamethods)
* [simple embedding](https://github.com/Rohansi/Mond/wiki/Basic-Usage) with a [great binding API](https://github.com/Rohansi/Mond/wiki/Binding-API)
* and a [useful debugger](https://github.com/Rohansi/Mond/wiki/Debugging) that integrates with [VS Code](https://marketplace.visualstudio.com/items?itemName=Rohansi.mond-vscode)

### Trying it
[You can try it in your browser!](https://mond.rohan.dev/)

![The Mond REPL in action](https://files.facepunch.com/Rohan/2019/January/21_11-14-04.gif)

Alternatively, the Mond REPL is available as a `dotnet` tool:

```
dotnet tool install -g Mond.Repl
```

### Example
```kotlin
const Seq = require("Seq.mnd");

const randomApi =
    "https://www.random.org/decimal-fractions/?num=1&dec=9&col=1&format=plain";

Async.start(seq() {
    // concurrently request for 10 random numbers
    var numberTasks = Seq.range(0, 10)
        |> Seq.select(() -> Http.getAsync(randomApi))
        |> Seq.toArray();

    // wait for all the requests to finish
    var numbers = yield Task.whenAll(numberTasks);

    // parse and sum the numbers
    var total = numbers
        |> Seq.select(s -> Json.deserialize(s))
        |> Seq.aggregate(0, (acc, n) -> acc + n);
        
    printLn("average = {0}".format(total / 10));
});

Async.runToCompletion();
```

### Install
Mond is [available on NuGet](https://www.nuget.org/packages/Mond/). To install it, use the following command in the Package Manager Console.
```
PM> Install-Package Mond
PM> Install-Package Mond.SourceGenerator
```

The remote debugger is [also available on NuGet](https://www.nuget.org/packages/Mond.RemoteDebugger/).
```
PM> Install-Package Mond.RemoteDebugger
```

Syntax highlighting and debugging functionality is provided in Visual Studio Code with [the Mond VSCode extension](https://marketplace.visualstudio.com/items?itemName=Rohansi.mond-vscode).

### Documentation
Please check the [wiki](https://github.com/Rohansi/Mond/wiki) for documentation.
