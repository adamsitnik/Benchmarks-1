// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace PlatformBenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(BenchmarkApplication.ApplicationName);
            Console.WriteLine(BenchmarkApplication.Paths.Plaintext);
            Console.WriteLine(BenchmarkApplication.Paths.Json);
            DateHeader.SyncDateTimer();

            if (int.TryParse(Environment.GetEnvironmentVariable("maxThreadCount"), out int max))
            {
                Console.WriteLine($"Thread count: <{max - 1}, {max}> {ThreadPool.SetMinThreads(max - 1, max - 1)} {ThreadPool.SetMaxThreads(max, max)}");
            }

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .UseConfiguration(config)
                .ConfigureKestrel((context, options) =>
                {
                    IPEndPoint endPoint = context.Configuration.CreateIPEndPoint();
                
                    options.Listen(endPoint, builder =>
                    {
                        //builder.UseHttpApplication<BenchmarkApplication>();
                    });
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IConnectionListenerFactory, RawSocketTransportFactory>();
                    
                    services.AddTransient<IConfigureOptions<KestrelServerOptions>, RawKestrelServerOptionsSetup>();
                    services.AddSingleton<IServer, KestrelServer>();
                })
                .UseStartup<Startup>()
                .Build();

            return host;
        }
    }
}
