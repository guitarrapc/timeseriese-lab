using Dapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SampleConsole.Models
{
    [Keyless]
    [Table("conditions")]
    public class Condition : IHyperTable
    {
        private const string TABLE_NAME = "conditions";

        [Column("time")]
        public DateTime Time { get; init; }
        [Column("location")]
        public string Location { get; init; }
        [Column("temperature")]
        public double? Temperature { get; set; }
        [Column("humidity")]
        public double? Humidity { get; set; }

        public (string tableName, string columnName) GetHyperTableKey() => (TABLE_NAME, AttributeUtilities.GetColumnName(this.GetType(), nameof(Time)));

        public static async Task<Condition[]> BetweenAsync(IDbConnection connection, DateTime from, DateTime to)
        {
            var results = await connection.QueryAsync<Condition>(
                $@"SELECT * FROM {TABLE_NAME} WHERE {nameof(Time)} between @from AND @to",
                new { from, to });
            return results.ToArray();
        }

        public static async Task<int> InsertBulkAsync(IDbConnection connection, Condition[] values)
        {
            var rows = await connection.ExecuteAsync(
                @$"INSERT INTO {TABLE_NAME} (
                    {nameof(Time)},
                    {nameof(Location)},
                    {nameof(Temperature)},
                    {nameof(Humidity)}
                )
                VALUES (
                    @Time, @Location, @Temperature, @Humidity
                );", values);
            return rows;
        }
        public static Condition[] GenerateRandomOfficeData(DateTime time, int dataCount)
        {
            var random = new Random();
            var location = "オフィス";
            double temp = random.Next(10, 15);
            double hum = random.Next(30, 50);
            var conditions = Enumerable.Range(0, dataCount).Select(x => new Condition
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
            var conditions = Enumerable.Range(0, dataCount).Select(x => new Condition
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
