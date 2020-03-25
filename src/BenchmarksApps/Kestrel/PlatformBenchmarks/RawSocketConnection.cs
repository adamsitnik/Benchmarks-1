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
        private readonly Socket _acceptSocket;
        private readonly SocketTransportOptions _options;

        public RawSocketConnection(Socket acceptSocket, SocketTransportOptions options)
        {
            _acceptSocket = acceptSocket;
            _options = options;
        }

        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }

        public void Start()
        {
            var benchmarkApplication = new BenchmarkApplication(_acceptSocket);
            benchmarkApplication.ProcessRequestsAsync();
        }
    }
}