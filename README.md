 Mond
==========

Mond is a simple scripting language written entirely in C#. It supports advanced features like:
* sequences (generators)
* list comprehension
* lambda expressions
* prototype-based inheritance
* simple embedding
* sane variable scopes

This is what it looks like:
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

Please check [the wiki](https://github.com/Rohansi/Mond/wiki) for documentation.
