// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace PlatformBenchmarks
{
    // Same as KestrelHttpServer\src\Kestrel.Core\Internal\Http\PipelineExtensions.cs
    // However methods accept T : struct, IBufferWriter<byte> rather than PipeWriter.
    // This allows a struct wrapper to turn CountingBufferWriter into a non-shared generic,
    // while still offering the WriteNumeric extension.

    public static class BufferExtensions
    {
        private const int _maxULongByteLength = 20;

        [ThreadStatic]
        private static byte[] _numericBytesScratch;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void WriteNumericMultiWrite<T>(ref this BufferWriter<T> buffer, uint number)
             where T : IBufferWriter<byte>
        {
            const byte AsciiDigitStart = (byte)'0';

            var value = number;
            var position = _maxULongByteLength;
            var byteBuffer = NumericBytesScratch;
            do
            {
                // Consider using Math.DivRem() if available
                var quotient = value / 10;
                byteBuffer[--position] = (byte)(AsciiDigitStart + (value - quotient * 10)); // 0x30 = '0'
                value = quotient;
            }
            while (value != 0);

            var length = _maxULongByteLength - position;
            buffer.Write(new ReadOnlySpan<byte>(byteBuffer, position, length));
        }

        private static byte[] NumericBytesScratch => _numericBytesScratch ?? CreateNumericBytesScratch();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static byte[] CreateNumericBytesScratch()
        {
            var bytes = new byte[_maxULongByteLength];
            _numericBytesScratch = bytes;
            return bytes;
        }
    }
}
