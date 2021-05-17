using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SampleConsole.Models
{
    [DistributedHypertable(tableName, "time", "sensor_id")]
    [Table(tableName)]
    [Keyless]
    [Index(nameof(Time), Name = "sensor_data_time_idx")]
    // auto gen: [Index(nameof(SensorId), nameof(Time), Name = "sensor_data_sensor_id_time_idx")]
    public class SensorData
    {
        private const string tableName = "sensor_data";
        private static string[] columns = AttributeEx.GetColumns<SensorData>();

        [Column("time")]
        public DateTime Time { get; init; }
        [Column("sensor_id")]
        public int? SensorId { get; init; }
        [Column("temperature")]
        public double? Temperature { get; set; }
        [Column("cpu")]
        public double? Cpu { get; set; }

        public static async Task<ulong> CopyAsync(NpgsqlConnection connection, IEnumerable<SensorData> values, CancellationToken ct)
        {
            using var writer = connection.BeginBinaryImport($"COPY {tableName} ({string.Join(",", columns)}) FROM STDIN (FORMAT BINARY)");
            foreach (var value in values)
            {
                await writer.StartRowAsync(ct);
                await writer.WriteAsync(value.Time, ct);
                await writer.WriteAsync(value.SensorId, ct);
                await writer.WriteAsync(value.Temperature, ct);
                await writer.WriteAsync(value.Cpu, ct);
            }
            var rows = await writer.CompleteAsync(ct);
            return rows;
        }

        public static async Task<int> InsertSampleDataAsync(IDbConnection connection, IDbTransaction transaction, int timeOutSec = 300)
        {
            var sql = $@"INSERT INTO {tableName}
SELECT
	time,
	sensor_id,
	random() AS cpu,
	random() * 100 AS temperature
FROM
	generate_series(now() - interval '6 months', now(), interval '30 second') AS g1(time),
	generate_series(1, 10, 1) AS g2(sensor_id);
";
            var rows = await connection.ExecuteAsync(sql, transaction: transaction, commandTimeout: timeOutSec);
            return rows;
        }
    }
}
