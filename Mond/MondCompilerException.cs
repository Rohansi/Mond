using JetBrains.Annotations;

namespace Mond
{
    public class MondCompilerException : MondException
    {
        [StringFormatMethod("format")]
        internal MondCompilerException(string fileName, int line, string format, params object[] args)
            : base(string.Format("{0}(line {1}): {2}", fileName ?? "null", line, string.Format(format, args)))
        {
            
        }
    }
}
