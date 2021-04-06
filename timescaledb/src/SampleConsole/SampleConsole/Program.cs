using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleConsole.Data;
using System;
using System.Threading.Tasks;
using ZLogger;

namespace SampleConsole
{
    class Program
    {
        static async Task Main(string[] args)
            => await Host.CreateDefaultBuilder()
            .ConfigureServices((hostContext, services) => services.AddTimeScaleDbConnection(hostContext.Configuration))
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
        public Runner(TimeScaleDbConnection connection)
        {
            _connection = connection;
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
