<p align="center"><img src="http://i.imgur.com/As4LMO6.png" alt="Mond"/>

### Features
* [sequences](https://github.com/Rohansi/Mond/wiki/Sequences) that can also be used for [async/await](https://fpp.literallybrian.com/mond/#e24f0a629859b38e2c27efa8aebaf60cf2cc4aed)
* [prototype-based inheritance](https://github.com/Rohansi/Mond/wiki/Prototypes)
* [metamethods](https://github.com/Rohansi/Mond/wiki/Metamethods)
* [simple embedding](https://github.com/Rohansi/Mond/wiki/Basic-Usage) with a [great binding API](https://github.com/Rohansi/Mond/wiki/Binding-API)

[Try it in your browser!](https://fpp.literallybrian.com/mond/)

### Example
```
seq range(start, end) {
    for (var i = start; i <= end; i++)
        yield i;
}

seq where(list, filter) {
    foreach (var x in list) {
        if (filter(x))
            yield x;
    }
}

seq select(list, transform) {
    foreach (var x in list)
        yield transform(x);
}

fun toArray(list) {
    var array = [];
    foreach (var value in list) {
        array.add(value);
    }
    return array;
}

return range(0, 1000)
       |> where(x -> x % 2 == 0)
       |> select(x -> x / 2)
       |> toArray();
```

### Documentation
Please check the [wiki](https://github.com/Rohansi/Mond/wiki) for documentation. If you have any questions, try asking someone on [Gitter](https://gitter.im/Rohansi/Mond).

| .NET | Mono |
|------|------|
| [![Build status](https://ci.appveyor.com/api/projects/status/di5tqqt73bu6aire)](https://ci.appveyor.com/project/Rohansi/mond) | [![Build Status](https://travis-ci.org/Rohansi/Mond.svg?branch=master)](https://travis-ci.org/Rohansi/Mond)
