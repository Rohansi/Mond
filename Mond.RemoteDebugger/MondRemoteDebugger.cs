using System;
using System.Collections.Generic;
using System.Linq;
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

        private readonly object _sync = new object();
        private HashSet<MondProgram> _seenPrograms;
        private List<Tuple<MondProgram, MondDebugInfo>> _programs;
        private TaskCompletionSource<MondDebugAction> _breaker;
        private BreakPosition _position;

        public MondRemoteDebugger(IPEndPoint endPoint)
        {
            _server = new WebSocketServer(endPoint.Address, endPoint.Port);
            _server.KeepClean = true;

            _server.AddWebSocketService("/", () => new Session(this));

            _server.Start();
        }

        public void RequestBreak()
        {
            IsBreakRequested = true;
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

        protected override void OnAttached()
        {
            _seenPrograms = new HashSet<MondProgram>();
            _programs = new List<Tuple<MondProgram, MondDebugInfo>>();
            _breaker = null;
        }

        protected override void OnDetached()
        {
            TaskCompletionSource<MondDebugAction> breaker;

            lock (_sync)
            {
                _seenPrograms = null;
                _programs = null;

                breaker = _breaker;
            }

            if (breaker != null)
                breaker.SetResult(MondDebugAction.Run);
        }

        protected override MondDebugAction OnBreak(MondProgram program, MondDebugInfo debugInfo, int address)
        {
            // if missing debug info, leave the function
            if (IsMissingDebugInfo(debugInfo))
                return MondDebugAction.StepOut;

            // keep track of program instances
            VisitProgram(program, debugInfo);

            // update where we are in the source code
            UpdateBreakPosition(program, debugInfo, address);

            // block until an action is set
            return WaitForAction();
        }

        private MondDebugAction WaitForAction()
        {
            TaskCompletionSource<MondDebugAction> breaker;

            lock (_sync)
            {
                if (_breaker != null)
                    throw new InvalidOperationException("Debugger hit breakpoint while waiting on another");

                _breaker = breaker = new TaskCompletionSource<MondDebugAction>();
            }

            var result = breaker.Task.Result;

            lock (_sync)
                _breaker = null;

            Broadcast(new
            {
                Type = "State",
                Running = true
            });

            return result;
        }

        internal void GetState(out bool isRunning, out List<Tuple<MondProgram, MondDebugInfo>> programs, out BreakPosition position)
        {
            lock (_sync)
            {
                isRunning = _breaker == null;
                programs = _programs.ToList();
                position = _position;
            }
        }

        internal void PerformAction(MondDebugAction action)
        {
            TaskCompletionSource<MondDebugAction> breaker;

            lock (_sync)
                breaker = _breaker;

            if (breaker != null)
                breaker.SetResult(action);
        }

        private void UpdateBreakPosition(MondProgram program, MondDebugInfo debugInfo, int address)
        {
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
                    statement = new MondDebugInfo.Statement(0, -1, -1, -1, -1);
                }
            }

            object message;

            lock (_sync)
            {
                var stmtValue = statement.Value;
                var programId = _programs.FindIndex(t => t.Item1 == program);

                _position = new BreakPosition(
                    programId,
                    stmtValue.StartLineNumber,
                    stmtValue.StartColumnNumber,
                    stmtValue.EndLineNumber,
                    stmtValue.EndColumnNumber);

                message = new
                {
                    Type = "State",
                    Running = false,
                    Id = _position.Id,
                    StartLine = _position.StartLine,
                    StartColumn = _position.StartColumn,
                    EndLine = _position.EndLine,
                    EndColumn = _position.EndColumn
                };
            }

            Broadcast(message);
        }

        private void VisitProgram(MondProgram program, MondDebugInfo debugInfo)
        {
            int id;

            lock (_sync)
            {
                if (_seenPrograms.Contains(program))
                    return;

                _seenPrograms.Add(program);

                id = _programs.Count;
                _programs.Add(Tuple.Create(program, debugInfo));
            }

            Broadcast(new
            {
                Type = "NewProgram",
                Id = id,
                FileName = debugInfo.FileName,
                SourceCode = debugInfo.SourceCode,
                FirstLine = Utility.FirstLineNumber(debugInfo)
            });
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
    }
}
