using Amazon.CDK;
using Amazon.CDK.AWS.KMS;
using Amazon.CDK.AWS.Timestream;
using System.Collections.Generic;
using System.Text.Json;

namespace Timestream
{
    public class TimestreamStack : Stack
    {
        internal TimestreamStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
        {
            var key = new Key(this, "timestream-sample", new KeyProps
            {
                EnableKeyRotation = true,
            });
            var database = new CfnDatabase(this, "sampledb", new CfnDatabaseProps
            {
                DatabaseName = "sampledb",
                KmsKeyId = key.KeyId,
            });
            var table = new CfnTable(this, "temp_humidities", new CfnTableProps
            {
                TableName = "temp_humidities",
                DatabaseName = database.DatabaseName,
                RetentionProperties = new Dictionary<string, string>()
                {
                    { "MemoryStoreRetentionPeriodInHours", "24" },
                    { "MagneticStoreRetentionPeriodInDays", "7" }
                },
            });
            table.AddDependsOn(database);
        }
    }
}
