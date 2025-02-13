using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Require", bareMethods: true)]
    internal partial class RequireModule
    {
        private readonly RequireLibrary _require;

        public RequireModule(RequireLibrary require)
        {
            _require = require ?? throw new ArgumentNullException(nameof(require));
        }

        [MondFunction]
        public MondValue Require(MondState state, string fileName)
        {
            if (_require.Resolver == null)
                throw new MondRuntimeException("require: module resolver is not set");
            if (_require.Loader == null)
                throw new MondRuntimeException("require: module loader is not set");

            const string cacheObjectName = "__modules";

            MondValue cacheObject;

            // make sure we have somewhere to cache modules
            if (state[cacheObjectName].Type != MondValueType.Object)
            {
                cacheObject = MondValue.Object(state);
                state[cacheObjectName] = cacheObject;
            }
            else
            {
                cacheObject = state[cacheObjectName];
            }

            // gather search directories
            IEnumerable<string> searchDirectories =
                _require.SearchDirectories ?? Array.Empty<string>();

            if (_require.SearchBesideScript)
            {
                var currentDir = Path.GetDirectoryName(state.CurrentScript);
                searchDirectories = Enumerable.Repeat(currentDir, 1)
                    .Concat(searchDirectories);
            }
            
            // resolve the module name so we have a consistent caching key
            var resovledName = _require.Resolver(fileName, searchDirectories);

            // return cached module if it exists
            var cachedExports = cacheObject[resovledName];
            if (cachedExports.Type == MondValueType.Object)
                return cachedExports;

            // create a new object to store the exports
            var exports = MondValue.Object(state);

            // instantly cache it so we can have circular dependencies
            cacheObject[resovledName] = exports;

            try
            {
                var moduleSource = _require.Loader(resovledName);

                // wrap the module script in a function so we can pass out exports object to it
                var source = _require.Definitions + "return fun (exports) {\n" + moduleSource + "\n return exports; };";

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

                var program = MondProgram.Compile(source, resovledName, options);
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
                cacheObject[resovledName] = MondValue.Undefined;
                throw;
            }

            return exports;
        }
    }
}
