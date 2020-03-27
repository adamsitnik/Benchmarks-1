// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace PlatformBenchmarks
{
    public partial class BenchmarkApplication
    {
        private readonly Socket _socket;
        private readonly SocketAwaitableEventArgs _awaitableEventArgs = new SocketAwaitableEventArgs();
        private State _state;

        public BenchmarkApplication(Socket socket) => _socket = socket;

        private HttpParser<ParsingAdapter> Parser { get; } = new HttpParser<ParsingAdapter>();
        
        private SocketAwaitableEventArgs WaitForDataAsync(Memory<byte> memory)
        {
            _awaitableEventArgs.SetBuffer(memory);

            if (!_socket.ReceiveAsync(_awaitableEventArgs))
            {
                _awaitableEventArgs.Complete();
            }

            return _awaitableEventArgs;
        }

        internal async Task StartAsync()
        {
            try
            {
                await ProcessRequestsAsync();
            }
            finally
            {
                _awaitableEventArgs.Dispose();
            }
        }

        private async Task ProcessRequestsAsync()
        {
            byte[] output = new byte[16 * 1024];
            byte[] input = new byte[16 * 1024];

            var socket = _socket;
            var segment = new Memory<byte>(input);

            while (true)
            {
                await WaitForDataAsync(Memory<byte>.Empty);
                
                var bytesRead = await WaitForDataAsync(segment);
                if (bytesRead == 0)
                {
                    return;
                }
                
                var buffer = new ReadOnlySequence<byte>(input, 0, bytesRead);
                int offset = 0;

                while (true)
                {
                    if (!ParseHttpRequest(ref buffer, true, out var examined))
                    {
                        return;
                    }

                    if (_state == State.Body)
                    {
                        ProcessRequest(output, ref offset);

                        _state = State.StartLine;

                        if (!buffer.IsEmpty)
                        {
                            // More input data to parse
                            continue;
                        }
                    }

                    // No more input or incomplete data, Advance the Reader
                    break;
                }

                socket.Send(output, 0, offset, SocketFlags.None, out var error);
            }
        }

        private bool ParseHttpRequest(ref ReadOnlySequence<byte> buffer, bool isCompleted, out SequencePosition examined)
        {
            examined = buffer.End;

            var consumed = buffer.Start;
            var state = _state;

            if (!buffer.IsEmpty)
            {
                if (state == State.StartLine)
                {
                    if (Parser.ParseRequestLine(new ParsingAdapter(this), buffer, out consumed, out examined))
                    {
                        state = State.Headers;
                    }

                    buffer = buffer.Slice(consumed);
                }

                if (state == State.Headers)
                {
                    var reader = new SequenceReader<byte>(buffer);
                    var success = Parser.ParseHeaders(new ParsingAdapter(this), ref reader);

                    consumed = reader.Position;
                    if (success)
                    {
                        examined = consumed;
                        state = State.Body;
                    }
                    else
                    {
                        examined = buffer.End;
                    }

                    buffer = buffer.Slice(consumed);
                }

                if (state != State.Body && isCompleted)
                {
                    ThrowUnexpectedEndOfData();
                }
            }
            else if (isCompleted)
            {
                return false;
            }

            _state = state;
            return true;
        }

#if NETCOREAPP5_0

        public void OnStaticIndexedHeader(int index)
        {
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
        }

        public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
        }

        public void OnHeadersComplete(bool endStream)
        {
        }
#else
        public void OnHeader(Span<byte> name, Span<byte> value)
        {
        }

        public void OnHeadersComplete()
        {
        }
#endif

        private static void ThrowUnexpectedEndOfData()
        {
            throw new InvalidOperationException("Unexpected end of data!");
        }

        private enum State
        {
            StartLine,
            Headers,
            Body
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static BufferWriter<WriterAdapter> GetWriter(PipeWriter pipeWriter)
            => new BufferWriter<WriterAdapter>(new WriterAdapter(pipeWriter));

        private struct WriterAdapter : IBufferWriter<byte>
        {
            public PipeWriter Writer;

            public WriterAdapter(PipeWriter writer)
                => Writer = writer;

            public void Advance(int count)
                => Writer.Advance(count);

            public Memory<byte> GetMemory(int sizeHint = 0)
                => Writer.GetMemory(sizeHint);

            public Span<byte> GetSpan(int sizeHint = 0)
                => Writer.GetSpan(sizeHint);
        }

        private struct ParsingAdapter : IHttpRequestLineHandler, IHttpHeadersHandler
        {
            public BenchmarkApplication RequestHandler;

            public ParsingAdapter(BenchmarkApplication requestHandler)
                => RequestHandler = requestHandler;

#if NETCOREAPP5_0
            public void OnStaticIndexedHeader(int index) 
                => RequestHandler.OnStaticIndexedHeader(index);

            public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
                => RequestHandler.OnStaticIndexedHeader(index, value);

            public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
                => RequestHandler.OnHeader(name, value);

            public void OnHeadersComplete(bool endStream)
                => RequestHandler.OnHeadersComplete(endStream);
#else
            public void OnHeader(Span<byte> name, Span<byte> value)
                => RequestHandler.OnHeader(name, value);

            public void OnHeadersComplete()
                => RequestHandler.OnHeadersComplete();
#endif

            public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
                => RequestHandler.OnStartLine(method, version, target, path, query, customMethod, pathEncoded);

#if !NETCOREAPP
#error This is a .NET Core 3.0 application and needs to be compiled for <TargetFramework>$(DefaultNetCoreTargetFramework)</TargetFramework>
#endif
        }
    }

}
