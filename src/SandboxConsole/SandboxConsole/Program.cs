using System;
using System.Threading.Tasks;
using ConsoleAppFramework;
using Microsoft.Extensions.Hosting;

namespace SandboxConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            args = new[] { "add" };
            await Host.CreateDefaultBuilder().RunConsoleAppFrameworkAsync<SampleApp>(args);
        }            
    }

    public class SampleApp : ConsoleAppBase
    {
        [Command("add")]
        public async Task Add(string database = "sampledb", string table = "sampletable")
        {
            var random = new Random();
            var os = new []
            {
                "iPhone 12", "iPhone 11", "iPhone 10", "iPhone 9", "iPhone 8", "iPhone 7",
                "android 11", "android 10", "android 9", "android 8", "android 7",
            };
            using var client = new TimeStreamClient(database);
            for (var i = 1; i < 10000; i++)
            {
                var metadata = new[] {
                    new RecordMetadata("hostname", Environment.MachineName),
                    new RecordMetadata("os", os[(os.Length - 1) % i]),
                    new RecordMetadata("region", "日本"),
                };
                var cpu = random.Next(30, 100);
                var cpuRecord = new RecordData(metadata, DateTimeOffset.Now, "cpu_utilization", cpu);

                var memory = random.Next(1000, 1500);
                var memoryRecord = new RecordData(metadata, DateTimeOffset.Now, "memory_utilization", memory);
                var records = new[] { cpuRecord, memoryRecord };

                Console.WriteLine($"adding {records.Length} records to {database}.{table}");
                await client.AddAsync(table, records);
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        [Command("read")]
        public async Task Read(string database = "sampledb", string table = "sampletable")
        {
            using var client = new TimeStreamClient(database);
            await client.ReadAsync($@"SELECT * FROM {database}.{table} WHERE os = 'iPhone 12'");
        }
    }
}
