using System.Threading;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondClass("CancellationToken")]
    internal partial class CancellationTokenClass
    {
        internal CancellationToken CancellationToken { get; }

        internal CancellationTokenClass(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        [MondConstructor]
        public CancellationTokenClass(bool canceled)
        {
            CancellationToken = new CancellationToken(canceled);
        }

        [MondFunction]
        public bool IsCancellationRequested()
        {
            return CancellationToken.IsCancellationRequested;
        }

        [MondFunction]
        public void Register(MondState state, MondValue function)
        {
            if (function.Type != MondValueType.Function)
                throw new MondRuntimeException("register: first argument must be a function");

            CancellationToken.Register(() => state.Call(function));
        }

        [MondFunction]
        public void ThrowIfCancellationRequested()
        {
            CancellationToken.ThrowIfCancellationRequested();
        }
    }
}
