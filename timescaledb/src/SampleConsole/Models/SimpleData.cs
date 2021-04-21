using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleConsole.Models
{
    [Keyless]
    [Table("simpledata")]
    public class SimpleData : IHyperTable
    {
        private static string TableName = AttributeHelper.GetTableName<SimpleData>();
        private static string[] Columns = AttributeHelper.GetColumns<SimpleData>();
        private static string ColumnNames = string.Join(",", Columns);

        [Column("id")]
        public int Id { get; init; }
        [Column("temperature")]
        public double Temperature { get; init; }
        [Column("humidity")]
        public double Humidity { get; init; }
        [Column("name")]
        public string Name { get; init; }

        public (string tableName, string columnName) GetHyperTableKey() => (TableName, Columns.FirstOrDefault(x => string.Equals(x, nameof(Id), StringComparison.OrdinalIgnoreCase)));

        public static async Task<ulong> CopyAsync(NpgsqlConnection connection, IEnumerable<SimpleData> values, CancellationToken ct)
        {
            using var writer = connection.BeginBinaryImport($"COPY {TableName} ({ColumnNames}) FROM STDIN (FORMAT BINARY)");
            foreach (var value in values)
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(value.Id, ct);
                await writer.WriteAsync(value.Temperature, ct);
                await writer.WriteAsync(value.Humidity, ct);
                await writer.WriteAsync(value.Name, ct);
            }
            var rows = await writer.CompleteAsync(ct);
            return rows;
        }

        public static SimpleData[] GenerateSameData(int key, int dataCount)
        {
            var name = "office";
            double temp = 20;
            double hum = 50;
            var data = Enumerable.Range(1, dataCount).Select(x => new SimpleData
            {
                Id = key,
                Temperature = temp,
                Humidity = hum,
                Name = name,
            })
            .ToArray();
            return data;
        }
    }
}
