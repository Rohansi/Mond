using System.Threading;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    /// <summary>
    /// Signals to running tasks that they should stop.
    /// </summary>
    [MondClass("CancellationToken")]
    internal partial class CancellationTokenClass
    {
        internal CancellationToken CancellationToken { get; }

        internal CancellationTokenClass(CancellationToken cancellationToken)
        {
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// Creates a token that is either already cancelled or can never be cancelled.
        /// </summary>
        [MondConstructor]
        public CancellationTokenClass(bool canceled)
        {
            CancellationToken = new CancellationToken(canceled);
        }

        /// <summary>
        /// Returns true once cancellation has been requested.
        /// </summary>
        [MondFunction]
        public bool IsCancellationRequested()
        {
            return CancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// Calls the given function when cancellation is requested.
        /// </summary>
        [MondFunction]
        public void Register(MondState state, MondValue function)
        {
            if (function.Type != MondValueType.Function)
                throw new MondRuntimeException("register: first argument must be a function");

            CancellationToken.Register(() => state.Call(function));
        }

        /// <summary>
        /// Throws an error when cancellation has been requested, otherwise does nothing.
        /// </summary>
        [MondFunction]
        public void ThrowIfCancellationRequested()
        {
            CancellationToken.ThrowIfCancellationRequested();
        }
    }
}
