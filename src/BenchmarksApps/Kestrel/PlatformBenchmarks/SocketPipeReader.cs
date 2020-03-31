using System;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformBenchmarks
{
    internal sealed class SocketPipeReader : PipeReader
    {
        private Socket _socket;
        private byte[] _array;
        private SocketAwaitableEventArgs _awaitableEventArgs;

        public SocketPipeReader(Socket socket)
        {
            _socket = socket;
            _array = new byte[16 * 1024];
            _awaitableEventArgs = new SocketAwaitableEventArgs();
        }

        public override void AdvanceTo(SequencePosition consumed) { } // todo: implement

        public override void AdvanceTo(SequencePosition consumed, SequencePosition examined) { } // todo: implement

        public override void CancelPendingRead() => throw new NotSupportedException();

        public override bool TryRead(out ReadResult result) => throw new NotSupportedException();

        public override void Complete(Exception exception = null) {  } // nop

        public override async ValueTask<ReadResult> ReadAsync(CancellationToken cancellationToken = default)
        {
            var array = _array;
            var args = _awaitableEventArgs;
            args.SetBuffer(new Memory<byte>(array));

            if (_socket.ReceiveAsync(args))
            {
                // returns true if the I / O operation is pending
                await args;
            }

            return new ReadResult(new System.Buffers.ReadOnlySequence<byte>(array, 0, args.GetResult()), isCanceled: false, isCompleted: true);
        }
    }

    internal sealed class SocketAwaitableEventArgs : SocketAsyncEventArgs, ICriticalNotifyCompletion
    {
        private static readonly Action _callbackCompleted = () => { };

        private Action _callback;

        public SocketAwaitableEventArgs()
#if NETCOREAPP5_0
            : base(unsafeSuppressExecutionContextFlow: true)
#endif
        {
        }

        public SocketAwaitableEventArgs GetAwaiter() => this;
        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public int GetResult()
        {
            //Debug.Assert(ReferenceEquals(_callback, _callbackCompleted));

            _callback = null;

            if (SocketError != SocketError.Success)
            {
                ThrowSocketException(SocketError);
            }

            return BytesTransferred;

            static void ThrowSocketException(SocketError e)
            {
                throw new SocketException((int)e);
            }
        }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(_callback, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
            {
                Task.Run(continuation);
            }
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Complete()
        {
            OnCompleted(this);
        }

        protected override void OnCompleted(SocketAsyncEventArgs _)
        {
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);

            if (continuation != null)
            {
                continuation.Invoke();
                //PipeScheduler.Inline.Schedule(state => ((Action)state)(), continuation);
            }
        }
    }
}
