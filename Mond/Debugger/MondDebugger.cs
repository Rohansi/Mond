using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Debugger
{
    public abstract class MondDebugger
    {
        protected readonly object SyncRoot = new();
        protected readonly List<MondProgram> Programs;
        protected readonly Dictionary<MondProgram, List<int>> ProgramBreakpoints;

        private bool _attached;

        protected MondDebugger()
        {
            Programs = new List<MondProgram>();
            ProgramBreakpoints = new Dictionary<MondProgram, List<int>>();

            _attached = false;
        }

        /// <summary>
        /// Set to true when the VM should break on the next statement.
        /// </summary>
        protected bool IsBreakRequested { get; set; }

        /// <summary>
        /// Called when the debugger is attached to a state.
        /// </summary>
        protected virtual void OnAttached() { }

        /// <summary>
        /// Called when the debugger is detached from a state.
        /// </summary>
        protected virtual void OnDetached() { }

        protected virtual void OnProgramAdded(MondProgram program) { }

        /// <summary>
        /// Called when the VM breaks. This should block until an action should be taken.
        /// </summary>
        /// <returns>The action the VM should continue with.</returns>
        protected abstract MondDebugAction OnBreak(MondDebugContext context, int address /* TODO */);

        /// <summary>
        /// Adds a breakpoint to the given program.
        /// </summary>
        protected void AddBreakpoint(MondProgram program, int address)
        {
            lock (SyncRoot)
            {
                if (!ProgramBreakpoints.TryGetValue(program, out var breakpoints))
                {
                    breakpoints = new List<int>();
                    ProgramBreakpoints.Add(program, breakpoints);
                    Programs.Add(program);
                }

                // snap breakpoints to the statement's address if it isn't already
                // the VM will only check checkpoint instructions which align with statement addresses
                var statement = program.DebugInfo?.FindStatement(address);
                if (statement != null)
                    address = statement.Value.Address;

                if (breakpoints.Contains(address))
                    return;

                breakpoints.Add(address);
            }
        }

        /// <summary>
        /// Removes a breakpoint from the given program.
        /// </summary>
        protected void RemoveBreakpoint(MondProgram program, int address)
        {
            lock (SyncRoot)
            {
                if (!ProgramBreakpoints.TryGetValue(program, out var breakpoints))
                    return;

                breakpoints.Remove(address);
            }
        }

        /// <summary>
        /// Removes all breakpoints from the given program.
        /// </summary>
        protected void ClearBreakpoints(MondProgram program)
        {
            lock (SyncRoot)
            {
                if (ProgramBreakpoints.TryGetValue(program, out var breakpoints))
                    breakpoints.Clear();
            }
        }

        // -------------------------------

        internal MondDebugAction Break(MondDebugContext context, int address /* TODO */)
        {
            return OnBreak(context, address);
        }

        protected internal virtual bool ShouldBreak(MondProgram program, int address)
        {
            lock (SyncRoot)
            {
                if (IsBreakRequested)
                {
                    IsBreakRequested = false;
                    return true;
                }

                var breakpointsFound = ProgramBreakpoints.TryGetValue(program, out var breakpoints);
                if (!breakpointsFound)
                {
                    ProgramBreakpoints.Add(program, new List<int>());
                    Programs.Add(program);
                    OnProgramAdded(program);
                }

                return breakpointsFound && breakpoints.Contains(address);
            }
        }

        internal bool TryAttach()
        {
            lock (SyncRoot)
            {
                if (_attached)
                    return false;

                _attached = true;
            }

            OnAttached();
            return true;
        }

        internal void Detach()
        {
            bool detached;

            lock (SyncRoot)
            {
                detached = _attached;
                _attached = false;
                
                ProgramBreakpoints.Clear();
            }

            if (detached)
                OnDetached();
        }
    }
}
