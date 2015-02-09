using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondClass("")]
    internal class RequireClass
    {
        private RequireLibrary _require;

        public static MondValue Create(RequireLibrary require)
        {
            MondValue prototype;
            MondClassBinder.Bind<RequireClass>(out prototype);

            var instance = new RequireClass();
            instance._require = require;

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction("require")]
        public MondValue Require(MondState state, string fileName)
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
                var moduleSource = _require.Loader(fileName);
                var source = _require.Definitions + "return fun (exports) {\n" + moduleSource + " return exports; };";

                var options = new MondCompilerOptions
                {
                    FirstLineNumber = -1
                };

                var program = MondProgram.Compile(source, fileName, options);
                var initializer = state.Load(program);

                var result = state.Call(initializer, exports);

                if (result.Type != MondValueType.Object)
                    throw new MondRuntimeException("Modules must return an Object");

                if (!ReferenceEquals(exports, result))
                {
                    // module returned a different object, merge with ours
                    foreach (var kv in result.Object)
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
