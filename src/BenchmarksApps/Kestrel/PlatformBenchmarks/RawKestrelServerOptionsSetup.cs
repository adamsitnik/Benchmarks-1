using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace PlatformBenchmarks
{
    internal sealed class RawKestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        private readonly IServiceProvider _services;

        public RawKestrelServerOptionsSetup(IServiceProvider services) => _services = services;

        public void Configure(KestrelServerOptions options) => options.ApplicationServices = _services;
    }
}