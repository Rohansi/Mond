using System;
using JetBrains.Annotations;

namespace Mond
{
    public class MondRuntimeException : MondException
    {
        public bool HasStackTrace { get; internal set; }

        public MondRuntimeException(string message)
            : base(message)
        {
            HasStackTrace = false;
        }

        public MondRuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {
            HasStackTrace = false;
        }

        [StringFormatMethod("format")]
        public MondRuntimeException(string format, params object[] args)
            : base(string.Format(format, args))
        {
            HasStackTrace = false;
        }

        internal MondRuntimeException(string message, Exception innerException, bool hasStackTrace)
            : base(message, innerException)
        {
            HasStackTrace = hasStackTrace;
        }
    }
}
