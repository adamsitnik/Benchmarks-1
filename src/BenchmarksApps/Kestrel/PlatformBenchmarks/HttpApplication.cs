﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO.Pipelines;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace PlatformBenchmarks
{
    public static class HttpApplicationConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseHttpApplication<TConnection>(this IConnectionBuilder builder) where TConnection : IHttpConnection, new()
        {
            return builder.Use(next => new HttpApplication<TConnection>().ExecuteAsync);
        }
    }

    public class HttpApplication<TConnection> where TConnection : IHttpConnection, new()
    {
        public Task ExecuteAsync(ConnectionContext connection)
        {
            var pipeField = connection.Transport.Output.GetType().GetField("_pipe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var pipe = pipeField.GetValue(connection.Transport.Output);

            var schedulerField = typeof(Pipe).GetField("_readerScheduler", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            schedulerField.SetValue(pipe, PipeScheduler.Inline);

            var httpConnection = new TConnection
            {
                Reader = connection.Transport.Input,
                Writer = connection.Transport.Output
            };
            return httpConnection.ExecuteAsync();
        }
    }
}
