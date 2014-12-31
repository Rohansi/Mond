using System.Collections.Generic;
using Mond.Binding;
using Mond.Libraries.Math;

namespace Mond.Libraries
{
    /// <summary>
    /// Contains all of the standard math libraries.
    /// </summary>
    public class MathLibraries : IMondLibraryCollection
    {
        public IEnumerable<IMondLibrary> Create(MondState state)
        {
            yield return new MathLibrary();
            yield return new RandomLibrary();
        }
    }

    /// <summary>
    /// Library containing the <c>Math</c> object.
    /// </summary>
    public class MathLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var mathModule = MondModuleBinder.Bind<MondMath>();

            mathModule["PI"] = System.Math.PI;
            mathModule["E"] = System.Math.E;

            mathModule.Lock();

            yield return new KeyValuePair<string, MondValue>("Math", mathModule);
        }
    }

    /// <summary>
    /// Library containing the <c>Random</c> class.
    /// </summary>
    public class RandomLibrary : IMondLibrary
    {
        public IEnumerable<KeyValuePair<string, MondValue>> GetDefinitions()
        {
            var randomClass = MondClassBinder.Bind<MondRandom>();
            yield return new KeyValuePair<string, MondValue>("Random", randomClass);
        }
    }
}
