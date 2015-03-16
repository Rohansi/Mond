using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mond.Debugger;
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
            List<Watch> watches;
            ReadOnlyCollection<MondDebugContext.CallStackEntry> callStack;

            _debugger.GetState(out isRunning, out programs, out position, out watches, out callStack);

            var message = new MondValue(MondValueType.Object);
            message["Type"] = "InitialState";
            message["Programs"] = new MondValue(programs.Select(Utility.JsonProgram));
            message["Running"] = isRunning;
            message["Id"] = position.Id;
            message["StartLine"] = position.StartLine;
            message["StartColumn"] = position.StartColumn;
            message["EndLine"] = position.EndLine;
            message["EndColumn"] = position.EndColumn;
            message["Watches"] = new MondValue(watches.Select(Utility.JsonWatch));
            message["CallStack"] = new MondValue(callStack.Select(Utility.JsonCallStackEntry));

            Send(Json.Serialize(message));
        }

        protected override void OnMessage(MessageEventArgs args)
        {
            if (args.Type != Opcode.Text)
                return;

            try
            {
                var obj = Json.Deserialize(args.Data);

                switch ((string)obj["Type"])
                {
                    case "Action":
                        {
                            var value = (string)obj["Action"];

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
                            var id = (int)obj["Id"];
                            var line = (int)obj["Line"];
                            var value = (bool)obj["Value"];

                            if (_debugger.SetBreakpoint(id, line, value))
                            {
                                var message = new MondValue(MondValueType.Object);
                                message["Type"] = "Breakpoint";
                                message["Id"] = id;
                                message["Line"] = line;
                                message["Value"] = value;

                                Sessions.Broadcast(Json.Serialize(message));
                            }

                            break;
                        }

                    case "AddWatch":
                        {
                            var expression = (string)obj["Expression"];
                            _debugger.AddWatch(expression);
                            break;
                        }

                    case "RemoveWatch":
                        {
                            var id = (int)obj["Id"];
                            _debugger.RemoveWatch(id);
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
