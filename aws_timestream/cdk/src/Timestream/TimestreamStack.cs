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
            var key = Alias.FromAliasName(this, "timestream", "alias/aws/timestream");
            var database = new CfnDatabase(this, "sampledb", new CfnDatabaseProps
            {
                DatabaseName = "sampledb",
                KmsKeyId = key.KeyId,
            });
            var table = new CfnTable(this, "sampletable", new CfnTableProps
            {
                TableName = "sampletable",
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
