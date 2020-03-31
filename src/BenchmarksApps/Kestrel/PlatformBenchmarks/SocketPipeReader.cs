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
            _array = new byte[16 * 1024];
        }

        public override void AdvanceTo(SequencePosition consumed) { } // todo: implement

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { } // todo: implement

        public override void CancelPendingRead() => throw new NotSupportedException();

        public override bool TryRead(out ReadResult result) => throw new NotSupportedException();

        public override void Complete(Exception exception = null) {  } // nop

        public override ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            ValueTask<int> readTask = _socket.ReceiveAsync(new Memory<byte>(_array), SocketFlags.None, cancellationToken);

            if (readTask.IsCompleted) // fast path
            {
                return new ValueTask<ReadResult>(new ReadResult(new System.Buffers.ReadOnlySequence<byte>(_array, 0, readTask.Result), isCanceled: false, isCompleted: true));
            }
            else
            {
                return ReadAsync(readTask);
            }
        }

        private async ValueTask<ReadResult> ReadAsync(ValueTask<int> readTask)
        {
            var bytesRead = await readTask;

            return new ReadResult(new System.Buffers.ReadOnlySequence<byte>(_array, 0, bytesRead), isCanceled: false, isCompleted: true);
        }
    }
}
