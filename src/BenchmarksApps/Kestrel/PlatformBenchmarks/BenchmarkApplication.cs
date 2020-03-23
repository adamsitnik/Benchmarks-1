// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers.Binary;
using System.IO.Pipelines;
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

        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions();

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

        public void ProcessRequest(byte[] output)
        {
            if (_requestType == RequestType.PlainText)
            {
                PlainText(output, Socket);
            }
            else if (_requestType == RequestType.Json)
            {
                Json(output, Socket);
            }
            else
            {
                Default(output, Socket);
            }
        }

        private static void PlainText(byte[] output, System.Net.Sockets.Socket socket)
        {
            int length = 0;
            var span = new Span<byte>(output);
            // HTTP 1.1 OK
            var tmp = _http11OK.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Server headers
            tmp = _headerServer.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Date header
            tmp = DateHeader.HeaderBytes;
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Content-Type header
            tmp = _headerContentTypeText;
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Content-Length header
            tmp = _headerContentLength.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;
            if (BitConverter.IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)_plainTextBody.Length);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(span, (uint)_plainTextBody.Length);
            }
            span = span.Slice(sizeof(uint));
            length += tmp.Length;

            // End of headers
            tmp = _eoh.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Body
            tmp = _plainTextBody.AsSpan();
            tmp.CopyTo(span);
            // no slicing as we dont write anything more to this span
            length += tmp.Length;

            socket.Send(output, 0, length, System.Net.Sockets.SocketFlags.None, out _);
        }

        private static void Json(byte[] output, System.Net.Sockets.Socket socket)
        {
            int length = 0;
            var span = new Span<byte>(output);
            // HTTP 1.1 OK
            var tmp = _http11OK.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Server headers
            tmp = _headerServer.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Date header
            tmp = DateHeader.HeaderBytes;
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Content-Type header
            tmp = _headerContentTypeJson;
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Content-Length header
            tmp = _headerContentLength.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            var jsonPayload = JsonSerializer.SerializeToUtf8Bytes(new JsonMessage { message = "Hello, World!" }, SerializerOptions);
            if (BitConverter.IsLittleEndian)
            {
                BinaryPrimitives.WriteUInt32LittleEndian(span, (uint)jsonPayload.Length);
            }
            else
            {
                BinaryPrimitives.WriteUInt32BigEndian(span, (uint)jsonPayload.Length);
            }
            span = span.Slice(sizeof(uint));
            length += tmp.Length;

            // End of headers
            tmp = _eoh.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Body
            tmp = jsonPayload.AsSpan();
            tmp.CopyTo(span);
            length += jsonPayload.Length;

            socket.Send(output, 0, length, System.Net.Sockets.SocketFlags.None, out _);
        }

        private static void Default(byte[] output, System.Net.Sockets.Socket socket)
        {
            int length = 0;
            var span = new Span<byte>(output);
            // HTTP 1.1 OK
            var tmp = _http11OK.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Server headers
            tmp = _headerServer.AsSpan();
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Date header
            tmp = DateHeader.HeaderBytes;
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // Content-Length 0
            tmp = _headerContentLengthZero;
            tmp.CopyTo(span);
            span = span.Slice(tmp.Length);
            length += tmp.Length;

            // End of headers
            tmp = _crlf;
            tmp.CopyTo(span);
            length += tmp.Length;

            socket.Send(output, 0, length, System.Net.Sockets.SocketFlags.None, out _);
        }

        private enum RequestType
        {
            NotRecognized,
            PlainText,
            Json
        }

        public struct JsonMessage
        {
            public string message { get; set; }
        }
    }
}
