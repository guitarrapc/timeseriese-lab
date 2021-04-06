using System;
using System.Threading;
using System.Threading.Tasks;

namespace SandboxConsole
{
    public readonly struct RecordMetadata
    {
        public readonly string Key;
        public readonly string Value;
        public RecordMetadata(string key, string value)
        {
            Key = key;
            Value = value;
        }
    }
    public readonly struct RecordData
    {
        public readonly RecordMetadata[] Metadata;
        public readonly DateTimeOffset Time;
        public readonly string Name;
        public readonly double Value;

        public RecordData(RecordMetadata[] metadata, DateTimeOffset time, string name, double value)
        {
            Metadata = metadata;
            Time = time;
            Name = name;
            Value = value;
        }
    }

    public interface ITimeStreamClient
    {
        Task AddAsync(string table, RecordData item, CancellationToken ct);
        Task AddAsync(string table, RecordData[] items, CancellationToken ct);

        Task ReadAsync(string query, CancellationToken ct);
    }

    public class TimeStreamClient : ITimeStreamClient, IDisposable
    {
        private readonly AmazonTimeStreamClient _client;
        public TimeStreamClient(string database, string region = "us-west-2")
        {
            _client = new AmazonTimeStreamClient(database, region);
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task AddAsync(string table, RecordData item, CancellationToken ct = default)
        {
            await AddAsync(table, new[] { item }, ct);
        }
        public async Task AddAsync(string table, RecordData[] items, CancellationToken ct = default)
        {
            await _client.WriteRecordsAsync(table, items, ct);
        }

        public async Task ReadAsync(string query, CancellationToken ct = default)
        {
            await _client.ReadRecordsAsync(query, ct);
        }
    }
}
