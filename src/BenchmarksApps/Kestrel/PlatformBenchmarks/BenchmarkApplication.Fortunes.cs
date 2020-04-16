// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace PlatformBenchmarks
{
    public partial class BenchmarkApplication
    {
        private async Task Fortunes(PipeWriter pipeWriter)
        {
            OutputFortunes(pipeWriter, await Db.LoadFortunesRows());
        }

        private void OutputFortunes(PipeWriter pipeWriter, List<Fortune> model)
        {
            var writer = GetWriter(pipeWriter);

            // HTTP 1.1 OK
            writer.Write(_http11OK);

            // Server headers
            writer.Write(_headerServer);

            // Date header
            writer.Write(DateHeader.HeaderBytes);

            // Content-Type header
            writer.Write(_headerContentTypeHtml);

            // Content-Length header
            writer.Write(_headerContentLength);

            var lengthWriter = writer;
            writer.Write(_contentLengthGap);

            // End of headers
            writer.Write(_eoh);

            var tableWriter = writer;
            Span<char> tableBegining = MemoryMarshal.Cast<byte, char>(tableWriter.Span);
            Span<char> tableSpan = tableBegining;
            // Body

            Write(ref tableSpan, _fortunesTableStart);
            Write(ref tableSpan, _fortunesRowStart);

            bool first = true;
            foreach (var item in model)
            {
                if (first) first = false;
                else Write(ref tableSpan, _fortunesRowEndAndStart);

                Write(ref tableSpan, item.Id.ToString());
                Write(ref tableSpan, _fortunesColumn);

                HtmlEncoder.Encode(item.Message, tableSpan, out _, out int charsWritten, true);
                tableSpan = tableSpan.Slice(charsWritten);
            }
            Write(ref tableSpan, _fortunesRowEnd);
            Write(ref tableSpan, _fortunesTableEnd);

            int bytesCount = Encoding.UTF8.GetBytes(tableBegining.Slice(0, tableBegining.Length - tableSpan.Length), writer.Span);
            writer.Advance(bytesCount);
            lengthWriter.WriteNumeric((uint)bytesCount);

            writer.Commit();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Write(ref Span<char> tableSpan, string text)
        {
            text.AsSpan().CopyTo(tableSpan);
            tableSpan = tableSpan.Slice(text.Length);
        }
    }
}
