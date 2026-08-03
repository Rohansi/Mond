using Mond.Binding;

namespace Mond.Libraries.Core
{
    [MondModule("Proxy", bareMethods: true)]
    internal partial class ProxyModule
    {
        /// <summary>
        /// Wraps the target in an object that routes member access through the handler.
        /// </summary>
        [MondFunction]
        public MondValue ProxyCreate(MondState state, MondValue target, MondValue handler)
        {
            if (handler.Type != MondValueType.Object)
                throw new MondRuntimeException("proxyCreate: handler must be an object");

            return MondValue.ProxyObject(target, handler, state);
        }
    }
}
