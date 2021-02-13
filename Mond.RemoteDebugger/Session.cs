using System;
using System.Linq;
using Fleck;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    internal class Session
    {
        private readonly Server _server;
        private readonly MondRemoteDebugger _debugger;
        private readonly IWebSocketConnection _socket;

        public Session(MondRemoteDebugger debugger, Server server, IWebSocketConnection socket)
        {
            _debugger = debugger;
            _server = server;
            _socket = socket;
        }

        public void Send(string data) => _socket.Send(data);

        public void Close() => _socket.Close();

        public void OnOpen()
        {
            _debugger.GetState(
                out var isRunning, out var programs, out var position, out var watches, out var callStack);

            var message = MondValue.Object();
            message["Type"] = "InitialState";
            message["Programs"] = MondValue.Array(programs.Select(Utility.JsonProgram));
            message["Running"] = isRunning;
            message["Id"] = position.Id;
            message["StartLine"] = position.StartLine;
            message["StartColumn"] = position.StartColumn;
            message["EndLine"] = position.EndLine;
            message["EndColumn"] = position.EndColumn;
            message["Watches"] = MondValue.Array(watches.Select(Utility.JsonWatch));

            if (callStack != null)
                message["CallStack"] = _debugger.BuildCallStackArray(callStack);

            Send(Json.Serialize(message));
        }

        public void OnMessage(string data)
        {
            try
            {
                var obj = Json.Deserialize(data);

                switch (obj["Type"])
                {
                    case "Action":
                        {
                            var value = (string)obj["Action"];

                            if (value == "Break")
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
                                var message = MondValue.Object();
                                message["Type"] = "Breakpoint";
                                message["Id"] = id;
                                message["Line"] = line;
                                message["Value"] = value;

                                _server.Broadcast(Json.Serialize(message));
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
                        Console.WriteLine("Unhandled message type: " + obj.Type);
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
                case "Continue":
                    return MondDebugAction.Run;

                case "StepIn":
                    return MondDebugAction.StepInto;

                case "StepOver":
                    return MondDebugAction.StepOver;

                case "StepOut":
                    return MondDebugAction.StepOut;

                default:
                    throw new NotSupportedException("Unknown action: " + value);
            }
        }
    }
}
