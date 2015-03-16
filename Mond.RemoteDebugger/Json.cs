using System;
using Mond.Libraries;

namespace Mond.RemoteDebugger
{
    static class Json
    {
        public static readonly Func<MondValue, string> Serialize;
        public static readonly Func<string, MondValue> Deserialize;

        static Json()
        {
            var state = new MondState
            {
                Libraries = new MondLibraryManager
                {
                    new JsonLibraries()
                }
            };

            state.EnsureLibrariesLoaded();

            Serialize = obj => state.Call(state["Json"]["serialize"], obj);
            Deserialize = obj => state.Call(state["Json"]["deserialize"], obj);
        }
    }
}
