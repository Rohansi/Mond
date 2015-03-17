using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using Mond.Debugger;
using WebSocketSharp.Server;

#if !UNITY
using System.Threading.Tasks;
#endif

namespace Mond.RemoteDebugger
{
    public class MondRemoteDebugger : MondDebugger, IDisposable
    {
        private WebSocketServer _server;

        private readonly object _sync = new object();
        private HashSet<MondProgram> _seenPrograms;
        private List<ProgramInfo> _programs;
        private List<Watch> _watches;
        private SemaphoreSlim _watchSemaphore;
        private bool _watchTimedOut;

        private MondDebugContext _context;
        private TaskCompletionSource<MondDebugAction> _breaker;
        private BreakPosition _position;

        public MondRemoteDebugger(IPEndPoint endPoint)
        {
            _server = new WebSocketServer(endPoint.Address, endPoint.Port);
            _server.KeepClean = true;

            _server.AddWebSocketService("/", () => new Session(this));

            _seenPrograms = new HashSet<MondProgram>();
            _programs = new List<ProgramInfo>();
            _watches = new List<Watch>();

            _server.Start();
        }

        public MondRemoteDebugger(IPAddress address, int port)
            : this(new IPEndPoint(address, port))
        {
            
        }

        public void RequestBreak()
        {
            IsBreakRequested = true;
        }

        public void Dispose()
        {
            _server.Stop();
            _server = null;
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

            lock (_sync)
            {
                _seenPrograms.Clear();
                _programs.Clear();

                breaker = _breaker;
            }

            if (breaker != null)
                breaker.SetResult(MondDebugAction.Run);
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
            if (IsMissingDebugInfo(context.DebugInfo))
                return MondDebugAction.StepOut;

            lock (_sync)
            {
                if (_breaker != null)
                    throw new InvalidOperationException("Debugger hit breakpoint while waiting on another");

                _context = context;
                _breaker = new TaskCompletionSource<MondDebugAction>();
            }

            // keep track of program instances
            VisitProgram(context);

            // update the current state
            UpdateState(context, address);

            // block until an action is set
            return WaitForAction();
        }

        private MondDebugAction WaitForAction()
        {
            var result = _breaker.Task.Result;

            lock (_sync)
            {
                _context = null;
                _breaker = null;
            }

            var message = new MondValue(MondValueType.Object);
            message["Type"] = "State";
            message["Running"] = true;

            Broadcast(message);

            return result;
        }

        internal void GetState(
            out bool isRunning,
            out List<ProgramInfo> programs,
            out BreakPosition position,
            out List<Watch> watches,
            out ReadOnlyCollection<MondDebugContext.CallStackEntry> callStack)
        {
            lock (_sync)
            {
                isRunning = _breaker == null;
                programs = _programs.ToList();
                position = _position;
                watches = _watches.ToList();
                callStack = _context != null ? _context.CallStack : null;
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
                watch = new Watch(_watches.Count, expression);
                _watches.Add(watch);
            }

            RefreshWatch(_context, watch);

            var message = new MondValue(MondValueType.Object);
            message["Type"] = "AddedWatch";
            message["Id"] = watch.Id;
            message["Expression"] = watch.Expression;
            message["Value"] = watch.Value;

            Broadcast(message);
        }

        internal void RemoveWatch(int id)
        {
            int removed;

            lock (_sync)
                removed = _watches.RemoveAll(w => w.Id == id);

            if (removed == 0)
                return;

            var message = new MondValue(MondValueType.Object);
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
                });

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
            var debugInfo = context.DebugInfo;

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

            lock (_sync)
                watches = _watches.ToList();

            foreach (var watch in watches)
            {
                RefreshWatch(_context, watch);
            }

            // apply new state and broadcast it
            MondValue message;
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

                message = new MondValue(MondValueType.Object);
                message["Type"] = "State";
                message["Running"] = false;
                message["Id"] = _position.Id;
                message["StartLine"] = _position.StartLine;
                message["StartColumn"] = _position.StartColumn;
                message["EndLine"] = _position.EndLine;
                message["EndColumn"] = _position.EndColumn;
                message["Watches"] = new MondValue(watches.Select(Utility.JsonWatch));
                message["CallStack"] = new MondValue(_context.CallStack.Select(Utility.JsonCallStackEntry));
            }

            Broadcast(message);
        }

        private void VisitProgram(MondDebugContext context)
        {
            var program = context.Program;
            var debugInfo = context.DebugInfo;

            int id;
            ProgramInfo programInfo;

            lock (_sync)
            {
                if (_seenPrograms.Contains(program))
                    return;

                _seenPrograms.Add(program);

                id = _programs.Count;
                programInfo = new ProgramInfo(program, debugInfo);

                _programs.Add(programInfo);
            }

            var message = new MondValue(MondValueType.Object);
            message["Type"] = "MondProgram";
            message["Id"] = id;
            message["FileName"] = programInfo.FileName;
            message["SourceCode"] = debugInfo.SourceCode;
            message["FirstLine"] = Utility.FirstLineNumber(debugInfo);
            message["Breakpoints"] = new MondValue(programInfo.Breakpoints.Select(e => new MondValue(e)));

            Broadcast(message);
        }

        private void Broadcast(MondValue obj)
        {
            var data = Json.Serialize(obj);
            _server.WebSocketServices["/"].Sessions.Broadcast(data);
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
