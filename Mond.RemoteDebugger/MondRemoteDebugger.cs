using System;
using System.Net;
using Mond.Debugger;

namespace Mond.RemoteDebugger
{
    /// <summary>
    /// Debugger using the Mond debug protocol over WebSocket. Supports debugging locally or remotely.
    /// </summary>
    public class MondRemoteDebugger : MondProtocolDebugger, IDisposable
    {
        private readonly Server _server;

        /// <summary>
        /// Starts listening for local connections on the given port.
        /// </summary>
        /// <param name="port">The port to accept connections to the debugger on.</param>
        public MondRemoteDebugger(int port)
            : this(new IPEndPoint(IPAddress.Loopback, port))
        {
        }

        /// <summary>
        /// Starts listening for connections on the given endpoint. Must be used to enable remote debugging.
        /// </summary>
        /// <param name="endpoint">The endpoint to accept connections to the debugger on.</param>
        public MondRemoteDebugger(IPEndPoint endpoint)
        {
            _server = new Server(this, endpoint);
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        protected override void Send(string json)
        {
            _server.Broadcast(json);
        }
    }
}
