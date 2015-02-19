using System;
using System.Dynamic;
using System.Linq;
using Mond.Debugger;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Mond.RemoteDebugger
{
    internal class Session : WebSocketBehavior
    {
        private readonly MondRemoteDebugger _debugger;

        public Session(MondRemoteDebugger debugger)
        {
            _debugger = debugger;
        }

        protected override void OnOpen()
        {
            Send(JsonConvert.SerializeObject(new
            {
                Type = "InitialState",
                Programs = _debugger.Programs.Select(t => new
                {
                    FirstLineNumber = MondRemoteDebugger.FirstLineNumber(t.Item2),
                    FileName = t.Item2.FileName,
                    SourceCode = t.Item2.SourceCode
                }),
                Running = _debugger.Break == null,
                BreakLine = _debugger.BreakLine,
                BreakColumn = _debugger.BreakColumn
            }));
        }

        protected override void OnMessage(MessageEventArgs args)
        {
            if (args.Type != Opcode.Text)
                return;

            try
            {
                var obj = JsonConvert.DeserializeObject<dynamic>(args.Data);

                switch ((string)obj.Type)
                {
                    case "Action":
                        var value = (string)obj.Action;
                        var breaker = _debugger.Break;

                        if (breaker == null)
                        {
                            if (value != "break")
                                throw new NotSupportedException("unhandled action: " + value);

                            _debugger.RequestBreak();
                            break;
                        }

                        breaker.SetResult(ParseAction(value));
                        break;

                    default:
                        Console.WriteLine("unhandled message type: " + obj.Type);
                        break;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static MondDebugAction ParseAction(string value)
        {
            switch (value)
            {
                case "run":
                    return MondDebugAction.Run;

                case "step-in":
                    return MondDebugAction.StepInto;

                case "step-over":
                    return MondDebugAction.StepOver;

                case "step-out":
                    return MondDebugAction.StepOut;

                default:
                    throw new NotSupportedException("unknown action: " + value);
            }
        }
    }
}
