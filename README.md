<p align="center">
<img src="http://i.imgur.com/5oL2kVC.png" alt="Mond"></img>
<br/><b>Mond is a simple and elegant scripting language written entirely in C#</b>
</p>

### Features
* [sequences](https://github.com/Rohansi/Mond/wiki/Sequences)
* [list comprehension](https://github.com/Rohansi/Mond/wiki/List-Comprehension)
* [prototype-based inheritance](https://github.com/Rohansi/Mond/wiki/Prototypes)
* [metamethods](https://github.com/Rohansi/Mond/wiki/Metamethods)
* [simple embedding](https://github.com/Rohansi/Mond/wiki/Basic-Usage)

[Try it in your browser!](https://fpp.literallybrian.com/mond/)

### Example
```
seq range(start, end) {
    for (var i = start; i <= end; i++)
        yield i;
}

fun where(list, filter) -> [x : x in list, filter(x)];
fun select(list, transform) -> [transform(x) : x in list];

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
Please check the [wiki](https://github.com/Rohansi/Mond/wiki) for documentation.

[![Build status](https://ci.appveyor.com/api/projects/status/di5tqqt73bu6aire)](https://ci.appveyor.com/project/Rohansi/mond)
