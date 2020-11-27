// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Npgsql;

namespace PlatformBenchmarks
{
    public class RawDb
    {
        private const int MaxCount = 10001;
        private readonly ConcurrentRandom _random;
        private readonly string _connectionString;
        private readonly CachedWorld[] _cache = new CachedWorld[MaxCount];

        public RawDb(ConcurrentRandom random, AppSettings appSettings)
        {
            _random = random;
            _connectionString = appSettings.ConnectionString;
        }

        public async Task<World> LoadSingleQueryRow()
        {
            using (var db = new NpgsqlConnection(_connectionString))
            {
                await db.OpenAsync();

                var (cmd, _) = CreateReadCommand(db);
                using (cmd)
                {
                    return await ReadSingleRow(cmd);
                }
            }
        }

        public async Task<World[]> LoadMultipleQueriesRows(int count)
        {
            var result = new World[count];

            using (var db = new NpgsqlConnection(_connectionString))
            {
                await db.OpenAsync();

                var (cmd, idParameter) = CreateReadCommand(db);
                using (cmd)
                {
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = await ReadSingleRow(cmd);
                        idParameter.TypedValue = _random.Next(1, MaxCount);
                    }
                }
            }

            return result;
        }

        public Task<World[]> LoadCachedQueries(int count)
        {
            var result = new World[count];
            var cache = _cache;
            var random = _random;
            for (var i = 0; i < result.Length; i++)
            {
                var id = random.Next(1, 10001);
                result[i] = cache[id];
            }

            return Task.FromResult(result);
        }

        public async Task PopulateCache()
        {
            using (var db = new NpgsqlConnection(_connectionString))
            {
                await db.OpenAsync();

                var (cmd, idParameter) = CreateReadCommand(db);
                using (cmd)
                {
                    var cache = _cache;
                    for (var i = 1; i < MaxCount; i++)
                    {
                        idParameter.TypedValue = i;
                        cache[i] = await ReadSingleRow(cmd);
                    }
                }
            }

            Console.WriteLine("Caching Populated");
        }

        public async Task<World[]> LoadMultipleUpdatesRows(int count)
        {
            var results = new World[count];

            using (var db = new NpgsqlConnection(_connectionString))
            {
                await db.OpenAsync();

                var (queryCmd, queryParameter) = CreateReadCommand(db);
                using (queryCmd)
                {
                    for (int i = 0; i < results.Length; i++)
                    {
                        results[i] = await ReadSingleRow(queryCmd);
                        queryParameter.TypedValue = _random.Next(1, 10001);
                    }
                }

                using (var updateCmd = new NpgsqlCommand(BatchUpdateString.Query(count), db))
                {
                    var ids = BatchUpdateString.Ids;
                    var randoms = BatchUpdateString.Randoms;

                    for (int i = 0; i < results.Length; i++)
                    {
                        var randomNumber = _random.Next(1, 10001);

                        updateCmd.Parameters.Add(new NpgsqlParameter<int>(parameterName: ids[i], value: results[i].Id));
                        updateCmd.Parameters.Add(new NpgsqlParameter<int>(parameterName: randoms[i], value: randomNumber));

                        results[i].RandomNumber = randomNumber;
                    }

                    await updateCmd.ExecuteNonQueryAsync();
                }
            }

            return results;
        }

        public async Task<List<Fortune>> LoadFortunesRows()
        {
            var result = new List<Fortune>(20);

            using (var db = new NpgsqlConnection(_connectionString))
            {
                await db.OpenAsync();

                using (var cmd = new NpgsqlCommand("SELECT id, message FROM fortune", db))
                using (var rdr = await cmd.ExecuteReaderAsync())
                {
                    while (await rdr.ReadAsync())
                    {
                        result.Add(new Fortune
                        (
                            id:rdr.GetInt32(0),
                            message: rdr.GetString(1)
                        ));
                    }
                }
            }

            result.Add(new Fortune(id: 0, message: "Additional fortune added at request time." ));
            result.Sort();

            return result;
        }

        private (NpgsqlCommand readCmd, NpgsqlParameter<int> idParameter) CreateReadCommand(NpgsqlConnection connection)
        {
            var cmd = new NpgsqlCommand("SELECT id, randomnumber FROM world WHERE id = @Id", connection);
            var parameter = new NpgsqlParameter<int>(parameterName: "@Id", value: _random.Next(1, 10001));

            cmd.Parameters.Add(parameter);

            return (cmd, parameter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private async Task<World> ReadSingleRow(NpgsqlCommand cmd)
        {
            using (var rdr = await cmd.ExecuteReaderAsync(System.Data.CommandBehavior.SingleRow))
            {
                await rdr.ReadAsync();

                return new World
                {
                    Id = rdr.GetInt32(0),
                    RandomNumber = rdr.GetInt32(1)
                };
            }
        }
    }
}
