using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Fleck;

namespace Mond.RemoteDebugger
{
    internal class Server : IDisposable
    {
        private readonly WebSocketServer _server;
        private readonly List<Session> _sessions;

        public Server(MondRemoteDebugger debugger, IPEndPoint endpoint)
        {
            if (endpoint.AddressFamily != AddressFamily.InterNetwork &&
                endpoint.AddressFamily != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("Endpoint must be an IPv4 or IPv6 address.", nameof(endpoint));
            }

            _server = new WebSocketServer($"ws://{endpoint}")
            {
                RestartAfterListenError = true,
                ListenerSocket = { NoDelay = true },
            };
            _sessions = new List<Session>();

            _server.Start(socket =>
            {
                var session = new Session(debugger, this, socket);

                socket.OnOpen = () =>
                {
                    lock (_sessions)
                    {
                        _sessions.Add(session);
                    }

                    session.OnOpen();
                };

                socket.OnClose = () =>
                {
                    lock (_sessions)
                    {
                        _sessions.Remove(session);
                    }
                };

                socket.OnMessage = session.OnMessage;
            });
        }

        public void Dispose()
        {
            _server.Dispose();

            lock (_sessions)
            {
                foreach (var session in _sessions)
                {
                    session.Close();
                }

                _sessions.Clear();
            }
        }

        public void Broadcast(string data)
        {
            lock (_sessions)
            {
                foreach (var session in _sessions)
                {
                    try
                    {
                        session.Send(data);
                    }
                    catch
                    {
                        session.Close();
                    }
                }
            }
        }
    }
}
