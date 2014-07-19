using System;
using JetBrains.Annotations;

namespace Mond
{
    public class MondRuntimeException : MondException
    {
        internal bool HasStackTrace;

        internal MondRuntimeException(string message, bool hasStackTrace = false)
            : base(message)
        {
            HasStackTrace = hasStackTrace;
        }

        internal MondRuntimeException(string message, Exception innerException, bool hasStackTrace = false)
            : base(message, innerException)
        {
            HasStackTrace = hasStackTrace;
        }

        [StringFormatMethod("format")]
        internal MondRuntimeException(string format, params object[] args)
            : base(string.Format(format, args))
        {
            HasStackTrace = false;
        }
    }
}
