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
            const string cacheObjectName = "__modules";

            MondValue cacheObject;

            // make sure we have somewhere to cache modules
            if (state[cacheObjectName].Type != MondValueType.Object)
            {
                cacheObject = new MondValue(state);
                cacheObject.Prototype = MondValue.Null;
                state[cacheObjectName] = cacheObject;
            }
            else
            {
                cacheObject = state[cacheObjectName];
            }

            // return cached module if it exists
            var cachedExports = cacheObject[fileName];
            if (cachedExports.Type == MondValueType.Object)
                return cachedExports;

            // create a new object to store the exports
            var exports = new MondValue(state);
            exports.Prototype = MondValue.Null;

            // instantly cache it so we can have circular dependencies
            cacheObject[fileName] = exports;

            try
            {
                // wrap the module script in a function so we can pass out exports object to it
                var moduleSource = File.ReadAllText(fileName);
                var source = Definitions + "return fun Module(exports) {" + moduleSource + "return exports; };";

                var program = MondProgram.Compile(source, fileName);
                var initializer = state.Load(program);

                var result = state.Call(initializer, exports);

                if (result.Type != MondValueType.Object)
                    throw new MondRuntimeException("Modules must return an Object");

                if (!ReferenceEquals(exports, result))
                {
                    // module returned a different object, merge with ours
                    exports.Prototype = result.Prototype;

                    foreach (var kv in result.RawEnumerate())
                    {
                        var key = kv["key"];
                        var value = kv["value"];

                        exports[key] = value;
                    }
                }
            }
            catch
            {
                // if something goes wrong, remove the entry from the cache
                cacheObject[fileName] = MondValue.Undefined;
                throw;
            }

            return exports;
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

        [MondFunction("readLn")]
        public static string ReadLn()
        {
            return Console.ReadLine();
        }

        [MondFunction("error")]
        public static void Error(string message)
        {
            throw new MondRuntimeException(message);
        }
    }
}
