using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace PlatformBenchmarks
{
    public class RawSocketConnection : ConnectionContext
    {
        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }
        private Socket Socket { get; }

        public RawSocketConnection(Socket socket)
        {
            Features = new FeatureCollection();
            Transport = new SocketPipe(socket);
            LocalEndPoint = socket.LocalEndPoint;
            RemoteEndPoint = socket.RemoteEndPoint;
            ConnectionId = Guid.NewGuid().ToString();
        }

        public override ValueTask DisposeAsync()
        {
            Socket.Dispose();

            return default;
        }
        
    }
}