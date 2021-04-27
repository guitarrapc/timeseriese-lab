using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SampleConsole.Data;
using SampleConsole.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace SampleConsole.Data
{
    public class TimeScaleDbConnection
    {
        public IConfiguration _config;

        public TimeScaleDbConnection(IConfiguration config)
        {
            _config = config;
        }

        public NpgsqlConnection Create()
        {
            return new NpgsqlConnection(_config.GetConnectionString("TimescaleDbContext"));
        }
    }

    public class TimescaleDbContext : DbContext
    {
        public TimescaleDbContext(DbContextOptions<TimescaleDbContext> options) : base(options)
        {
        }

        public DbSet<Condition> Conditions { get; set; }
        public DbSet<SimpleData> SimpleData { get; set; }
        public DbSet<SimpleSmallData> SimpleSmallDatas { get; set; }
    }

    /// <summary>
    /// MigrationOperationGenerator for Timescaledb.
    /// TimescaleDb require run `SELECT create_hypertable('TABLE', 'time')` to convert table to timescaledb.
    /// This MigrationOperationGenerator will automatically generate create_hypertable query when table create migration triggered.
    /// </summary>
    public class TimescaledbMigrationOperationGenerator : CSharpMigrationOperationGenerator
    {
        private readonly Dictionary<string, (string table, string key, HyperTableAttribute attribute)> hyperTables;

        public TimescaledbMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies) : base(dependencies)
        {
            hyperTables = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IHyperTable)))
                .Select(x => Activator.CreateInstance(x) as IHyperTable)
                .Select(x => x.GetHyperTableInfo())
                .ToDictionary(kv => kv.tableName, kv => kv);
        }
        protected override void Generate(CreateTableOperation operation, IndentedStringBuilder builder)
        {
            base.Generate(operation, builder);

            if (hyperTables.TryGetValue(operation.Name, out var value))
            {
                builder.Append(";").AppendLine();
                GenerateHyperTable(value.table, value.key, value.attribute, builder);
            }
        }

        /// <summary>
        /// Auto execute create_hypertable query when Create Table executed.
        /// generated sample: migrationBuilder.Sql("SELECT create_hypertable('TABLE', 'time')");
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="builder"></param>
        private void GenerateHyperTable(string table, string key, HyperTableAttribute attribute, IndentedStringBuilder builder)
        {
            Console.WriteLine($"Creating hypertable migration query for table. table {table}, key {key}");
            var chunkTimeInterval = attribute.ChunkTimeInterval != 0 ? $", chunk_time_interval => {attribute.ChunkTimeInterval}" : "";
            builder.Append(@$"migrationBuilder.Sql(""SELECT create_hypertable('{table}', '{key}'{chunkTimeInterval})"")");
        }
    }
    /// <summary>
    /// Automatically discover via EF Core Reflection
    /// </summary>
    public class TimescaledbDesignTimeServices : IDesignTimeServices
    {
        public void ConfigureDesignTimeServices(IServiceCollection services)
            => services.AddSingleton<ICSharpMigrationOperationGenerator, TimescaledbMigrationOperationGenerator>();
    }

    /// <summary>
    /// for dotnet-ef migrations config initialization.
    /// </summary>
    public class TimescaleDbContextFactory : IDesignTimeDbContextFactory<TimescaleDbContext>
    {
        private IConfiguration _config;
        public TimescaleDbContextFactory()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddJsonFile("appsettings.json");
            _config = configBuilder.Build();
        }
        public TimescaleDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<TimescaleDbContext>();
            optionsBuilder.UseNpgsql(_config.GetConnectionString("TimescaleDbContext"));
            return new TimescaleDbContext(optionsBuilder.Options);
        }
    }
}

namespace SampleConsole
{
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// For EF Query
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddTimeScaleDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<TimescaleDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("TimescaleDbContext"));
            });

            return services;
        }

        /// <summary>
        /// For Dapper Query
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static IServiceCollection AddTimeScaleDbConnection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<TimeScaleDbConnection>(new TimeScaleDbConnection(configuration));
            return services;
        }
    }
}