using ConsoleAppFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleConsole.Data;
using SampleConsole.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using ZLogger;

namespace SampleConsole
{
    class Program
    {
        static async Task Main(string[] args) => await Host.CreateDefaultBuilder()
                .ConfigureServices((hostContext, services) => services.AddTimeScaleDbConnection(hostContext.Configuration))
                .ConfigureServices((hostContext, services) => services.AddTimeScaleDbContext(hostContext.Configuration))
                .ConfigureLogging((context, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddZLoggerConsole(true);
                })
                .RunConsoleAppFrameworkAsync(args);
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
            using var con = _connection.Create();
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await con.OpenAsync(Context.CancellationToken);
                await Condition.InsertBulkAsync(con, new[] { 
                    new Condition
                    {
                        Location = location,
                        Temperature = temperature,
                        Humidity = humidity,
                        Time = time,
                    } 
                });
                transaction.Complete();
            }

            Console.WriteLine("test read from database");
            var conditions = await Condition.BetweenAsync(con, time, time);
            foreach (var condition in conditions)
            {
                Console.WriteLine($"{condition.Time}, {condition.Location}, {condition.Temperature}, {condition.Humidity}");
            }
        }

        [Command("keep")]
        public async Task Keep()
        {
            Console.WriteLine($"keep inserting data");
            using var con = _connection.Create();
            await con.OpenAsync(Context.CancellationToken);

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
                using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    await Condition.InsertBulkAsync(con, data);
                    transaction.Complete();
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
            using var con = _connection.Create();
            using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                await con.OpenAsync(Context.CancellationToken);
                await Condition.InsertBulkAsync(con, data);
                transaction.Complete();
            }

            Console.WriteLine("read from database");
            var conditions = await Condition.BetweenAsync(con, time.AddSeconds(-count), time);
            foreach (var condition in conditions)
            {
                Console.WriteLine($"{condition.Time}, {condition.Location}, {condition.Temperature}, {condition.Humidity}");
            }
        }

        [Command("migrate")]
        public async Task Migrate()
        {
            await _dbContext.Database.MigrateAsync(Context.CancellationToken);
            await Seed();
        }

        [Command("seed")]
        public async Task Seed()
        {
            Console.WriteLine("seeding database.");
            using var con = _connection.Create();
            await con.OpenAsync(Context.CancellationToken);
            using (var transaction = await con.BeginTransactionAsync(Context.CancellationToken))
            {
                await con.SeedAsync();
                await transaction.CommitAsync();
            }
            Console.WriteLine("seeding complete.");
        }
    }
}
