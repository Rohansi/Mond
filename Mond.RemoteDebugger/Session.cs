using System;
using System.Linq;
using Newtonsoft.Json;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Mond.RemoteDebugger
{
    internal class Session : WebSocketBehavior
    {
        private readonly MondRemoteDebugger _debugger;

        public Session(MondRemoteDebugger debugger)
        {
            _debugger = debugger;
        }

        protected override void OnOpen()
        {
            Console.WriteLine("got connection");
            
            Send(JsonConvert.SerializeObject(new
            {
                Type = "InitialState",
                Programs = _debugger.Programs.Select(t => new
                {
                    t.Item2.FileName,
                    t.Item2.SourceCode
                })
            }));
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            // TODO: handle
        }
    }
}
