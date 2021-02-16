using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    public class MondRemoteDebugger : MondDebugger, IDisposable
    {
        internal const int ProtocolVersion = 1;

        private readonly Server _server;
        
        private readonly List<Watch> _watches;
        private SemaphoreSlim _watchSemaphore;
        private bool _watchTimedOut;

        private MondDebugContext _context;
        private TaskCompletionSource<MondDebugAction> _breaker;
        private BreakPosition _position;

        public MondRemoteDebugger(IPEndPoint endpoint)
        {
            _server = new Server(this, endpoint);
            
            _watches = new List<Watch>();
        }

        public void RequestBreak()
        {
            IsBreakRequested = true;
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        protected override void OnAttached()
        {
            _watchSemaphore = new SemaphoreSlim(1);
            _watchTimedOut = false;
            _breaker = null;
        }

        protected override void OnDetached()
        {
            TaskCompletionSource<MondDebugAction> breaker;

            lock (SyncRoot)
            {
                breaker = _breaker;
            }

            breaker?.SetResult(MondDebugAction.Run);
        }

        protected override MondDebugAction OnBreak(MondDebugContext context, int address)
        {
            if (_watchTimedOut)
            {
                _watchTimedOut = false;
                throw new MondRuntimeException("Execution timed out");
            }

            if (_watchSemaphore.CurrentCount == 0)
                return MondDebugAction.Run;

            // if missing debug info, leave the function
            if (IsMissingDebugInfo(context.Program.DebugInfo))
                return MondDebugAction.StepOut;

            lock (SyncRoot)
            {
                if (_breaker != null)
                    throw new InvalidOperationException("Debugger hit breakpoint while waiting on another");

                _context = context;
                _breaker = new TaskCompletionSource<MondDebugAction>();
            }

            // update the current state
            UpdateState(context, address);

            // block until an action is set
            return WaitForAction();
        }

        private MondDebugAction WaitForAction()
        {
            var result = _breaker.Task.Result;

            lock (SyncRoot)
            {
                _context = null;
                _breaker = null;
            }

            var message = MondValue.Object();
            message["type"] = "state";
            message["isRunning"] = true;

            Broadcast(message);

            return result;
        }

        internal void GetState(out bool isRunning, out BreakPosition position)
        {
            lock (SyncRoot)
            {
                isRunning = _breaker == null;
                position = _position;
            }
        }

        internal void PerformAction(MondDebugAction action)
        {
            TaskCompletionSource<MondDebugAction> breaker;

            lock (SyncRoot)
                breaker = _breaker;

            breaker?.SetResult(action);
        }

        internal List<MondDebugInfo.Statement> SetBreakpoints(int programId, IEnumerable<(int Line, int? Column)> breakpoints)
        {
            lock (SyncRoot)
            {
                if (programId < 0 || programId >= Programs.Count)
                    return null;

                var program = Programs[programId];
                ClearBreakpoints(program);

                var statementsQuery =
                    from bp in breakpoints
                    from s in program.DebugInfo.Statements
                    where s.StartLineNumber == bp.Line && (!bp.Column.HasValue || bp.Column.Value == s.StartColumnNumber)
                    select s;

                var statements = statementsQuery.ToList();
                
                foreach (var statement in statements)
                {
                    AddBreakpoint(program, statement.Address);
                }

                return statements;
            }
        }

        internal List<MondDebugInfo.Statement> GetBreakpointLocations(int programId, int startLine, int startColumn, int endLine, int endColumn)
        {
            lock (SyncRoot)
            {
                if (programId < 0 || programId >= Programs.Count)
                    return null;
                    
                var program = Programs[programId];
                return program.DebugInfo.FindStatements(startLine, startColumn, endLine, endColumn).ToList();
            }
        }

        internal void AddWatch(string expression)
        {
            Watch watch;

            lock (SyncRoot)
            {
                watch = new Watch(_watches.Count, expression);
                _watches.Add(watch);
            }

            RefreshWatch(_context, watch);

            var message = MondValue.Object();
            message["Type"] = "AddedWatch";
            message["Id"] = watch.Id;
            message["Expression"] = watch.Expression;
            message["Value"] = watch.Value;

            Broadcast(message);
        }

        internal void RemoveWatch(int id)
        {
            int removed;

            lock (SyncRoot)
                removed = _watches.RemoveAll(w => w.Id == id);

            if (removed == 0)
                return;

            var message = MondValue.Object();
            message["Type"] = "RemovedWatch";
            message["Id"] = id;

            Broadcast(message);
        }

        private void RefreshWatch(MondDebugContext context, Watch watch)
        {
            _watchSemaphore.Wait();

            try
            {
                var timer = new Timer(state =>
                {
                    _watchTimedOut = true;
                    IsBreakRequested = true;
                }, null, -1, -1);

                _watchTimedOut = false;
                timer.Change(500, -1);

                watch.Refresh(context);

                timer.Change(-1, -1);
                _watchTimedOut = false;
            }
            finally
            {
                _watchSemaphore.Release();
            }
        }

        private void UpdateState(MondDebugContext context, int address)
        {
            var program = context.Program;
            var debugInfo = program.DebugInfo;

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
                    statement = new MondDebugInfo.Statement(0, -1, -1, -1, -1);
                }
            }

            // refresh all watches
            List<Watch> watches;

            lock (SyncRoot)
                watches = _watches.ToList();

            foreach (var watch in watches)
            {
                RefreshWatch(_context, watch);
            }

            // apply new state and broadcast it
            MondValue message;
            lock (SyncRoot)
            {
                var stmtValue = statement.Value;
                var programId = FindProgramIndex(program);

                var stoppedOnBreakpoint = ProgramBreakpoints.TryGetValue(program, out var breakpoints) &&
                                          breakpoints.Contains(address);

                _position = new BreakPosition(
                    programId,
                    program.DebugInfo?.FileName,
                    stmtValue.StartLineNumber,
                    stmtValue.StartColumnNumber,
                    stmtValue.EndLineNumber,
                    stmtValue.EndColumnNumber);

                message = MondValue.Object();
                message["type"] = "state";
                message["isRunning"] = false;
                message["stoppedOnBreakpoint"] = stoppedOnBreakpoint;
            }

            Broadcast(message);
        }

        internal MondValue GetStackFramesArray()
        {
            var entries = _context.CallStack;
            var objs = new List<MondValue>(entries.Count);

            foreach (var entry in entries)
            {
                var programId = FindProgramIndex(entry.Program);
                objs.Add(Utility.JsonCallStackEntry(programId, entry));
            }

            return MondValue.Array(objs);
        }

        internal int FindProgramIndex(MondProgram program)
        {
            lock (SyncRoot)
            {
                return Programs.IndexOf(program);
            }
        }

        internal int FindProgramIndex(string path)
        {
            lock (SyncRoot)
            {
                return Programs.FindIndex(p => p.DebugInfo?.FileName?.EndsWith(path) ?? false);
            }
        }

        private void Broadcast(MondValue obj)
        {
            var data = Json.Serialize(obj);
            _server.Broadcast(data);
        }

        private static bool IsMissingDebugInfo(MondDebugInfo debugInfo)
        {
            return
                debugInfo == null ||
                debugInfo.SourceCode == null ||
                debugInfo.Functions == null ||
                debugInfo.Lines == null ||
                debugInfo.Statements == null ||
                debugInfo.Scopes == null;
        }
    }
}
