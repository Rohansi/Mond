using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using Mond.Libraries.Json;

namespace Mond.Debugger;

/// <summary>
/// Abstract class for developing debuggers based on Mond's JSON-based debug protocol.
/// </summary>
public abstract class MondProtocolDebugger : MondDebugger
{
    public const int ProtocolVersion = 2;

    private SemaphoreSlim _evalSemaphore;
    private bool _evalTimedOut;

    private MondDebugContext _context;
    private TaskCompletionSource<MondDebugAction> _breaker;
    private BreakPosition _position;

    /// <summary>
    /// Send debug state notification messages to the debugger client.
    /// </summary>
    /// <param name="json">The message JSON which needs to be sent.</param>
    protected abstract void Send(string json);

    /// <summary>
    /// Gets the initial debugger state to send to the debugger client when attaching.
    /// </summary>
    /// <returns>Initial state JSON to be sent to the debugger client.</returns>
    public string GetInitialState()
    {
        GetState(out var isRunning, out _);

        var message = MondValue.Object();
        message["type"] = "initialState";
        message["version"] = ProtocolVersion;
        message["isRunning"] = isRunning;

        return JsonModule.Serialize(message);
    }

    /// <summary>
    /// Handles a request sent from the debugger client.
    /// </summary>
    /// <param name="requestJson">The request JSON sent from the debugger client.</param>
    /// <returns>Response JSON to be sent back to the debugger client.</returns>
    public string HandleRequest(string requestJson)
    {
        MondValue obj;
        try
        {
            obj = JsonModule.Deserialize(requestJson);
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Failed to deserialize request JSON: {requestJson}", nameof(requestJson), e);
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
                            RequestBreak();
                            return ReplyWithOk();
                        }

                        PerformAction(ParseAction(value));
                        return ReplyWithOk();
                    }

                case "stackTrace":
                    {
                        var stackFrames = GetStackFramesArray();
                        var response = MondValue.Object();
                        response["stackFrames"] = stackFrames;
                        return ReplyWithOk(response);
                    }

                case "setBreakpoints":
                    {
                        var programId = GetProgramId();
                        var breakpoints = obj["breakpoints"].AsList
                            .Select(o => (Line: (int)o["line"], Column: Utility.GetInt(o, "column")))
                            .ToList();

                        var breakpointStatements = SetBreakpoints(programId, breakpoints);

                        var response = MondValue.Object();
                        response["programId"] = programId;
                        response["breakpoints"] = MondValue.Array(breakpointStatements.Select(Utility.JsonBreakpoint));
                        return ReplyWithOk(response);
                    }

                case "getBreakpointLocations":
                    {
                        var programId = GetProgramId();
                        var startLine = (int)obj["line"];
                        var startColumn = Utility.GetInt(obj, "column") ?? int.MinValue;
                        var endLine = Utility.GetInt(obj, "endLine") ?? startLine;
                        var endColumn = Utility.GetInt(obj, "endColumn") ?? int.MaxValue;

                        var breakpointLocations = GetBreakpointLocations(programId, startLine, startColumn, endLine, endColumn);

                        var response = MondValue.Object();
                        response["programId"] = programId;
                        response["locations"] = MondValue.Array(breakpointLocations.Select(Utility.JsonBreakpoint));
                        return ReplyWithOk(response);
                    }

                case "eval":
                    {
                        var expression = (string)obj["expression"];
                        var value = string.IsNullOrWhiteSpace(expression)
                            ? GetLocals()
                            : Evaluate(expression);

                        var response = MondValue.Object();
                        response["value"] = value.ToString();
                        response["type"] = value.Type.GetName();
                        response["properties"] = Utility.JsonValueProperties(value);
                        return ReplyWithOk(response);
                    }

