using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace PlatformBenchmarks
{
    public class RawSocketConnection : ConnectionContext
    {
        private readonly Socket _acceptSocket;
        private readonly SocketTransportOptions _options;
        private readonly BenchmarkApplication _benchmarkApplication;

        public RawSocketConnection(Socket acceptSocket, SocketTransportOptions options)
        {
            _acceptSocket = acceptSocket;
            _options = options;
            _benchmarkApplication = new BenchmarkApplication(_acceptSocket);
        }

        public override string ConnectionId { get; set; }
        public override IFeatureCollection Features { get; }
        public override IDictionary<object, object> Items { get; set; }
        public override IDuplexPipe Transport { get; set; }

        public Task Start() => _benchmarkApplication.ProcessRequestsAsync();
    }
}