using System;
using System.Collections.Generic;
using System.IO;
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
                { "require", Require },
                { "print", Print },
                { "printLn", PrintLn },
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
                _definitions = "const " + values + ";";

                return _definitions;
            }
        }

        private static MondValue Require(MondState state, params MondValue[] arguments)
        {
            if (arguments.Length < 1)
                throw new MondRuntimeException("require: must be called with 1 argument");

            if (arguments[0].Type != MondValueType.String)
                throw new MondRuntimeException("require: argument 1 must be of type String");

            var fileName = (string)arguments[0];
            var program = MondProgram.Compile(Definitions + File.ReadAllText(fileName), fileName);
            return state.Load(program);
        }

        private static MondValue Print(MondState state, params MondValue[] arguments)
        {
            foreach (var v in arguments)
            {
                PrintImpl(v);
            }

            return MondValue.Undefined;
        }

        private static MondValue PrintLn(MondState state, params MondValue[] arguments)
        {
            if (arguments.Length == 0)
                Console.WriteLine();

            foreach (var v in arguments)
            {
                PrintImpl(v);
                Console.WriteLine();
            }

            return MondValue.Undefined;
        }

        private static void PrintImpl(MondValue value)
        {
            Console.Write((string)value);
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