                default:
                    {
                        var response = MondValue.Object();
                        response["status"] = "error";
                        response["error"] = $"unhandled type {obj["type"]}";
                        return ReplyWith(response);
                    }
            }

            int GetProgramId()
            {
                var programId = obj["programId"];
                if (programId.Type == MondValueType.Number)
                    return (int)programId;

                var programPath = obj["programPath"];
                if (programPath.Type == MondValueType.String)
                    return FindProgramIndex(programPath);

                throw new InvalidOperationException("Both Program ID and Program Path were unspecified");
            }
        }
        catch (Exception e)
        {
            HandleRequestError(e);

            var response = MondValue.Object();
            response["status"] = "error";
            response["error"] = e.Message;
            return ReplyWith(response);
        }

        string ReplyWithOk(MondValue responseObj = default)
        {
            if (responseObj.Type != MondValueType.Object)
                responseObj = MondValue.Object();

            responseObj["status"] = "ok";
            return ReplyWith(responseObj);
        }

        string ReplyWith(MondValue responseObj)
        {
            responseObj["seq"] = obj["seq"];
            return JsonModule.Serialize(responseObj);
        }
    }

    /// <summary>
    /// Called when a request from the debugger client threw an exception.
    /// </summary>
    /// <param name="exception">The exception that was caught.</param>
    protected virtual void HandleRequestError(Exception exception)
    {
    }

    /// <summary>
    /// Requests for the runtime to pause execution as soon as possible.
    /// </summary>
    public void RequestBreak()
    {
        IsBreakRequested = true;
    }

    protected override void OnAttached()
    {
        _evalSemaphore = new SemaphoreSlim(1);
        _evalTimedOut = false;
        _breaker = null;
    }

    protected override void OnDetached()
    {
        _breaker?.SetResult(MondDebugAction.Run);
    }

    protected override MondDebugAction OnBreak(MondDebugContext context, int address)
    {
        if (_evalTimedOut)
        {
            _evalTimedOut = false;
            throw new MondRuntimeException("Execution timed out");
        }

        if (_evalSemaphore.CurrentCount == 0)
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

    private void GetState(out bool isRunning, out BreakPosition position)
    {
        lock (SyncRoot)
        {
            isRunning = _breaker == null;
            position = _position;
        }
    }

    private void PerformAction(MondDebugAction action)
    {
        _breaker?.SetResult(action);
    }

    /// <summary>
    /// Resolves each requested breakpoint to the statement it will actually break on. The returned list
    /// always has one entry per request, in the same order, and contains null where nothing could be bound.
    /// </summary>
    private List<MondDebugInfo.Statement?> SetBreakpoints(int programId, List<(int Line, int? Column)> breakpoints)
    {
        lock (SyncRoot)
        {
            var resolved = new List<MondDebugInfo.Statement?>(breakpoints.Count);

            if (programId < 0 || programId >= Programs.Count)
            {
                for (var i = 0; i < breakpoints.Count; i++)
                {
                    resolved.Add(null);
                }

                return resolved;
            }

            var program = Programs[programId];
            var statements = program.DebugInfo?.Statements;
            ClearBreakpoints(program);

            var candidates = breakpoints
                .Select(bp => statements == null ? null : FindBreakpointStatement(statements, bp.Line, bp.Column))
                .ToList();

            // several requests can land on the same statement, which would otherwise leave the client
            // showing a pile of breakpoints on one line - only the first one to claim a statement binds
            var claimed = new HashSet<int>();

            foreach (var candidate in candidates)
            {
                if (candidate != null && claimed.Add(candidate.Value.Address))
                {
                    AddBreakpoint(program, candidate.Value.Address);
                    resolved.Add(candidate);
                }
                else
                {
                    resolved.Add(null);
                }
            }

            return resolved;
        }
    }

    private static MondDebugInfo.Statement? FindBreakpointStatement(IEnumerable<MondDebugInfo.Statement> statements, int line, int? column)
    {
        // the VM only breaks on checkpoint instructions, which line up with statement addresses, so a
        // breakpoint only binds if the requested line actually starts a statement - it never moves to
        // another line, otherwise blank lines would stack their breakpoints onto the next line of code
        var candidates = column == null
            ? statements.Where(s => s.StartLineNumber == line)
            : statements.Where(s => s.StartLineNumber == line && s.StartColumnNumber == column);

        return candidates
            .OrderBy(s => s.StartColumnNumber)
            .Select(s => (MondDebugInfo.Statement?)s)
            .FirstOrDefault();
    }

    private List<MondDebugInfo.Statement> GetBreakpointLocations(int programId, int startLine, int startColumn, int endLine, int endColumn)
    {
        lock (SyncRoot)
        {
            if (programId < 0 || programId >= Programs.Count)
                return new List<MondDebugInfo.Statement>();

            var program = Programs[programId];
            return program.DebugInfo.FindStatements(startLine, startColumn, endLine, endColumn).ToList();
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

    private MondValue GetStackFramesArray()
    {
        if (_context == null)
            return MondValue.Array();

        var entries = _context.CallStack;
        var objs = new List<MondValue>(entries.Count);

        foreach (var entry in entries)
        {
            var programId = FindProgramIndex(entry.Program);
            objs.Add(Utility.JsonCallStackEntry(programId, entry));
        }

        return MondValue.Array(objs);
    }

    private MondValue GetLocals()
    {
        return _context?.GetLocals() ?? MondValue.Object();
    }

    private MondValue Evaluate(string expression)
    {
        if (_context == null)
            return MondValue.Undefined;

        try
        {
            _evalSemaphore.Wait();

            Timer timer = null;
            try
            {
                timer = new Timer(_ =>
                {
                    _evalTimedOut = true;
                    IsBreakRequested = true;
                }, null, -1, -1);

                _evalTimedOut = false;
                timer.Change(500, -1);

                return _context.Evaluate(expression);

            }
            finally
            {
                timer?.Change(-1, -1);
                timer?.Dispose();
                _evalTimedOut = false;

                _evalSemaphore.Release();
            }

        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    private int FindProgramIndex(MondProgram program)
    {
        lock (SyncRoot)
        {
            return Programs.IndexOf(program);
        }
    }

    private int FindProgramIndex(string path)
    {
        lock (SyncRoot)
        {
            return Programs.FindIndex(p => p.DebugInfo?.FileName?.EndsWith(path, StringComparison.InvariantCultureIgnoreCase) ?? false);
        }
    }

    private void Broadcast(MondValue obj)
    {
        var data = JsonModule.Serialize(obj);
        Send(data);
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

    private static class Utility
    {
        public static int? GetInt(MondValue obj, string fieldName)
        {
            var field = obj[fieldName];
            return field.Type == MondValueType.Number
                ? (int)field
                : null;
        }

        public static MondValue JsonBreakpoint(MondDebugInfo.Statement? statement)
        {
            return statement == null ? MondValue.Null : JsonBreakpoint(statement.Value);
        }

        public static MondValue JsonBreakpoint(MondDebugInfo.Statement statement)
        {
            var obj = MondValue.Object();
            obj["address"] = statement.Address;
            obj["line"] = statement.StartLineNumber;
            obj["column"] = statement.StartColumnNumber != int.MinValue ? statement.StartColumnNumber : MondValue.Undefined;
            obj["endLine"] = statement.EndLineNumber != int.MinValue ? statement.EndLineNumber : MondValue.Undefined;
            obj["endColumn"] = statement.EndColumnNumber != int.MinValue ? statement.EndColumnNumber : MondValue.Undefined;
            return obj;
        }

        public static MondValue JsonCallStackEntry(int programId, MondDebugContext.CallStackEntry callStackEntry)
        {
            var obj = MondValue.Object();
            obj["programId"] = programId;
            obj["address"] = callStackEntry.Address;
            obj["fileName"] = callStackEntry.FileName;
            obj["function"] = callStackEntry.Function;
            obj["line"] = callStackEntry.StartLineNumber;
            obj["column"] = callStackEntry.StartColumnNumber;
            obj["endLine"] = callStackEntry.EndLineNumber ?? MondValue.Undefined;
            obj["endColumn"] = callStackEntry.EndColumnNumber ?? MondValue.Undefined;
            return obj;
        }

        public static MondValue JsonValueProperties(MondValue value)
        {
            switch (value.Type)
            {
                case MondValueType.Object:
                    var objProperties = value.AsDictionary
                        .Where(kvp => IsPrimitive(kvp.Key))
                        .Select(kvp => JsonValueProperty(kvp.Key, kvp.Value));
                    return MondValue.Array(objProperties);

                case MondValueType.Array:
                    var arrayProperties = value.AsList
                        .Select((v, i) => JsonValueProperty(i, v));
                    return MondValue.Array(arrayProperties);

                default:
                    return MondValue.Array();
            }
        }

        private static MondValue JsonValueProperty(MondValue key, MondValue value)
        {
            var property = MondValue.Object();
            property["name"] = key.ToString();
            property["nameType"] = key.Type.GetName();
            property["value"] = value.ToString();
            property["valueType"] = value.Type.GetName();
            return property;
        }

        private static bool IsPrimitive(in MondValue value)
        {
            return value.Type != MondValueType.Object &&
                   value.Type != MondValueType.Array &&
                   value.Type != MondValueType.Function;
        }
    }
}
