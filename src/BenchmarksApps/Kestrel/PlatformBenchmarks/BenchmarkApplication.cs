// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace PlatformBenchmarks
{
    public partial class BenchmarkApplication
    {
        private readonly static AsciiString _applicationName = "Kestrel Platform-Level Application";
        public static AsciiString ApplicationName => _applicationName;

        private readonly static AsciiString _crlf = "\r\n";
        private readonly static AsciiString _eoh = "\r\n\r\n"; // End Of Headers
        private readonly static AsciiString _http11OK = "HTTP/1.1 200 OK\r\n";
        private readonly static AsciiString _headerServer = "Server: Custom";
        private readonly static AsciiString _headerContentLength = "Content-Length: ";
        private readonly static AsciiString _headerContentLengthZero = "Content-Length: 0\r\n";
        private readonly static AsciiString _headerContentTypeText = "Content-Type: text/plain\r\n";
        private readonly static AsciiString _headerContentTypeJson = "Content-Type: application/json\r\n";

        private readonly static AsciiString _plainTextBody = "Hello, World!";

        private static readonly JsonSerializerOptions SerializerOptions = CreateSerializeOptions();

        private static JsonSerializerOptions CreateSerializeOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new AsciiStringConverter());
            return options;
        }

        public static class Paths
        {
            public readonly static AsciiString Plaintext = "/plaintext";
            public readonly static AsciiString Json = "/json";
        }

        private RequestType _requestType;

        public void OnStartLine(HttpMethod method, HttpVersion version, Span<byte> target, Span<byte> path, Span<byte> query, Span<byte> customMethod, bool pathEncoded)
        {
            var requestType = RequestType.NotRecognized;
            if (method == HttpMethod.Get)
            {
                if (Paths.Plaintext.Length <= path.Length && path.StartsWith(Paths.Plaintext))
                {
                    requestType = RequestType.PlainText;
                }
                else if (Paths.Json.Length <= path.Length && path.StartsWith(Paths.Json))
                {
                    requestType = RequestType.Json;
                }
            }

            _requestType = requestType;
        }

        public void PrepareResponse(byte[] output, ref int offset)
        {
            if (_requestType == RequestType.PlainText)
            {
                PlainText(output, ref offset);
            }
            else if (_requestType == RequestType.Json)
            {
                Json(output, ref offset);
            }
            else
            {
                Default(output, ref offset);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyTo(in ReadOnlySpan<byte> from, ref Span<byte> to, ref int offset)
        {
            from.CopyTo(to);
            to = to.Slice(from.Length);
            offset += from.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteNumeric(uint number, ref Span<byte> to, ref int offset)
        {
            const byte AsciiDigitStart = (byte)'0';

            int advanceBy = 0;
            if (to.Length >= 3)
            {
                if (number < 10)
                {
                    to[0] = (byte)(number + AsciiDigitStart);
                    advanceBy = 1;
                }
                else if (number < 100)
                {
                    var tens = (byte)((number * 205u) >> 11); // div10, valid to 1028

                    to[0] = (byte)(tens + AsciiDigitStart);
                    to[1] = (byte)(number - (tens * 10) + AsciiDigitStart);
                    advanceBy = 2;
                }
                else if (number < 1000)
                {
                    var digit0 = (byte)((number * 41u) >> 12); // div100, valid to 1098
                    var digits01 = (byte)((number * 205u) >> 11); // div10, valid to 1028

                    to[0] = (byte)(digit0 + AsciiDigitStart);
                    to[1] = (byte)(digits01 - (digit0 * 10) + AsciiDigitStart);
                    to[2] = (byte)(number - (digits01 * 10) + AsciiDigitStart);
                    advanceBy = 3;
                }
            }

            to = to.Slice(advanceBy);
            offset += advanceBy;
        }

        private static void PlainText(byte[] output, ref int offset)
        {
            var span = new Span<byte>(output, offset, output.Length - offset);

            // HTTP 1.1 OK
            CopyTo(_http11OK, ref span, ref offset);

            // Server headers
            CopyTo(_headerServer, ref span, ref offset);

            // Date header
            CopyTo(DateHeader.HeaderBytes, ref span, ref offset);

            // Content-Type header
            CopyTo(_headerContentTypeText, ref span, ref offset);

            // Content-Length header
            CopyTo(_headerContentLength, ref span, ref offset);
            WriteNumeric((uint)_plainTextBody.Length, ref span, ref offset);

            // End of headers
            CopyTo(_eoh, ref span, ref offset);

            // Body
            CopyTo(_plainTextBody, ref span, ref offset);
        }

        private static void Json(byte[] output, ref int offset)
        {
            var span = new Span<byte>(output, offset, output.Length - offset);
            // HTTP 1.1 OK
            CopyTo(_http11OK, ref span, ref offset);

            // Server headers
            CopyTo(_headerServer, ref span, ref offset);

            // Date header
            CopyTo(DateHeader.HeaderBytes, ref span, ref offset);

            // Content-Type header
            CopyTo(_headerContentTypeJson, ref span, ref offset);

            // Content-Length header
            CopyTo(_headerContentLength, ref span, ref offset);
            //var jsonPayload = JsonSerializer.SerializeToUtf8Bytes(new JsonMessage { message = "Hello, World!" }, SerializerOptions);
            WriteNumeric((uint)27, ref span, ref offset);

            // End of headers
            CopyTo(_eoh, ref span, ref offset);

            // Body
            using (var utf8Writer = new Utf8JsonWriter(new ArrayBufferWriter(output, offset)))
            {
                JsonSerializer.Serialize<JsonMessage>(utf8Writer, new JsonMessage { message = _plainTextBody }, SerializerOptions);
                offset += 27;
            }
        }

        private sealed class ArrayBufferWriter : IBufferWriter<byte>
        {
            private byte[] _output;
            private int _offset;

            public ArrayBufferWriter(byte[] output, int offset)
            {
                _output = output;
                _offset = offset;
            }

            internal int Offset => _offset;

            public void Advance(int count) => _offset += count;

            public Memory<byte> GetMemory(int sizeHint = 0) => new Memory<byte>(_output, _offset, _output.Length - _offset);

            public Span<byte> GetSpan(int sizeHint = 0) => new Span<byte>(_output, _offset, _output.Length - _offset);
        }

        private static void Default(byte[] output, ref int offset)
        {
            var span = new Span<byte>(output);

            // HTTP 1.1 OK
            CopyTo(_http11OK.AsSpan(), ref span, ref offset);

            // Server headers
            CopyTo(_headerServer.AsSpan(), ref span, ref offset);

            // Date header
            CopyTo(DateHeader.HeaderBytes, ref span, ref offset);

            // Content-Length 0
            CopyTo(_headerContentLengthZero, ref span, ref offset);

            // End of headers
            CopyTo(_crlf, ref span, ref offset);
        }

        private enum RequestType
        {
            NotRecognized,
            PlainText,
            Json
        }

        public struct JsonMessage
        {
            public AsciiString message { get; set; }
        }
    }

    internal class AsciiStringConverter : JsonConverter<AsciiString>
    {
        public AsciiStringConverter() { }

        public override AsciiString Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new AsciiString(reader.GetString());

        public override void Write(Utf8JsonWriter writer, AsciiString value, JsonSerializerOptions options)
            => writer.WriteStringValue(utf8Value: value.AsSpan());
    }
}
