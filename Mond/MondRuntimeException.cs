using System;
using JetBrains.Annotations;

namespace Mond
{
    public class MondRuntimeException : MondException
    {
        internal MondRuntimeException(string message)
            : base(message)
        {
            
        }

        internal MondRuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
            
        }

        [StringFormatMethod("format")]
        internal MondRuntimeException(string format, params object[] args)
            : base(string.Format(format, args))
        {

        }
    }
}
