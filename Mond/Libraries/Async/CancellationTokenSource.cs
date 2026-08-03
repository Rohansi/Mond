using System;
using System.Threading;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    /// <summary>
    /// Hands out cancellation tokens and decides when they are cancelled.
    /// </summary>
    [MondClass("CancellationTokenSource")]
    internal partial class CancellationTokenSourceClass
    {
        private readonly CancellationTokenSource _cts;

        /// <summary>
        /// Creates a source, cancelling itself after the given number of seconds when one is given.
        /// </summary>
        [MondConstructor]
        public CancellationTokenSourceClass()
        {
            _cts = new CancellationTokenSource();
        }

        /// <summary>
        /// Creates a source, cancelling itself after the given number of seconds when one is given.
        /// </summary>
        [MondConstructor]
        public CancellationTokenSourceClass(double seconds)
        {
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        }

        /// <summary>
        /// Returns true once cancellation has been requested.
        /// </summary>
        [MondFunction]
        public bool IsCancellationRequested()
        {
            return _cts.IsCancellationRequested;
        }

        /// <summary>
        /// Returns the token that this source cancels.
        /// </summary>
        [MondFunction]
        public CancellationTokenClass GetToken()
        {
            return new CancellationTokenClass(_cts.Token);
        }

        /// <summary>
        /// Requests cancellation immediately.
        /// </summary>
        [MondFunction]
        public void Cancel()
        {
            _cts.Cancel();
        }

        /// <summary>
        /// Requests cancellation after the given number of seconds.
        /// </summary>
        [MondFunction]
        public void CancelAfter(double seconds)
        {
            _cts.CancelAfter(TimeSpan.FromSeconds(seconds));
        }
    }
}
