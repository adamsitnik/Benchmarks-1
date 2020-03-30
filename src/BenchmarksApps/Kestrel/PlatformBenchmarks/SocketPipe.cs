using System.IO.Pipelines;
using System.Net.Sockets;

namespace PlatformBenchmarks
{
    internal sealed class SocketPipe : IDuplexPipe
    {
        internal SocketPipe(Socket socket)
        {
            Input = new SocketPipeReader(socket);
            Output = new SocketPipeWriter(socket);
        }

        public PipeReader Input { get; }

        public PipeWriter Output { get; }
    }
}
