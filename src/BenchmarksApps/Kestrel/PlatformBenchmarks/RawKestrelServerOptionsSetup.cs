using System;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace PlatformBenchmarks
{
    public class RawKestrelServerOptionsSetup : IConfigureOptions<KestrelServerOptions>
    {
        private readonly IServiceProvider _services;

        public RawKestrelServerOptionsSetup(IServiceProvider services) => this._services = services;

        public void Configure(KestrelServerOptions options) => options.ApplicationServices = this._services;
    }
}