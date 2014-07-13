using System;

namespace Mond
{
    public class MondException : Exception
    {
        internal MondException(string message)
            : base(message)
        {
            
        }

        internal MondException(string message, Exception innerException)
            : base(message, innerException)
        {
            
        }
    }
}
