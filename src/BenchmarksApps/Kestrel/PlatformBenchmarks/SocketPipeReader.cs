using System;
using System.Diagnostics;
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
        private int _offset;
        private int _length;
        private SocketAwaitableEventArgs _awaitableEventArgs;

        public SocketPipeReader(Socket socket, SocketAwaitableEventArgs awaitableEventArgs)
        {
            _socket = socket;
            _array = new byte[16 * 1024];
            _offset = 0;
            _length = 0;
            _awaitableEventArgs = awaitableEventArgs;
        }

        public override void AdvanceTo(SequencePosition consumed) => _offset += consumed.GetInteger();

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) => _offset += consumed.GetInteger();

        public override void CancelPendingRead() { } // nop

        public override bool TryRead(out ReadResult result) => throw new NotSupportedException();

        public override void Complete(Exception exception = null) {  } // nop

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            if (_offset == _length) // previously entire array was parsed (100% of cases for TechEmpower)
            {
                _offset = 0;
            }

            var array = _array;
            var args = _awaitableEventArgs;
            args.SetBuffer(new Memory<byte>(array, _offset, array.Length - _offset));

            if (_socket.ReceiveAsync(args))
            {
                // returns true if the I / O operation is pending
                await args;
            }

            _length = args.GetResult();

            return new ReadResult(new System.Buffers.ReadOnlySequence<byte>(array, _offset, _length), isCanceled: false, isCompleted: true);
        }
    }
}
