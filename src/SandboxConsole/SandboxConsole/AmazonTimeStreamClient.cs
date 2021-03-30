using Amazon;
using Amazon.TimestreamQuery;
using Amazon.TimestreamQuery.Model;
using Amazon.TimestreamWrite;
using Amazon.TimestreamWrite.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SandboxConsole
{
    public class AmazonTimeStreamClient : IDisposable
    {
        private readonly string _database;
        private readonly AmazonTimestreamWriteClient _writer;
        private readonly AmazonTimestreamQueryClient _reader;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true
        };

        public AmazonTimeStreamClient(string database, string region)
        {
            _database = database;
            _writer = new AmazonTimestreamWriteClient(new AmazonTimestreamWriteConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region),
            });
            _reader = new AmazonTimestreamQueryClient(new AmazonTimestreamQueryConfig
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(region),
            });
        }

        public void Dispose()
        {
            _writer.Dispose();
        }

        public async Task WriteRecordsAsync(string table, RecordData[] records, CancellationToken ct = default)
        {
            await _writer.WriteRecordsAsync(new WriteRecordsRequest
            {
                DatabaseName = _database,
                TableName = table,
                Records = records.Select(x => new Record
                {
                    Time = x.Time.ToUnixTimeMilliseconds().ToString(),
                    TimeUnit = TimeUnit.MILLISECONDS,
                    Dimensions = x.Metadata.Select(m => new Dimension
                    {
                        DimensionValueType = DimensionValueType.VARCHAR,
                        Name = m.Key,
                        Value = m.Value,
                    }).ToList(),
                    MeasureName = x.Name,
                    MeasureValue = x.Value.ToString(),
                    MeasureValueType = MeasureValueType.DOUBLE,
                })
                .ToList(),
            }, ct);
        }

        public async Task ReadRecordsAsync(string query, CancellationToken ct = default)
        {
            var request = new QueryRequest
            {
                QueryString = query,
            };
            var response = await _reader.QueryAsync(request);
            while (true)
            {
                ParseQueryResult(response);
                if (response.NextToken == null)
                    break;

                request.NextToken = response.NextToken;
                response = await _reader.QueryAsync(request);
            }
        }

        private void ParseQueryResult(QueryResponse response)
        {
            var columnInfo = response.ColumnInfo;
            var columnInfoStrings = columnInfo.ConvertAll(x => JsonSerializer.Serialize(x, _jsonSerializerOptions));
            var rows = response.Rows;

            var queryStatus = response.QueryStatus;
            Console.WriteLine("Current Query status:" + JsonSerializer.Serialize(queryStatus, _jsonSerializerOptions));
            Console.WriteLine("Metadata:" + string.Join(",", columnInfoStrings));
            Console.WriteLine("Data:");

            foreach (Row row in rows)
            {
                Console.WriteLine(ParseRow(columnInfo, row));
            }
        }

        private string ParseRow(IList<ColumnInfo> columnInfo, Row row)
        {
            var data = row.Data;
            var rowOutput = new List<string>();
            for (int j = 0; j < data.Count; j++)
            {
                var info = columnInfo[j];
                var datum = data[j];
                rowOutput.Add(ParseDatum(info, datum));
            }
            return $"{{{string.Join(",", rowOutput)}}}";
        }

        private string ParseDatum(ColumnInfo info, Datum datum)
        {
            if (datum.NullValue)
            {
                return $"{info.Name}=NULL";
            }

            var columnType = info.Type;
            if (columnType.TimeSeriesMeasureValueColumnInfo != null)
            {
                return ParseTimeSeries(info, datum);
            }
            else if (columnType.ArrayColumnInfo != null)
            {
                var arrayValues = datum.ArrayValue;
                return $"{info.Name}={ParseArray(info.Type.ArrayColumnInfo, arrayValues)}";
            }
            else if (columnType.RowColumnInfo != null && columnType.RowColumnInfo.Count > 0)
            {
                var rowColumnInfo = info.Type.RowColumnInfo;
                var rowValue = datum.RowValue;
                return ParseRow(rowColumnInfo, rowValue);
            }
            else
            {
                return ParseScalarType(info, datum);
            }
        }

        private string ParseTimeSeries(ColumnInfo info, Datum datum)
        {
            var timeseriesString = datum.TimeSeriesValue
                .Select(value => $"{{time={value.Time}, value={ParseDatum(info.Type.TimeSeriesMeasureValueColumnInfo, value.Value)}}}")
                .Aggregate((current, next) => current + "," + next);

            return $"[{timeseriesString}]";
        }

        private string ParseScalarType(ColumnInfo info, Datum datum)
        {
            return ParseColumnName(info) + datum.ScalarValue;
        }

        private string ParseColumnName(ColumnInfo info)
        {
            return info.Name == null ? "" : (info.Name + "=");
        }

        private string ParseArray(ColumnInfo arrayColumnInfo, List<Datum> arrayValues)
        {
            return $"[{arrayValues.Select(value => ParseDatum(arrayColumnInfo, value)).Aggregate((current, next) => current + "," + next)}]";
        }
    }
}
