using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Mond.VirtualMachine
{
    internal sealed class Object
    {
        [Flags]
        private enum Flags
        {
            Locked = 1 << 0,
            HasPrototype = 1 << 1,
            IsProxy = 1 << 2,
        }

        private Flags _flags;

        public readonly Dictionary<MondValue, MondValue> Values;
        public MondValue Prototype;
        public object UserData;

        private MondState _dispatcherState;

        public MondState State
        {
            get => _dispatcherState;
            set => _dispatcherState ??= value;
        }

        public MondValue ProxyTarget =>
            IsProxy
                ? Prototype
                : throw new InvalidOperationException("Object is not a proxy, cannot get target");

        public bool Locked
        {
            get => _flags.HasFlag(Flags.Locked);
            set => SetFlag(ref _flags, Flags.Locked, value);
        }

        public bool HasPrototype
        {
            get => _flags.HasFlag(Flags.HasPrototype);
            set => SetFlag(ref _flags, Flags.HasPrototype, value);
        }

        public bool IsProxy
        {
            get => _flags.HasFlag(Flags.IsProxy);
            set => SetFlag(ref _flags, Flags.IsProxy, value);
        }

        public Object()
        {
            Values = new Dictionary<MondValue, MondValue>();
            Prototype = MondValue.Undefined;
            UserData = null;
        }

        public Object(MondValue target, Dictionary<MondValue, MondValue> handler, MondState state)
        {
            Prototype = target;
            Values = handler ?? throw new ArgumentNullException(nameof(handler));
            _dispatcherState = state ?? throw new ArgumentNullException(nameof(state));
            IsProxy = true;
        }

        private static void SetFlag(ref Flags flags, Flags flag, bool value)
        {
            if (value)
            {
                flags |= flag;
            }
            else
            {
                flags &= ~flag;
            }
        }
    }
}
