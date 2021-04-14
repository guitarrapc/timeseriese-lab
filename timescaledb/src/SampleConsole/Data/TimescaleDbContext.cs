using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;
using SampleConsole.Data;
using SampleConsole.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

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

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<IMigrationsSqlGenerator, TimescaledbMigrationSqlGenerator>();
        }

        public DbSet<Condition> Conditions { get; set; }
    }

    /// <summary>
    /// MigrationSqlGenerator for Timescaledb.
    /// TimescaleDb require run `SELECT create_hypertable('TABLE', 'time')` to map table to chunk.
    /// This MigrationSqlGenerator will automatically run create_hypertable query when table create migration triggered.
    /// </summary>
    internal class TimescaledbMigrationSqlGenerator: NpgsqlMigrationsSqlGenerator
    {
        private readonly Dictionary<string, (string table, string key)> hyperTables;

        public TimescaledbMigrationSqlGenerator(
            MigrationsSqlGeneratorDependencies dependencies,
#pragma warning disable EF1001 // Internal EF Core API usage.
            INpgsqlOptions npgsqlOptions)
#pragma warning restore EF1001 // Internal EF Core API usage.
            : base(dependencies, npgsqlOptions)
        {
            hyperTables = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IHyperTable)))
                .Select(x => Activator.CreateInstance(x) as IHyperTable)
                .Select(x => x.GetHyperTableKey())
                .ToDictionary(kv => kv.tableName, kv => kv);
        }

        protected override void Generate(CreateTableOperation operation, IModel model, MigrationCommandListBuilder builder, bool terminate = true)
        {
            base.Generate(operation, model, builder, terminate);

            if (hyperTables.TryGetValue(operation.Name, out var keys))
            {
                GenerateHyperTable(keys.table, keys.key, builder);
            }
        }

        /// <summary>
        /// Auto execute create_hypertable query when Create Table executed.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="builder"></param>
        private void GenerateHyperTable(string table, string key, MigrationCommandListBuilder builder)
        {
            Console.WriteLine($"Creating hypertable for table. table: {table}");
            var sqlHelper = Dependencies.SqlGenerationHelper;
            var stringMapping = Dependencies.TypeMappingSource.FindMapping(typeof(string));

            // SELECT create_hypertable('TABLE', 'time')
            builder
                .Append("SELECT create_hypertable(")
                .Append(stringMapping.GenerateSqlLiteral(table))
                .Append(",")
                .Append(stringMapping.GenerateSqlLiteral(key))
                .Append(")")
                .AppendLine(sqlHelper.StatementTerminator)
                .EndCommand();
        }
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

    public static class IDbConnectionExtensions
    {
        public static async Task SeedAsync(this IDbConnection connection)
        {
            var initData = Condition.GenerateRandomOfficeData(new DateTime(2021, 1, 1, 0, 0, 0), 10000);
            var rows = await Condition.InsertBulkAsync(connection, initData);
            Console.WriteLine(rows);

            var initData2 = Condition.GenerateRandomHomeData(new DateTime(2021, 1, 1, 0, 0, 0), 10000);
            var rows2 = await Condition.InsertBulkAsync(connection, initData2);
            Console.WriteLine(rows2);
        }
    }

    public static class AttributeUtilities
    {
        public static string GetColumnName(Type type, string name)
        {
            var attribute = type.GetProperty(name).GetCustomAttribute<ColumnAttribute>();
            return attribute.Name;
        }
    }
}