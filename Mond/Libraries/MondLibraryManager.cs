using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mond.Libraries
{
    public class MondLibraryManager : IEnumerable<IMondLibraryCollection>
    {
        public const string LockedError = "Libraries have been loaded and can no longer be changed";

        private readonly List<IMondLibraryCollection> _factories;
        private bool _locked;

        private Action<MondLibraryCollection> _configAction;

        public MondLibraryManager()
        {
            _factories = new List<IMondLibraryCollection>();
            _locked = false;

            _configAction = null;
        }

        /// <summary>
        /// Gets the definition string for the libraries. Will only be
        /// set after the first call to <c>Load</c>.
        /// </summary>
        public string Definitions { get; private set; }

        /// <summary>
        /// Adds a configuration action to the instance.
        /// </summary>
        public MondLibraryManager Configure(Action<MondLibraryCollection> configAction)
        {
            if (configAction == null)
                throw new ArgumentNullException("configAction");

            if (_locked)
                throw new InvalidOperationException(LockedError);

            if (_configAction == null)
            {
                _configAction = configAction;
            }
            else
            {
                _configAction += configAction;
            }

            return this;
        }

        /// <summary>
        /// Loads the libraries into a given state.
        /// </summary>
        public void Load(MondState state, Action<MondLibraryCollection> configAction = null)
        {
            _locked = true;

            if (state == null)
                throw new ArgumentNullException("state");

            var definitionNames = new HashSet<string>();

            var libraries = _factories
                .SelectMany(f => f.Create(state))
                .ToList();

            var definitions = libraries
                .Distinct(new MondLibraryEqualityComparer())
                .SelectMany(l => l.GetDefinitions());

            // copy definitions into state
            foreach (var definition in definitions)
            {
                var key = definition.Key;
                var value = definition.Value;

                if (!definitionNames.Add(key))
                    throw new Exception(string.Format("Duplicate definition for '{0}'", key));

                state[key] = value;
            }

            if (Definitions == null)
            {
                // create the definitions string
                Definitions = MakeDefinitionString(definitionNames);
            }

            var libraryCollection = new MondLibraryCollection(libraries);

            // set default require definitions
            var require = libraryCollection.Get<RequireLibrary>();
            if (require != null)
            {
                require.Definitions = Definitions;
            }

            // call instance config action
            _configAction?.Invoke(libraryCollection);

            // call config action
            configAction?.Invoke(libraryCollection);
        }

        private static string MakeDefinitionString(ICollection<string> definitions)
        {
            if (definitions.Count == 0)
                return "\n";

            var sb = new StringBuilder();

            sb.Append("const ");

            var first = true;
            foreach (var d in definitions)
            {
                if (first)
                    first = false;
                else
                    sb.Append(", ");

                sb.Append(d);
                sb.Append(" = global.");
                sb.Append(d);
            }

            sb.AppendLine(";");

            return sb.ToString();
        }

        public void Add(IMondLibraryCollection item)
        {
            if (item == null)
                throw new ArgumentNullException("item");

            if (_locked)
                throw new InvalidOperationException(LockedError);

            _factories.Add(item);
        }

        public IEnumerator<IMondLibraryCollection> GetEnumerator()
        {
            return _factories.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        class MondLibraryEqualityComparer : IEqualityComparer<IMondLibrary>
        {
            public bool Equals(IMondLibrary x, IMondLibrary y)
            {
                return x.GetType() == y.GetType();
            }

            public int GetHashCode(IMondLibrary obj)
            {
                return obj.GetType().GetHashCode();
            }
        }
    }

    public class MondLibraryCollection : IEnumerable<IMondLibrary>
    {
        private readonly List<IMondLibrary> _libraries;
         
        internal MondLibraryCollection(List<IMondLibrary> libraries)
        {
            _libraries = libraries;
        }

        public T Get<T>() where T : IMondLibrary
        {
            return _libraries.OfType<T>().FirstOrDefault();
        }

        public IEnumerator<IMondLibrary> GetEnumerator()
        {
            return _libraries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
