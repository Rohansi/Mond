using System;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    internal class Watch
    {
        private string _value;

        public int Id { get; }
        public string Expression { get; }

        public string Value => _value ?? "";

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

            try
            {
                var result = context.Evaluate(Expression);
                _value = result.Serialize();
            }
            catch (Exception e)
            {
                _value = e.Message;
            }
        }
    }
}
