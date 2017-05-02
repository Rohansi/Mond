using System;
using System.Reflection;
using IotWeb.Server;
using IotWeb.Common.Http;
using System.Collections.Generic;

namespace Mond.RemoteDebugger
{
    class Server : IDisposable
    {
        private HttpServer _server;
        private SessionManager _sessionMgr;

        public Server(MondRemoteDebugger debugger, int port)
        {
            _server = new HttpServer(port);

            var assembly = GetType().GetTypeInfo().Assembly;
            _server.AddHttpRequestHandler("/", 
                new HttpResourceHandler(assembly, "DebuggerClient", "index.html"));

            _sessionMgr = new SessionManager(debugger);
            _server.AddWebSocketRequestHandler("/", _sessionMgr);

            _server.Start();
        }

        public void Dispose()
        {
            _server.Stop();
            _server = null;
        }

        public void Broadcast(string data)
        {
           _sessionMgr.Broadcast(data);
        }
    }

    class SessionManager : IWebSocketRequestHandler
    {
        private readonly object _sync = new object();
        private readonly List<Session> _clients = new List<Session>();
        private readonly MondRemoteDebugger _debugger;

        public SessionManager(MondRemoteDebugger debugger)
        {
            _debugger = debugger;
        }

        public bool WillAcceptRequest(string uri, string protocol) => true;

        public void Connected(WebSocket socket)
        {
            var session = new Session(this, _debugger);
            session.OnOpen(socket);

            lock (_sync)
            {
                _clients.Add(session);
            }
            
            socket.ConnectionClosed += sender =>
            {
                lock (_sync)
                {
                    _clients.Remove(session);
                }
            };
        }

        public void Broadcast(string data)
        {
            foreach (var session in _clients)
            {
                try
                {
                    session.Send(data);
                }
                catch { }
            }
        }
    }
}
