using System.Collections.Generic;

namespace Mond
{
    class IndexedStack<T>
    {
        private readonly List<T> _items;

        public IndexedStack(int capacity = 16)
        {
            _items = new List<T>(capacity);
        }

        public int Count => _items.Count;

        public T this[int index] => _items[index];

        public void Push(T value)
        {
            _items.Add(value);
        }

        public T Pop()
        {
            var value = Peek();
            _items.RemoveAt(_items.Count - 1);
            return value;
        }

        public T Peek()
        {
            return _items[_items.Count - 1];
        }
    }
}
