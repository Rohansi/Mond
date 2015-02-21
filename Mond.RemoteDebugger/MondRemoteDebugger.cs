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
        private List<ProgramInfo> _programs;
        private int _watchId;
        private List<Watch> _watches;

        private MondDebugContext _context;
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
            _programs = new List<ProgramInfo>();
            _watchId = 0;
            _watches = new List<Watch>();

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

        protected override MondDebugAction OnBreak(MondDebugContext context, int address)
        {
            // if missing debug info, leave the function
            if (IsMissingDebugInfo(context.DebugInfo))
                return MondDebugAction.StepOut;

            // keep track of program instances
            VisitProgram(context);

            // update where we are in the source code
            UpdateBreakPosition(context, address);

            // broadcast new watch values
            UpdateWatches(context);

            // block until an action is set
            return WaitForAction(context);
        }

        private MondDebugAction WaitForAction(MondDebugContext context)
        {
            TaskCompletionSource<MondDebugAction> breaker;

            lock (_sync)
            {
                if (_breaker != null)
                    throw new InvalidOperationException("Debugger hit breakpoint while waiting on another");

                _context = context;
                _breaker = breaker = new TaskCompletionSource<MondDebugAction>();
            }

            var result = breaker.Task.Result;

            lock (_sync)
            {
                _context = null;
                _breaker = null;
            }

            Broadcast(new
            {
                Type = "State",
                Running = true
            });

            return result;
        }

        internal void GetState(out bool isRunning, out List<ProgramInfo> programs, out BreakPosition position, out List<Watch> watches)
        {
            lock (_sync)
            {
                isRunning = _breaker == null;
                programs = _programs.ToList();
                position = _position;
                watches = _watches.ToList();
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

        internal bool SetBreakpoint(int id, int line, bool value)
        {
            lock (_sync)
            {
                if (id < 0 || id >= _programs.Count)
                    return false;

                var programInfo = _programs[id];

                var statements = programInfo.DebugInfo.Statements
                    .Where(s => s.StartLineNumber == line)
                    .ToList();

                if (statements.Count == 0)
                    return false;

                if (value)
                {
                    // set breakpoint
                    if (programInfo.ContainsBreakpoint(line))
                        return true;

                    programInfo.AddBreakpoint(line);

                    foreach (var statement in statements)
                    {
                        AddBreakpoint(programInfo.Program, statement.Address);
                    }
                }
                else
                {
                    // clear breakpoint
                    if (!programInfo.ContainsBreakpoint(line))
                        return true;

                    programInfo.RemoveBreakpoint(line);

                    foreach (var statement in statements)
                    {
                        RemoveBreakpoint(programInfo.Program, statement.Address);
                    }
                }

                return true;
            }
        }

        internal void AddWatch(string expression)
        {
            Watch watch;

            lock (_sync)
            {
                watch = new Watch(_watchId++, expression);
                _watches.Add(watch);
            }

            watch.Refresh(_context);

            Broadcast(new
            {
                Type = "AddedWatch",
                Id = watch.Id,
                Expression = watch.Expression,
                Value = watch.Value
            });
        }

        internal void RemoveWatch(int id)
        {
            int removed;

            lock (_sync)
                removed = _watches.RemoveAll(w => w.Id == id);

            if (removed == 0)
                return;

            Broadcast(new
            {
                Type = "RemovedWatch",
                Id = id
            });
        }

        private void UpdateWatches(MondDebugContext context)
        {
            List<Watch> watches;

            lock (_sync)
                watches = _watches.ToList();

            foreach (var watch in watches)
            {
                watch.Refresh(context);
            }

            Broadcast(new
            {
                Type = "Watches",
                Watches = watches.Select(w => new
                {
                    Id = w.Id,
                    Expression = w.Expression,
                    Value = w.Value
                })
            });
        }

        private void UpdateBreakPosition(MondDebugContext context, int address)
        {
            var program = context.Program;
            var debugInfo = context.DebugInfo;

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
                var programId = _programs.FindIndex(t => t.Program == program);

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

        private void VisitProgram(MondDebugContext context)
        {
            var program = context.Program;
            var debugInfo = context.DebugInfo;

            int id;

            lock (_sync)
            {
                if (_seenPrograms.Contains(program))
                    return;

                _seenPrograms.Add(program);

                id = _programs.Count;
                _programs.Add(new ProgramInfo(program, debugInfo));
            }

            Broadcast(new
            {
                Type = "NewProgram",
                Id = id,
                FileName = debugInfo.FileName,
                SourceCode = debugInfo.SourceCode,
                FirstLine = Utility.FirstLineNumber(debugInfo),
                Breakpoints = _programs[id].Breakpoints
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
