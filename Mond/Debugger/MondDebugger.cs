using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mond.Debugger
{
    public abstract class MondDebugger
    {
        private readonly object _sync;
        private bool _attached;
        private Dictionary<MondProgram, List<int>> _programBreakpoints;

        protected MondDebugger()
        {
            _sync = new object();
            _attached = false;
            _programBreakpoints = new Dictionary<MondProgram, List<int>>();
        }

        /// <summary>
        /// Set to true when the VM should break on the next statement.
        /// </summary>
        protected bool IsBreakRequested { get; set; }

        /// <summary>
        /// Returns a list of the current breakpoints.
        /// </summary>
        protected IReadOnlyDictionary<MondProgram, IReadOnlyList<int>> Breakpoints
        {
            get
            {
                lock (_sync)
                {
                    var copy = _programBreakpoints
                        .ToDictionary(kv => kv.Key, kv => (IReadOnlyList<int>)kv.Value);

                    return new ReadOnlyDictionary<MondProgram, IReadOnlyList<int>>(copy);
                }
            }
        }

        /// <summary>
        /// Called when the debugger is attached to a state.
        /// </summary>
        protected virtual void OnAttached() { }

        /// <summary>
        /// Called when the debugger is detached from a state.
        /// </summary>
        protected virtual void OnDetached() { }

        /// <summary>
        /// Called when the VM breaks. This should block until an action should be taken.
        /// </summary>
        /// <returns>The action the VM should continue with.</returns>
        protected abstract MondDebugAction OnBreak(MondProgram program, int address, MondDebugInfo debugInfo /* TODO */);

        /// <summary>
        /// Adds a breakpoint to the given program.
        /// </summary>
        /// <param name="program"></param>
        /// <param name="address"></param>
        protected void AddBreakpoint(MondProgram program, int address)
        {
            lock (_sync)
            {
                List<int> breakpoints;
                if (!_programBreakpoints.TryGetValue(program, out breakpoints))
                {
                    breakpoints = new List<int>();
                    _programBreakpoints.Add(program, breakpoints);
                }

                if (breakpoints.Contains(address))
                    return;

                breakpoints.Add(address);
            }
        }

        /// <summary>
        /// Removes a breakpoint from the given program.
        /// </summary>
        /// <param name="program"></param>
        /// <param name="address"></param>
        protected void RemoveBreakpoint(MondProgram program, int address)
        {
            lock (_sync)
            {
                List<int> breakpoints;
                if (!_programBreakpoints.TryGetValue(program, out breakpoints))
                    return;

                breakpoints.Remove(address);
            }
        }

        // -------------------------------

        internal MondDebugAction Break(MondProgram program, int address, MondDebugInfo debugInfo /* TODO */)
        {
            return OnBreak(program, address, debugInfo);
        }

        internal bool ShouldBreak(MondProgram program, int address)
        {
            lock (_sync)
            {
                if (IsBreakRequested)
                {
                    IsBreakRequested = false;
                    return true;
                }

                List<int> breakpoints;
                if (!_programBreakpoints.TryGetValue(program, out breakpoints))
                    return false;

                return breakpoints.Contains(address);
            }
        }

        internal bool TryAttach()
        {
            lock (_sync)
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

            lock (_sync)
            {
                detached = _attached;
                _attached = false;
            }

            if (detached)
                OnDetached();
        }
    }
}
