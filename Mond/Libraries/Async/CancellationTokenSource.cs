﻿using System;
using System.Threading;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondClass("CancellationTokenSource")]
    internal partial class CancellationTokenSourceClass
    {
        private readonly CancellationTokenSource _cts;

        [MondConstructor]
        public CancellationTokenSourceClass()
        {
            _cts = new CancellationTokenSource();
        }

        [MondConstructor]
        public CancellationTokenSourceClass(double seconds)
        {
            _cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        }

        [MondFunction]
        public bool IsCancellationRequested()
        {
            return _cts.IsCancellationRequested;
        }

        [MondFunction]
        public CancellationTokenClass GetToken()
        {
            return new CancellationTokenClass(_cts.Token);
        }

        [MondFunction]
        public void Cancel()
        {
            _cts.Cancel();
        }

        [MondFunction]
        public void CancelAfter(double seconds)
        {
            _cts.CancelAfter(TimeSpan.FromSeconds(seconds));
        }
    }
}
