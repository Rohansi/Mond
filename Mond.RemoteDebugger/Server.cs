using System;
using System.IO;
using System.Reflection;
using WebSocketSharp.Net;
using WebSocketSharp.Server;

namespace Mond.RemoteDebugger
{
    class Server : IDisposable
    {
        private HttpServer _server;

        public Server(MondRemoteDebugger debugger, int port)
        {
            _server = new HttpServer(port);
            _server.KeepClean = true;

            var assembly = Assembly.GetExecutingAssembly();
            var rootNamespace = GetType().Namespace + ".DebuggerClient";

            _server.OnGet += (sender, args) =>
            {
                var req = args.Request;
                var res = args.Response;

                var path = req.RawUrl;
                if (path == "/")
                    path = "/index.htm";

                Stream stream;

                try
                {
                    stream = assembly.GetManifestResourceStream(rootNamespace + path.Replace('/', '.'));
                }
                catch
                {
                    res.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                if (stream == null)
                {
                    res.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                res.StatusCode = (int)HttpStatusCode.OK;
                res.ContentType = GetMimeType(path);

                int bytesRead;
                var buffer = new byte[4096];
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    res.OutputStream.Write(buffer, 0, bytesRead);
                }
            };

            _server.AddWebSocketService("/", () => new Session(debugger));

            _server.Start();
        }

        public void Dispose()
        {
            _server.Stop();
            _server = null;
        }

        public void Broadcast(string data)
        {
            _server.WebSocketServices["/"].Sessions.Broadcast(data);
        }

        private static string GetMimeType(string path)
        {
            var ext = Path.GetExtension(path);

            if (ext == null)
                return "application/octet-stream";

            switch (ext.ToLower())
            {
                case ".png":
                    return "image/png";

                case ".js":
                    return "application/javascript";

                case ".htm":
                    return "text/html";

                case ".css":
                    return "text/css";

                default:
                    return "application/octet-stream";
            }
        }
    }
}
