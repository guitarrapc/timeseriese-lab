using ConsoleAppFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleConsole.Data;
using SampleConsole.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ZLogger;

namespace SampleConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // args = "Runner migrate -count 100000".Split(" ");
            await Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) => services.AddTimeScaleDbConnection(hostContext.Configuration))
                .ConfigureServices((hostContext, services) => services.AddTimeScaleDbContext(hostContext.Configuration))
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddZLoggerConsole(true);
                })
                .RunConsoleAppFrameworkAsync(args);
        }
    }

    public class Runner : ConsoleAppBase
    {
        private readonly TimeScaleDbConnection _connection;
        private readonly TimescaleDbContext _dbContext;
        public Runner(TimeScaleDbConnection connection, TimescaleDbContext dbContext)
        {
            _connection = connection;
            _dbContext = dbContext;
        }

        [Command("test")]
        public async Task Test(string location, double temperature, double humidity)
        {
            var time = DateTime.UtcNow;

            Console.WriteLine("test insert data");
            using var connection = _connection.Create();
            using (var transaction = await connection.BeginTransactionAsync(Context.CancellationToken))
            {
                await connection.OpenAsync(Context.CancellationToken);
                await Condition.InsertBulkAsync(connection, transaction, new[] {
                    new Condition
                    {
                        Location = location,
                        Temperature = temperature,
                        Humidity = humidity,
                        Time = time,
                    }
                });
                await transaction.CommitAsync();
            }

            Console.WriteLine("test read from database");
            var conditions = await Condition.BetweenAsync(connection, time, time);
            foreach (var condition in conditions)
            {
                Console.WriteLine($"{condition.Time}, {condition.Location}, {condition.Temperature}, {condition.Humidity}");
            }
        }

        [Command("keep")]
        public async Task Keep()
        {
            Console.WriteLine($"keep inserting data");
            using var connection = _connection.Create();
            await connection.OpenAsync(Context.CancellationToken);

            var current = 1;
            while (!Context.CancellationToken.IsCancellationRequested)
            {
                Console.Write($"{current} ");
                var time = DateTime.UtcNow;
                var data = Enumerable.Concat(
                    Condition.GenerateRandomOfficeData(time, 1),
                    Condition.GenerateRandomHomeData(time, 1)
                )
                .ToArray();
                using (var transaction = await connection.BeginTransactionAsync(Context.CancellationToken))
                {
                    await Condition.InsertBulkAsync(connection, transaction, data);
                    await transaction.CommitAsync();
                }

                await Task.Delay(TimeSpan.FromSeconds(1), Context.CancellationToken);
                current++;
            }
        }

        [Command("random")]
        public async Task Random(int count = 1000)
        {
            var time = DateTime.UtcNow;
            var data = Enumerable.Concat(
                Condition.GenerateRandomOfficeData(time.AddSeconds(-count), count),
                Condition.GenerateRandomHomeData(time.AddSeconds(-count), count)
                )
                .ToArray();

            Console.WriteLine($"insert random {count} data");
            using var connection = _connection.Create();
            using (var transaction = await connection.BeginTransactionAsync(Context.CancellationToken))
            {
                await connection.OpenAsync(Context.CancellationToken);
                await Condition.InsertBulkAsync(connection, transaction, data);
                await transaction.CommitAsync();
            }

            Console.WriteLine("read from database");
            var conditions = await Condition.BetweenAsync(connection, time.AddSeconds(-count), time);
            foreach (var condition in conditions)
            {
                Console.WriteLine($"{condition.Time}, {condition.Location}, {condition.Temperature}, {condition.Humidity}");
            }
        }

        [Command("migrate")]
        public async Task Migrate()
        {
            await _dbContext.Database.MigrateAsync(Context.CancellationToken);
            //await SeedCopy(parallel, count);
            //await SameSeedCopy(parallel, count);
            //await SmallSameSeedCopy(parallel, count);
        }

        [Command("seed")]
        public async Task Seed(int parallel = 100, int count = 10000)
        {
            Console.WriteLine($"Begin seed database. {count} rows, parallel {parallel}");
            var size = count;
            var initData = Condition.GenerateRandomOfficeData(new DateTime(2020, 1, 1, 0, 0, 0), size);
            var groups = initData.Buffer(size / parallel).ToArray();

            var gate = new object();
            var completed = 0;
            var tasks = new List<Task>();
            var ct = Context.CancellationToken;
            var sw = Stopwatch.StartNew();
            foreach (var group in groups)
            {
                var task = Task.Run(async () =>
                {
                    using (var connection = _connection.Create())
                    {
                        await connection.OpenAsync(ct);
                            // 10000 will cause timeout
                            foreach (var data in group.Buffer(1000))
                        {
                            using (var transaction = await connection.BeginTransactionAsync(ct))
                            {
                                try
                                {
                                    var rows = await Condition.InsertBulkAsync(connection, transaction, data);
                                    await transaction.CommitAsync(ct);
                                    lock (gate)
                                    {
                                        completed += rows;
                                    }
                                    Console.WriteLine($"complete {completed}/{count}");
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine(ex);
                                    await transaction.RollbackAsync(ct);
                                }
                            }
                        }
                    }
                }, ct);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Console.WriteLine($"Complete seed database. plan {count}, completed {completed}, duration {sw.Elapsed.TotalSeconds}sec");
        }

        [Command("seedcopy")]
        public async Task SeedCopy(int parallel = 100, int count = 10000)
        {
            Console.WriteLine($"Begin seed copy database. {count} rows, parallel {parallel}");
            var size = count;
            var initData = Condition.GenerateRandomOfficeData(new DateTime(2020, 1, 1, 0, 0, 0), size);
            var groups = initData.Buffer(size / parallel).ToArray();

            var gate = new object();
            ulong completed = 0;
            var tasks = new List<Task>();
            var ct = Context.CancellationToken;
            var sw = Stopwatch.StartNew();
            foreach (var group in groups)
            {
                var task = Task.Run(async () =>
                {
                    using (var connection = _connection.Create())
                    {
                        await connection.OpenAsync(ct);
                            // 10000 will cause connection is not open.
                            foreach (var data in group.Buffer(5000))
                        {
                            try
                            {
                                var rows = await Condition.CopyAsync(connection, data, ct);
                                lock (gate)
                                {
                                    completed += rows;
                                }
                                Console.WriteLine($"complete {completed}/{count}");
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"{connection.State} {ex}");
                            }
                        }
                    }
                }, ct);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Console.WriteLine($"Complete seed database. plan {count}, completed {completed}, duration {sw.Elapsed.TotalSeconds}sec");
        }

        [Command("sameseedcopy")]
        public async Task SameSeedCopy(int parallel = 100, int count = 10000)
        {
            Console.WriteLine($"Begin seed copy database. {count} rows, parallel {parallel}");
            var size = count;
            var initData = SimpleData.GenerateSameData(0, size);
            var groups = initData.Buffer(size / parallel).ToArray();

            var gate = new object();
            ulong completed = 0;
            var tasks = new List<Task>();
            var ct = Context.CancellationToken;
            var sw = Stopwatch.StartNew();
            foreach (var group in groups)
            {
                var task = Task.Run(async () =>
                {
                    using (var connection = _connection.Create())
                    {
                        await connection.OpenAsync(ct);
                        // 10000 will cause connection is not open.
                        foreach (var data in group.Buffer(5000))
                        {
                            try
                            {
                                var rows = await SimpleData.CopyAsync(connection, data, ct);
                                lock (gate)
                                {
                                    completed += rows;
                                }
                                Console.WriteLine($"complete {completed}/{count}");
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"{connection.State} {ex}");
                            }
                        }
                    }
                }, ct);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Console.WriteLine($"Complete seed database. plan {count}, completed {completed}, duration {sw.Elapsed.TotalSeconds}sec");
        }
        
        [Command("smallsameseedcopy")]
        public async Task SmallSameSeedCopy(int parallel = 100, int count = 10000)
        {
            Console.WriteLine($"Begin seed copy database. {count} rows, parallel {parallel}");
            var size = count;
            var initData = SimpleSmallData.GenerateSameData(0, size);
            var groups = initData.Buffer(size / parallel).ToArray();

            var gate = new object();
            ulong completed = 0;
            var tasks = new List<Task>();
            var ct = Context.CancellationToken;
            var sw = Stopwatch.StartNew();
            foreach (var group in groups)
            {
                var task = Task.Run(async () =>
                {
                    using (var connection = _connection.Create())
                    {
                        await connection.OpenAsync(ct);
                        // 10000 will cause connection is not open.
                        foreach (var data in group.Buffer(5000))
                        {
                            try
                            {
                                var rows = await SimpleSmallData.CopyAsync(connection, data, ct);
                                lock (gate)
                                {
                                    completed += rows;
                                }
                                Console.WriteLine($"complete {completed}/{count}");
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine($"{connection.State} {ex}");
                            }
                        }
                    }
                }, ct);
                tasks.Add(task);
            }
            await Task.WhenAll(tasks);
            Console.WriteLine($"Complete seed database. plan {count}, completed {completed}, duration {sw.Elapsed.TotalSeconds}sec");
        }
    }
}
