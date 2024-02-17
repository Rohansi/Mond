using System;
using Fleck;

namespace Mond.RemoteDebugger
{
    internal class Session
    {
        private readonly MondRemoteDebugger _debugger;
        private readonly IWebSocketConnection _socket;

        public Session(MondRemoteDebugger debugger, IWebSocketConnection socket)
        {
            _debugger = debugger;
            _socket = socket;
        }

        public void Send(string data) => _socket.Send(data);

        public void Close() => _socket.Close();

        public void OnOpen()
        {
            var initialState = _debugger.GetInitialState();
            Send(initialState);
        }

        public void OnMessage(string data)
        {
            try
            {
                var response = _debugger.HandleRequest(data);
                Send(response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
