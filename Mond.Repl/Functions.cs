using System;
using System.Collections.Generic;
using System.Linq;

namespace Mond.Repl
{
    static class Functions
    {
        private static Dictionary<string, MondFunction> _functions;
        private static string _definitions;

        static Functions()
        {
            _functions = new Dictionary<string, MondFunction>
            {
                { "print", Print },
                { "stdin", Stdin }
            };
        }
         
        public static void Register(MondState state)
        {
            foreach (var fn in _functions)
            {
                state[fn.Key] = fn.Value;
            }
        }

        public static string Definitions
        {
            get
            {
                if (_definitions != null)
                    return _definitions;

                if (_functions.Count == 0)
                {
                    _definitions = "";
                    return _definitions;
                }

                var values = string.Join(", ", _functions.Keys.Select(fn => string.Format("{0}=global.{1}", fn, fn)));
                _definitions = "var " + values + ";";

                return _definitions;
            }
        }

        private static MondValue Print(MondState state, params MondValue[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine();
            }
            else if (args[0].Type == MondValueType.String)
            {
                Console.WriteLine((string)args[0]);
            }
            else
            {
                args[0].Serialize(Console.Out);
                Console.WriteLine();
            }

            return MondValue.Undefined;
        }

        private static MondValue Stdin(MondState state, params MondValue[] arguments)
        {
            return MondValue.FromEnumerable(StdinEnumerable().Select(c => new MondValue(new string(c, 1))));
        }

        private static IEnumerable<char> StdinEnumerable()
        {
            while (true)
            {
                var key = Console.Read();

                if (key == -1)
                    yield break;

                yield return (char)key;
            }
        }
    }
}
