using JetBrains.Annotations;

namespace Mond
{
    public class MondCompilerException : MondException
    {
        [StringFormatMethod("format")]
        internal MondCompilerException(string fileName, int line, int column, string format, params object[] args)
            : base(string.Format("{0}(line {1}:{2}): {3}", fileName ?? "null", line, column, string.Format(format, args)))
        {
            
        }
    }
}
