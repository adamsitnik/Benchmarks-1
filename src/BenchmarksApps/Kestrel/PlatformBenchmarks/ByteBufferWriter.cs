using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace PlatformBenchmarks
{
    public sealed class ByteBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer;
        private int _index;

        private const int MinimumBufferSize = 256;

        public ByteBufferWriter(int initialCapacity)
        {
            _buffer = new byte[initialCapacity];
            _index = 0;
        }

        public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _index);

        public int WrittenCount => _index;

        public int Capacity => _buffer.Length;

        public int FreeCapacity => _buffer.Length - _index;

        public void Clear() => _index = 0;

        public void Advance(int count) => _index += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsMemory(_index);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            CheckAndResizeBuffer(sizeHint);
            return _buffer.AsSpan(_index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<byte> GetReadOnlySpan()
        {
            var span = _buffer.AsSpan(0, _index);
            _index = 0;
            return span;
        }

        private void CheckAndResizeBuffer(int sizeHint)
        {
            if (sizeHint == 0)
            {
                sizeHint = MinimumBufferSize;
            }

            int availableSpace = _buffer.Length - _index;

            if (sizeHint > availableSpace)
            {
                Array.Resize(ref _buffer, sizeHint);
            }
        }
    }
}
