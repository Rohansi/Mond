using System;
using JetBrains.Annotations;

namespace Mond
{
    public class MondRuntimeException : MondException
    {
        public string MondStackTrace { get; internal set; }

        public override string Message
        {
            get
            {
                if (string.IsNullOrWhiteSpace(MondStackTrace))
                    return base.Message;

                return base.Message + Environment.NewLine + MondStackTrace;
            }
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
    }
}
