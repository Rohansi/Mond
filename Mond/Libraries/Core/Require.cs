using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondClass("Require")]
    internal class RequireClass
    {
        private RequireLibrary _require;

        public static MondValue Create(MondState state, RequireLibrary require)
        {
            MondClassBinder.Bind<RequireClass>(state, out var prototype);

            var instance = new RequireClass();
            instance._require = require;

            var obj = MondValue.Object();
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction]
        public MondValue Require(MondState state, string fileName)
        {
            if (_require.Loader == null)
                throw new MondRuntimeException("require: module loader is not set");

            const string cacheObjectName = "__modules";

            MondValue cacheObject;

            // make sure we have somewhere to cache modules
            if (state[cacheObjectName].Type != MondValueType.Object)
            {
                cacheObject = MondValue.Object(state);
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
            var exports = MondValue.Object(state);
            exports.Prototype = MondValue.Null;

            // instantly cache it so we can have circular dependencies
            cacheObject[fileName] = exports;

            try
            {
                IEnumerable<string> searchDirectories =
                    _require.SearchDirectories ?? Array.Empty<string>();

                if (_require.SearchBesideScript)
                {
                    var currentDir = Path.GetDirectoryName(state.CurrentScript);
                    searchDirectories = Enumerable.Repeat(currentDir, 1)
                        .Concat(searchDirectories);
                }

                var moduleSource = _require.Loader(fileName, searchDirectories);

                // wrap the module script in a function so we can pass out exports object to it
                var source = _require.Definitions + "return fun (exports) {\n" + moduleSource + " return exports; };";

                var options = new MondCompilerOptions
                {
                    FirstLineNumber = -1
                };

                var requireOptions = _require.Options;
                if (requireOptions != null)
                {
                    options.DebugInfo = requireOptions.DebugInfo;
                    options.MakeRootDeclarationsGlobal = requireOptions.MakeRootDeclarationsGlobal;
                    options.UseImplicitGlobals = requireOptions.UseImplicitGlobals;
                }

                var program = MondProgram.Compile(source, fileName, options);
                var initializer = state.Load(program);

                var result = state.Call(initializer, exports);

                if (result.Type != MondValueType.Object)
                    throw new MondRuntimeException("require: module must return an object (`{0}`)", fileName);

                if (!ReferenceEquals(exports.AsDictionary, result.AsDictionary))
                {
                    // module returned a different object, merge with ours
                    foreach (var kv in result.AsDictionary)
                    {
                        var key = kv.Key;
                        var value = kv.Value;

                        exports[key] = value;
                    }

                    exports.Prototype = result.Prototype;

                    if (result.IsLocked)
                        exports.Lock();
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
    }
}
