using System.Threading;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondClass("CancellationToken")]
    internal class CancellationTokenClass
    {
        internal CancellationToken CancellationToken { get; private set; }

        internal CancellationTokenClass(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        [MondConstructor]
        public CancellationTokenClass(bool canceled)
        {
            CancellationToken = new CancellationToken(canceled);
        }

        [MondFunction("isCancellationRequested")]
        public bool IsCancellationRequested()
        {
            return CancellationToken.IsCancellationRequested;
        }

        [MondFunction("register")]
        public void Register(MondState state, MondValue function)
        {
            if (function.Type != MondValueType.Function)
                throw new MondRuntimeException("register: first argument must be a function");

            CancellationToken.Register(() => state.Call(function));
        }

        [MondFunction("throwIfCancellationRequested")]
        public void ThrowIfCancellationRequested()
        {
            CancellationToken.ThrowIfCancellationRequested();
        }
    }
}
