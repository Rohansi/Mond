using System;
using System.Linq;
using Fleck;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    internal class Session
    {
        private readonly MondRemoteDebugger _debugger;
        private readonly IWebSocketConnection _socket;

        public Session(MondRemoteDebugger debugger, IWebSocketConnection socket)
        {
            _debugger = debugger;
            _socket = socket;
        }

        public void Send(string data) => _socket.Send(data);

        public void Close() => _socket.Close();

        public void OnOpen()
        {
            _debugger.GetState(out var isRunning, out _);

            var message = MondValue.Object();
            message["type"] = "initialState";
            message["version"] = MondRemoteDebugger.ProtocolVersion;
            message["isRunning"] = isRunning;

            Send(Json.Serialize(message));
        }

        public void OnMessage(string data)
        {
            MondValue obj;
            try
            {
                obj = Json.Deserialize(data);
            }
            catch
            {
                return;
            }

            try
            {
                switch (obj["type"])
                {
                    case "action":
                    {
                        var value = (string)obj["action"];

                        if (value == "break")
                        {
                            _debugger.RequestBreak();
                            break;
                        }

                        _debugger.PerformAction(ParseAction(value));
                        ReplyWithOk();
                        break;
                    }

                    case "stackTrace":
                    {
                        var stackFrames = _debugger.GetStackFramesArray();
                        var response = MondValue.Object();
                        response["stackFrames"] = stackFrames;
                        ReplyWithOk(response);
                        break;
                    }

                    case "setBreakpoints":
                    {
                        var programId = GetProgramId();
                        var breakpoints = obj["breakpoints"].AsList
                            .Select(o => (Line: (int)o["line"], Column: o.GetInt("column")));

                        var breakpointStatements = _debugger.SetBreakpoints(programId, breakpoints);

                        var response = MondValue.Object();
                        response["programId"] = programId;
                        response["breakpoints"] = MondValue.Array(breakpointStatements.Select(Utility.JsonBreakpoint));
                        ReplyWithOk(response);
                        break;
                    }

                    case "getBreakpointLocations":
                    {
                        var programId = GetProgramId();
                        var startLine = (int)obj["line"];
                        var startColumn = obj.GetInt("column") ?? int.MinValue;
                        var endLine = obj.GetInt("endLine") ?? startLine;
                        var endColumn = obj.GetInt("endColumn") ?? int.MaxValue;

                        var breakpointLocations = _debugger.GetBreakpointLocations(programId, startLine, startColumn, endLine, endColumn);

                        var response = MondValue.Object();
                        response["programId"] = programId;
                        response["locations"] = MondValue.Array(breakpointLocations.Select(Utility.JsonBreakpoint));
                        ReplyWithOk(response);
                        break;
                    }

                    case "eval":
                    {
                        var expression = (string)obj["expression"];
                        var value = string.IsNullOrEmpty(expression)
                            ? _debugger.GetLocals()
                            : _debugger.Evaluate(expression);

                        var response = MondValue.Object();
                        response["value"] = value.ToString();
                        response["type"] = value.Type.GetName();
                        response["properties"] = Utility.JsonValueProperties(value);
                        ReplyWithOk(response);
                        break;
                    }

                    default:
                    {
                        Console.WriteLine("Unhandled message type: " + obj.Type);

                        var response = MondValue.Object();
                        response["status"] = "error";
                        response["error"] = $"unhandled type {obj["type"]}";
                        ReplyWith(response);

                        break;
                    }
                }

                int GetProgramId()
                {
                    var programId = obj["programId"];
                    if (programId.Type == MondValueType.Number)
                        return (int)programId;

                    var programPath = obj["programPath"];
                    if (programPath.Type == MondValueType.String)
                        return _debugger.FindProgramIndex(programPath);

                    throw new InvalidOperationException("Both Program ID and Program Path were unspecified");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                var response = MondValue.Object();
                response["status"] = "error";
                response["error"] = e.Message;
                ReplyWith(response);
            }

            void ReplyWithOk(MondValue responseObj = default)
            {
                if (responseObj.Type != MondValueType.Object)
                    responseObj = MondValue.Object();

                responseObj["status"] = "ok";
                ReplyWith(responseObj);
            }

            void ReplyWith(MondValue responseObj)
            {
                responseObj["seq"] = obj["seq"];
                Send(Json.Serialize(responseObj));
            }
        }

        private static MondDebugAction ParseAction(string value)
        {
            switch (value)
            {
                case "continue":
                    return MondDebugAction.Run;

                case "stepIn":
                    return MondDebugAction.StepInto;

                case "stepOver":
                    return MondDebugAction.StepOver;

                case "stepOut":
                    return MondDebugAction.StepOut;

                default:
                    throw new NotSupportedException("Unknown action: " + value);
            }
        }
    }
}
