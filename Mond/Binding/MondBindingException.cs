using System;
using JetBrains.Annotations;

namespace Mond.Binding
{
    public class MondBindingException : Exception
    {
        [StringFormatMethod("format")]
        internal MondBindingException(string format, params object[] args)
            : base(string.Format(format, args))
        {
            
        }
    }
}
