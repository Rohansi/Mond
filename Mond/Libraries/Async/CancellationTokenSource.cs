using System;
using System.Threading;
using Mond.Binding;

namespace Mond.Libraries.Async
{
    [MondClass("CancellationTokenSource")]
    internal class CancellationTokenSourceClass
    {
        private CancellationTokenSource _cts;

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

        [MondFunction("isCancellationRequested")]
        public bool IsCancellationRequested()
        {
            return _cts.IsCancellationRequested;
        }

        [MondFunction("getToken")]
        public MondValue GetToken()
        {
            MondValue prototype;
            MondClassBinder.Bind<CancellationTokenClass>(out prototype);

            var instance = new CancellationTokenClass(_cts.Token);

            var obj = new MondValue(MondValueType.Object);
            obj.UserData = instance;
            obj.Prototype = prototype;
            obj.Lock();

            return obj;
        }

        [MondFunction("cancel")]
        public void Cancel()
        {
            _cts.Cancel();
        }

        [MondFunction("cancelAfter")]
        public void CancelAfter(double seconds)
        {
            _cts.CancelAfter(TimeSpan.FromSeconds(seconds));
        }
    }
}
