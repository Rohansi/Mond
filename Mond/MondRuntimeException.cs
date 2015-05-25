using System;
using JetBrains.Annotations;

namespace Mond
{
    public class MondRuntimeException : MondException
    {
        internal string InternalStackTrace;

        public override string StackTrace
        {
            get { return InternalStackTrace; }
        }

        public MondRuntimeException(string message)
            : base(message)
        {

        }

        public MondRuntimeException(string message, Exception innerException)
            : base(message, innerException)
        {

        }

        [StringFormatMethod("format")]
        public MondRuntimeException(string format, params object[] args)
            : base(string.Format(format, args))
        {

        }

        public override string ToString()
        {
            if (StackTrace == null)
                return Message;

            return Message + Environment.NewLine + StackTrace;
        }
    }
}
