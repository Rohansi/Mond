using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Proxy", bareMethods: true)]
    internal partial class ProxyModule
    {
        [MondFunction]
        public MondValue ProxyCreate(MondState state, MondValue target, MondValue handler)
        {
            if (handler.Type != MondValueType.Object)
                throw new MondRuntimeException("proxyCreate: handler must be an object");

            return MondValue.ProxyObject(target, handler, state);
        }
    }
}
