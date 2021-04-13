using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SampleConsole.Models
{
    [Keyless]
    public class Condition
    {
        public DateTime Time { get; init; }
        public string Location { get; init; }
        public double? Temperature { get; set; }
        public double? Humidity { get; set; }

        public static async Task<int> InsertBulkAsync(NpgsqlConnection connection, Condition[] values)
        {
            var rows = await connection.ExecuteAsync(
                @$"INSERT INTO ""Conditions"" (
                    ""{nameof(Condition.Time)}"",
                    ""{nameof(Condition.Location)}"",
                    ""{nameof(Condition.Temperature)}"",
                    ""{nameof(Condition.Humidity)}""
                )
                VALUES (
                    @Time, @Location, @Temperature, @Humidity
                );", values);
            return rows;
        }
        public static Condition[] InitialOfficeData(int dataCount)
        {
            var time = new DateTime(2021, 1, 1, 0, 0, 0);
            var location = "オフィス";
            var temp = 10.0;
            var hum = 30.0;
            var conditions = Enumerable.Range(1, dataCount).Select(x => new Condition
            {
                Time = time.AddSeconds(x),
                Location = location,
                Temperature = temp + Math.Min(Math.Cos(x % 1000.0 * x) - Math.Sin(x % 100), 10),
                Humidity = hum + Math.Max(Math.Min(Math.Tan(x % 1000 * x) / Math.Sin(x % 1000), 70), -30),
            })
            .ToArray();
            return conditions;
        }

        public static Condition[] InitialHomeData(int dataCount)
        {
            var time = new DateTime(2021, 1, 1, 0, 0, 0);
            var location = "家";
            var temp = 5.0;
            var hum = 40.0;
            var conditions = Enumerable.Range(1, dataCount).Select(x => new Condition
            {
                Time = time.AddSeconds(x),
                Location = location,
                Temperature = temp + Math.Min(Math.Cos(x % 5000.0 * x) - Math.Sin(x % 200), 20),
                Humidity = hum + Math.Max(Math.Min(Math.Tan(x % 5000 * x) / Math.Sin(x % 2000), 60), -40),
            })
            .ToArray();
            return conditions;
        }
    }
}
