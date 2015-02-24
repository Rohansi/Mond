<p align="center"><img src="http://i.imgur.com/As4LMO6.png" alt="Mond"/>

### Features
* [sequences](https://github.com/Rohansi/Mond/wiki/Sequences) that can also be used for [async/await](https://fpp.literallybrian.com/mond/#e24f0a629859b38e2c27efa8aebaf60cf2cc4aed)
* [prototype-based inheritance](https://github.com/Rohansi/Mond/wiki/Prototypes)
* [metamethods](https://github.com/Rohansi/Mond/wiki/Metamethods)
* [simple embedding](https://github.com/Rohansi/Mond/wiki/Basic-Usage) with a [great binding API](https://github.com/Rohansi/Mond/wiki/Binding-API)
* and a [useful debugger](https://github.com/Rohansi/Mond/wiki/Debugging)

[Try it in your browser!](https://fpp.literallybrian.com/mond/)

### Example
```
const Seq = require("Seq.mnd");

const randomApi =
    "https://www.random.org/decimal-fractions/?num=1&dec=9&col=1&format=plain";

Async.start(seq() {
    var numberTasks = Seq.range(0, 10)
        |> Seq.select(() -> Http.get(randomApi))
        |> Seq.toArray();

    var numbers = yield Task.whenAll(numberTasks);

    var total = numbers
        |> Seq.select(s -> Json.deserialize(s))
        |> Seq.aggregate(0, (acc, n) -> acc + n);
        
    printLn("average = {0}".format(total / 10));
});

Async.runToCompletion();
```

### Install
Mond is currently available on NuGet as a *prerelease* version. To install it, use the following command in the Package Manager Console.
```
PM> Install-Package Mond -Pre
```

The remote debugger is also available on NuGet.
```
PM> Install-Package Mond.RemoteDebugger -Pre
```

### Documentation
Please check the [wiki](https://github.com/Rohansi/Mond/wiki) for documentation. If you have any questions, try asking someone on [Gitter](https://gitter.im/Rohansi/Mond).


### Build Status
| .NET | Mono |
|------|------|
| [![Build status](https://ci.appveyor.com/api/projects/status/di5tqqt73bu6aire)](https://ci.appveyor.com/project/Rohansi/mond) | [![Build Status](https://travis-ci.org/Rohansi/Mond.svg?branch=master)](https://travis-ci.org/Rohansi/Mond)
