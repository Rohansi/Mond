using System;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    public class MondRemoteDebugger : MondDebugger
    {
        public MondRemoteDebugger()
        {
            IsBreakRequested = true;
        }

        protected override void OnAttached()
        {
            
        }

        protected override void OnDetached()
        {
            
        }

        protected override MondDebugAction OnBreak(MondProgram program, int address, MondDebugInfo debugInfo)
        {
            // if no debug info is available, leave the program
            if (debugInfo == null)
                return MondDebugAction.StepOut;

            Console.Write("dbg> ");
            var action = Console.ReadLine();

            switch (action)
            {
                case "in":
                    return MondDebugAction.StepInto;

                case "out":
                    return MondDebugAction.StepOut;

                case "over":
                    return MondDebugAction.StepOver;

                default:
                    return MondDebugAction.Run;
            }
        }
    }
}
