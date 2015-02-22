using System;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    internal class Watch
    {
        private string _value;

        public readonly int Id;
        public readonly string Expression;

        public string Value
        {
            get { return _value ?? ""; }
        }

        public Watch(int id, string expression)
        {
            Id = id;
            Expression = expression;
        }

        public void Refresh(MondDebugContext context)
        {
            if (context == null)
            {
                _value = null;
                return;
            }

            MondValue value;
            if (!context.TryGetLocal(Expression, out value))
            {
                _value = string.Format("`{0}` doesn't exist in the current context", Expression);
                return;
            }

            try
            {
                _value = value.Serialize();
            }
            catch (Exception e)
            {
                _value = e.Message;
            }
        }
    }
}
