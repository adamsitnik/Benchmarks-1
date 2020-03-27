// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace PlatformBenchmarks
{
    public static class HttpApplicationConnectionBuilderExtensions
    {
        public static IConnectionBuilder UseHttpApplication(this IConnectionBuilder builder)
        {
            return builder.Use(next => new HttpApplication().ExecuteAsync);
        }
    }

    public class HttpApplication
    {
        public Task ExecuteAsync(ConnectionContext connection)
        {
            var rawSocketConnection = (RawSocketConnection)connection;

            var app = new BenchmarkApplication(rawSocketConnection.Socket);

            return app.StartAsync();
        }
    }
}
