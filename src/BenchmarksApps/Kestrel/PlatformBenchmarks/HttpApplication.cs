// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Sockets;
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
            var socketField = connection.GetType().GetField("_socket", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var socket = socketField.GetValue(connection) as Socket;

            var httpConnection = new TConnection
            {
                Reader = connection.Transport.Input,
                Writer = connection.Transport.Output,
                Socket = socket
            };
            return httpConnection.ExecuteAsync();
        }
    }
}
