using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleConsole.Models
{
    [HyperTable()]
    [Keyless]
    [Table("conditions")]
    public class Condition : IHyperTable
    {
        private static string tableName = AttributeHelper.GetTableName<Condition>();
        private static string[] columns = AttributeHelper.GetColumns<Condition>();
        private static string columnNames = string.Join(",", columns);
        private static HyperTableAttribute hyperTableAttribute = AttributeHelper.GetHypertableAttribute<Condition>();

        [Column("time")]
        public DateTime Time { get; init; }
        [Column("location")]
        public string Location { get; init; }
        [Column("temperature")]
        public double? Temperature { get; set; }
        [Column("humidity")]
        public double? Humidity { get; set; }

        public bool IsHyperTable() => hyperTableAttribute != null;
        public (string tableName, string columnName, HyperTableAttribute attribute) GetHyperTableInfo()
        {
            var column = columns.FirstOrDefault(x => string.Equals(x, nameof(Time), StringComparison.OrdinalIgnoreCase));
            return (tableName, column, hyperTableAttribute);
        }

        public static async Task<Condition[]> BetweenAsync(IDbConnection connection, DateTime from, DateTime to)
        {
            var results = await connection.QueryAsync<Condition>(
                $@"SELECT * FROM {tableName} WHERE {nameof(Time)} between @from AND @to",
                new { from, to });
            return results.ToArray();
        }

        public static async Task<int> InsertBulkAsync(IDbConnection connection, IDbTransaction transaction, IEnumerable<Condition> values, int timeoutSec = 60)
        {
            var rows = await connection.ExecuteAsync(
                @$"INSERT INTO {tableName} ({columnNames}) VALUES (@Time, @Location, @Temperature, @Humidity);"
                , values, transaction, timeoutSec);
            return rows;
        }

        public static async Task<ulong> CopyAsync(NpgsqlConnection connection, IEnumerable<Condition> values, CancellationToken ct)
        {
            // COPY not support Nullable<T>
            // https://github.com/npgsql/npgsql/issues/1965
            using var writer = connection.BeginBinaryImport($"COPY {tableName} ({columnNames}) FROM STDIN (FORMAT BINARY)");
            foreach (var value in values)
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(value.Time, ct);
                await writer.WriteAsync(value.Location, ct);
                await writer.WriteAsync(value.Temperature.Value, ct);
                await writer.WriteAsync(value.Humidity.Value, ct);
            }
            var rows = await writer.CompleteAsync(ct);
            return rows;
        }

        public static Condition[] GenerateRandomOfficeData(DateTime time, int dataCount)
        {
            var random = new Random();
            var location = "オフィス";
            double temp = random.Next(10, 15);
            double hum = random.Next(30, 50);
            var conditions = Enumerable.Range(1, dataCount).Select(x => new Condition
            {
                Time = time.AddSeconds(x),
                Location = location,
                Temperature = temp + Math.Min(Math.Cos(x % 1000.0 * x) - Math.Sin(x % 100), 30 - temp),
                Humidity = hum + Math.Max(Math.Min(Math.Tan(x % 1000 * x) / Math.Sin(x % 1000), 100 - hum), 0 - hum),
            })
            .ToArray();
            return conditions;
        }

        public static Condition[] GenerateRandomHomeData(DateTime time, int dataCount)
        {
            var random = new Random();
            var location = "家";
            double temp = random.Next(5, 10);
            double hum = random.Next(40, 60);
            var conditions = Enumerable.Range(1, dataCount).Select(x => new Condition
            {
                Time = time.AddSeconds(x),
                Location = location,
                Temperature = temp + Math.Min(Math.Cos(x % 5000.0 * x) - Math.Sin(x % 200), 20 - temp),
                Humidity = hum + Math.Max(Math.Min(Math.Tan(x % 5000 * x) / Math.Sin(x % 2000), 100 - hum), 0 - hum),
            })
            .ToArray();
            return conditions;
        }
    }
}
