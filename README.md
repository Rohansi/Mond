 Mond [![Build status](https://ci.appveyor.com/api/projects/status/di5tqqt73bu6aire)](https://ci.appveyor.com/project/Rohansi/mond)
==========

Mond is a simple scripting language written entirely in C#. It supports advanced features like:
* [sequences](https://github.com/Rohansi/Mond/wiki/Sequences) (also known as [generators](http://en.wikipedia.org/wiki/Generator_(computer_programming)))
* [list comprehension](https://github.com/Rohansi/Mond/wiki/List-Comprehension)
* [prototype-based inheritance](https://github.com/Rohansi/Mond/wiki/Prototypes)
* [metamethods](https://github.com/Rohansi/Mond/wiki/Metamethods)
* [simple embedding](https://github.com/Rohansi/Mond/wiki/Basic-Usage)

[Try it in your browser!](https://fpp.literallybrian.com/mond/)

### Documentation
Please check [the wiki](https://github.com/Rohansi/Mond/wiki) for documentation.

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
