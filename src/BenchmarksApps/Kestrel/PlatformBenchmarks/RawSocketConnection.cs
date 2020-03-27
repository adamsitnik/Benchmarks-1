using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace PlatformBenchmarks
{
    public class RawSocketConnection : ConnectionContext
    {
        private readonly Socket _socket;
        private readonly SocketTransportOptions _options;

        public RawSocketConnection(Socket socket, SocketTransportOptions options)
        {
            _socket = socket;
            _options = options;
            Features = new FeatureCollection();
        }

        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }

        public Socket Socket => _socket;
    }
}