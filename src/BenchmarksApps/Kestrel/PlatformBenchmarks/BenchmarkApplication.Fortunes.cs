// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO.Pipelines;
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
            Span<char> tableSpan = MemoryMarshal.Cast<byte, char>(tableWriter.Span);
            // Body

            tableWriter.Write(_fortunesTableStart);
            tableWriter.Write(_fortunesRowStart);
            bool first = true;
            foreach (var item in model)
            {
                if (first) first = false;
                else tableWriter.Write(_fortunesRowEndAndStart);

                tableWriter.Write(item.Id.ToString());
                tableWriter.Write(_fortunesColumn);
                HtmlEncoder.Encode(item.Message, MemoryMarshal.Cast<byte, char>(tableWriter.Span), out _, out int charsWritten, true);
                tableWriter.Advance(charsWritten * 2);
            }
            tableWriter.Write(_fortunesRowEnd);
            tableWriter.Write(_fortunesTableEnd);

            int bytesCount = Encoding.UTF8.GetBytes(tableSpan.Slice(0, (tableWriter.Buffered - writer.Buffered) / 2), writer.Span);
            writer.Advance(bytesCount);
            lengthWriter.WriteNumeric((uint)bytesCount);

            writer.Commit();
        }
    }
}
