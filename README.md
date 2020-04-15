<p align="center"><img src="https://i.imgur.com/As4LMO6.png" alt="Mond Logo"/>

### Features
* [sequences](https://github.com/Rohansi/Mond/wiki/Sequences) that can also be used for [async/await](https://fpp.literallybrian.com/mond/#e24f0a629859b38e2c27efa8aebaf60cf2cc4aed)
* [prototype-based inheritance](https://github.com/Rohansi/Mond/wiki/Prototypes)
* [metamethods](https://github.com/Rohansi/Mond/wiki/Metamethods)
* [simple embedding](https://github.com/Rohansi/Mond/wiki/Basic-Usage) with a [great binding API](https://github.com/Rohansi/Mond/wiki/Binding-API)
* and a [useful debugger](https://github.com/Rohansi/Mond/wiki/Debugging)

### Trying it
[You can try it in your browser!](https://rohbot.net/mond/) If you're interested, the backend code for that website is [available here](https://github.com/Rohansi/Mondbot).

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
```

The remote debugger is [also available on NuGet](https://www.nuget.org/packages/Mond.RemoteDebugger/).
```
PM> Install-Package Mond.RemoteDebugger
```

### Documentation
Please check the [wiki](https://github.com/Rohansi/Mond/wiki) for documentation.


### Build Status
| .NET | Mono |
|------|------|
| [![Build status](https://ci.appveyor.com/api/projects/status/di5tqqt73bu6aire)](https://ci.appveyor.com/project/Rohansi/mond) | [![Build Status](https://travis-ci.org/Rohansi/Mond.svg?branch=master)](https://travis-ci.org/Rohansi/Mond)
