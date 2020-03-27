using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Options;

namespace PlatformBenchmarks
{
    public sealed class RawSocketTransportFactory : IConnectionListenerFactory
    {
        private readonly SocketTransportOptions _options;

        public RawSocketTransportFactory(IOptions<SocketTransportOptions> options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _options = options.Value;
        }

        public ValueTask<IConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
        {
            var transport = new RawSocketConnectionListener(endpoint, _options);
            transport.Bind();
            return new ValueTask<IConnectionListener>(transport);
        }
    }
}