using System;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformBenchmarks
{
    internal sealed class SocketPipeReader : PipeReader
    {
        private Socket _socket;
        private byte[] _array;

        public SocketPipeReader(Socket socket)
        {
            _socket = socket;
            _array = new byte[16 * 512];
        }

        public override void AdvanceTo(SequencePosition consumed) { } // todo: implement

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { } // todo: implement

        public override void CancelPendingRead() => throw new NotSupportedException();

        public override bool TryRead(out ReadResult result) => throw new NotSupportedException();

        public override void Complete(Exception exception = null) {  } // nop

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            int bytesRead = await _socket.ReceiveAsync(new Memory<byte>(_array), SocketFlags.None, cancellationToken);

            if (bytesRead >= 0)
            {
                return new ReadResult(new System.Buffers.ReadOnlySequence<byte>(_array, 0, bytesRead), isCanceled: cancellationToken.IsCancellationRequested, isCompleted: true);
            }
            else
            {
                return new ReadResult(System.Buffers.ReadOnlySequence<byte>.Empty, isCanceled: cancellationToken.IsCancellationRequested, isCompleted: false);
            }
        }
    }
}
