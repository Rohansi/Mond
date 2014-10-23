using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Binding;
using Mond.Repl.Modules;

namespace Mond.Repl
{
    [MondModule("")]
    class Functions
    {
        private static List<KeyValuePair<string, MondValue>> _values;

        public static string Definitions { get; private set; }

        static Functions()
        {
            var functions = MondModuleBinder.BindFunctions<Functions>()
                                            .ToDictionary(kv => kv.Key, kv => new MondValue(kv.Value));

            var modules = new Dictionary<string, MondValue>
            {
                { "Math", MondMath.Binding }
            };

            _values = functions.Concat(modules).ToList();

            if (_values.Count == 0)
            {
                Definitions = "";
                return;
            }

            Definitions = "const " + string.Join(",", _values.Select(v => string.Format("{0}=global.{0}", v.Key))) + ";";
        }

        public static void Register(MondState state)
        {
            foreach (var kv in _values)
            {
                state[kv.Key] = kv.Value;
            }
        }

        [MondFunction("require")]
        public static MondValue Require(MondState state, string fileName)
        {
            var program = MondProgram.Compile(Definitions + File.ReadAllText(fileName), fileName);
            return state.Load(program);
        }

        [MondFunction("print")]
        public static void Print(params MondValue[] arguments)
        {
            foreach (var v in arguments)
            {
                Console.Write((string)v);
            }
        }

        [MondFunction("printLn")]
        public static void PrintLn(params MondValue[] arguments)
        {
            if (arguments.Length == 0)
                Console.WriteLine();

            foreach (var v in arguments)
            {
                Console.Write((string)v);
                Console.WriteLine();
            }
        }

        [MondFunction("error")]
        public static void Error(string message)
        {
            throw new MondRuntimeException(message);
        }

        [MondFunction("stdin")]
        public static MondValue Stdin()
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
