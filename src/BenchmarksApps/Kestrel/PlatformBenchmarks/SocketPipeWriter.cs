using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformBenchmarks
{
    internal sealed class SocketPipeWriter : PipeWriter
    {
        private Socket _socket;
        private byte[] _array;
        private int _offset;

        public SocketPipeWriter(Socket socket)
        {
            _socket = socket;
            _array = new byte[16 * 1024];
            _offset = 0;
        }

        public override void Advance(int bytes)
        {
            _offset += bytes;

            if (_offset >= _array.Length)
            {
                Array.Resize(ref _array, _array.Length * 2);
            }
        }

        public override void CancelPendingFlush() => throw new NotSupportedException();

        public override void Complete(Exception exception = null) => _offset = 0;

        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
        {
            // we take advantage of the fact that all writes in TE are always small and non-blocking
            // so we perform a SYNC send on purpose

            int start = 0;
            int toSent = _offset;

            do
            {
                int bytesSent = await _socket.SendAsync(new ReadOnlyMemory<byte>(_array, start, toSent), SocketFlags.None, cancellationToken);

                start += bytesSent;
                toSent -= bytesSent;
            }
            while (toSent > 0);

            _offset = 0;

            return new FlushResult(isCanceled: false, isCompleted: true);
        }

        public override Memory<byte> GetMemory(int sizeHint = 0) => new Memory<byte>(_array, _offset, _array.Length - _offset);

        public override Span<byte> GetSpan(int sizeHint = 0) => new Span<byte>(_array, _offset, _array.Length - _offset);
    }
}
