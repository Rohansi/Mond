using Mond;
using Mond.Debugger;

namespace TryMond;

public class AutoAbortDebugger : MondDebugger
{
    private int _count;

    protected override bool ShouldBreak(MondProgram program, int address)
    {
        return ++_count >= 1_000_000;
    }

    protected override MondDebugAction OnBreak(MondDebugContext context, int address)
    {
        throw new MondRuntimeException("Execution timed out");
    }
}
