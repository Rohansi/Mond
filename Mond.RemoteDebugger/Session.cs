using System;
using System.Collections.Generic;
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
            bool isRunning;
            List<ProgramInfo> programs;
            BreakPosition position;

            _debugger.GetState(out isRunning, out programs, out position);

            Send(JsonConvert.SerializeObject(new
            {
                Type = "InitialState",
                Programs = programs.Select(t => new
                {
                    FileName = t.DebugInfo.FileName,
                    SourceCode = t.DebugInfo.SourceCode,
                    FirstLine = Utility.FirstLineNumber(t.DebugInfo),
                    Breakpoints = t.Breakpoints
                }),

                Running = isRunning,
                Id = position.Id,
                StartLine = position.StartLine,
                StartColumn = position.StartColumn,
                EndLine = position.EndLine,
                EndColumn = position.EndColumn
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
                        {
                            var value = (string)obj.Action;

                            if (value == "break")
                            {
                                _debugger.RequestBreak();
                                break;
                            }

                            _debugger.PerformAction(ParseAction(value));
                            break;
                        }

                    case "SetBreakpoint":
                        {
                            var id = (int)obj.Id;
                            var line = (int)obj.Line;
                            var value = (bool)obj.Value;

                            if (_debugger.SetBreakpoint(id, line, value))
                            {
                                Sessions.Broadcast(JsonConvert.SerializeObject(new
                                {
                                    Type = "Breakpoint",
                                    Id = id,
                                    Line = line,
                                    Value = value
                                }));
                            }

                            break;
                        }

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
