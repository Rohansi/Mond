using System;
using JetBrains.Annotations;

namespace Mond
{
    public class MondRuntimeException : MondException
    {
        public bool HasStackTrace { get; internal set; }

        public MondRuntimeException(string message, bool hasStackTrace = false)
            : base(message)
        {
            HasStackTrace = hasStackTrace;
        }

        public MondRuntimeException(string message, Exception innerException, bool hasStackTrace = false)
            : base(message, innerException)
        {
            HasStackTrace = hasStackTrace;
        }

        [StringFormatMethod("format")]
        public MondRuntimeException(string format, params object[] args)
            : base(string.Format(format, args))
        {
            HasStackTrace = false;
        }
    }
}
