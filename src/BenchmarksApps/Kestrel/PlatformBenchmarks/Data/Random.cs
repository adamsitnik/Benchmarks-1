// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace PlatformBenchmarks
{
    public class ConcurrentRandom
    {
        private readonly Random Random = System.Random.Shared;

        public int Next(int minValue, int maxValue)
        {
            return Random.Next(minValue, maxValue);
        }
    }
}
