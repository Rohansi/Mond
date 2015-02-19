using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Mond.Debugger;
using WebSocketSharp.Server;

namespace Mond.RemoteDebugger
{
    public class MondRemoteDebugger : MondDebugger, IDisposable
    {
        private WebSocketServer _server;
        private HashSet<MondProgram> _seenPrograms;

        internal TaskCompletionSource<MondDebugAction> Break;
        internal List<Tuple<MondProgram, MondDebugInfo>> Programs;

        public MondRemoteDebugger(IPEndPoint endPoint)
        {
            _server = new WebSocketServer(endPoint.Address, endPoint.Port);
            _server.KeepClean = true;

            _server.AddWebSocketService("/", () => new Session(this));

            _server.Start();
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
            Programs = new List<Tuple<MondProgram, MondDebugInfo>>();
        }

        protected override void OnDetached()
        {
            _seenPrograms = null;

            if (Break != null)
                Break.SetResult(MondDebugAction.Run);
        }

        protected override MondDebugAction OnBreak(MondProgram program, MondDebugInfo debugInfo, int address)
        {
            // if missing debug info, leave the function
            if (IsMissingDebugInfo(debugInfo))
                return MondDebugAction.StepOut;

            // check if we stopped in a new program
            if (!_seenPrograms.Contains(program))
            {
                var id = Programs.Count;
                Programs.Add(Tuple.Create(program, debugInfo));

                // TODO: broadcast new program info
            }

            // block until an action is set
            Break = new TaskCompletionSource<MondDebugAction>();
            var result = Break.Task.Result;
            Break = null;

            return result;
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
