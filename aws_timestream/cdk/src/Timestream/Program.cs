using Amazon.CDK;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Timestream
{
    sealed class Program
    {
        public static void Main(string[] args)
        {
            var app = new App();
            new TimestreamStack(app, "TimestreamStack", new StackProps
            {
                Env = new Amazon.CDK.Environment()
                {
                    Region = "us-west-2", // oregon
                },
            });
            app.Synth();
        }
    }
}
