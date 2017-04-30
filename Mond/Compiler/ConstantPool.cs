using System.Collections.Generic;

namespace Mond.Compiler
{
    class ConstantPool<T>
    {
        private Dictionary<T, ConstantOperand<T>> _operands;
        private int _nextId;

        public ConstantPool()
        {
            _operands = new Dictionary<T, ConstantOperand<T>>();
            _nextId = 0;
        }

        public List<T> Items
        {
            get
            {
                var items = new List<T>(_nextId);

                for (var i = 0; i < _nextId; i++)
                {
                    items.Add(default(T));
                }

                foreach (var operand in _operands.Values)
                {
                    items[operand.Id] = operand.Value;
                }

                return items;
            }
        }

        public ConstantOperand<T> GetOperand(T value)
        {
            if (_operands.TryGetValue(value, out var operand))
                return operand;

            operand = new ConstantOperand<T>(_nextId++, value);
            _operands.Add(value, operand);
            return operand;
        }
    }
}
