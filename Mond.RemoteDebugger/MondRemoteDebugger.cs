using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Mond.Debugger;
using Newtonsoft.Json;
using WebSocketSharp.Server;

namespace Mond.RemoteDebugger
{
    public class MondRemoteDebugger : MondDebugger, IDisposable
    {
        private WebSocketServer _server;
        private HashSet<MondProgram> _seenPrograms;

        internal TaskCompletionSource<MondDebugAction> Break;
        internal List<Tuple<MondProgram, MondDebugInfo>> Programs;

        internal int BreakId;
        internal int BreakStartLine;
        internal int BreakStartColumn;
        internal int BreakEndLine;
        internal int BreakEndColumn;

        public MondRemoteDebugger(IPEndPoint endPoint)
        {
            _server = new WebSocketServer(endPoint.Address, endPoint.Port);
            _server.KeepClean = true;

            _server.AddWebSocketService("/", () => new Session(this));

            _server.Start();
        }

        public void Dispose()
        {
            _server.Stop();

            var service = _server.WebSocketServices["/"];

            foreach (var sessionId in service.Sessions.IDs)
            {
                service.Sessions.CloseSession(sessionId);
            }

            _server = null;
        }

        public void RequestBreak()
        {
            IsBreakRequested = true;
        }

        protected override void OnAttached()
        {
            _seenPrograms = new HashSet<MondProgram>();
            Programs = new List<Tuple<MondProgram, MondDebugInfo>>();
        }

        protected override void OnDetached()
        {
            _seenPrograms = null;

            if (Break != null)
                Break.SetResult(MondDebugAction.Run);
        }

        protected override MondDebugAction OnBreak(MondProgram program, MondDebugInfo debugInfo, int address)
        {
            // if missing debug info, leave the function
            if (IsMissingDebugInfo(debugInfo))
                return MondDebugAction.StepOut;

            // check if we stopped in a new program
            if (!_seenPrograms.Contains(program))
            {
                _seenPrograms.Add(program);

                var id = Programs.Count;
                Programs.Add(Tuple.Create(program, debugInfo));

                Broadcast(new
                {
                    Type = "NewProgram",
                    Id = id,
                    FileName = debugInfo.FileName,
                    SourceCode = debugInfo.SourceCode,
                    FirstLine = FirstLineNumber(debugInfo)
                });
            }

            // find out where we are in the source code
            var statement = debugInfo.FindStatement(address);

            if (!statement.HasValue)
            {
                var position = debugInfo.FindPosition(address);

                if (position.HasValue)
                {
                    var line = position.Value.LineNumber;
                    var column = position.Value.ColumnNumber;
                    statement = new MondDebugInfo.Statement(0, line, column, line, column);
                }
                else
                {
                    statement = new MondDebugInfo.Statement(0, -100, -100, -100, -100);
                }
            }

            BreakId = Programs.FindIndex(t => t.Item1 == program);
            BreakStartLine = statement.Value.StartLineNumber;
            BreakStartColumn = statement.Value.StartColumnNumber;
            BreakEndLine = statement.Value.EndLineNumber;
            BreakEndColumn = statement.Value.EndColumnNumber;

            Broadcast(new
            {
                Type = "State",
                Running = false,
                Id = BreakId,
                StartLine = BreakStartLine,
                StartColumn = BreakStartColumn,
                EndLine = BreakEndLine,
                EndColumn = BreakEndColumn
            });

            // block until an action is set
            Break = new TaskCompletionSource<MondDebugAction>();
            var result = Break.Task.Result;
            Break = null;

            Broadcast(new
            {
                Type = "State",
                Running = true
            });

            return result;
        }

        private void Broadcast(object message)
        {
            var data = JsonConvert.SerializeObject(message);
            _server.WebSocketServices["/"].Sessions.Broadcast(data);
        }

        private static bool IsMissingDebugInfo(MondDebugInfo debugInfo)
        {
            return
                debugInfo == null ||
                debugInfo.FileName == null ||
                debugInfo.SourceCode == null ||
                debugInfo.Functions == null ||
                debugInfo.Lines == null ||
                debugInfo.Statements == null ||
                debugInfo.Scopes == null;
        }

        internal static int FirstLineNumber(MondDebugInfo debugInfo)
        {
            var lines = debugInfo.Lines;
            var firstLineNumber = lines.Count > 0 ? lines[0].LineNumber : 1;
            return firstLineNumber;
        }
    }
}
