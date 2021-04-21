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

        public DbSet<Condition> Conditions { get; set; }
    }

    /// <summary>
    /// MigrationOperationGenerator for Timescaledb.
    /// TimescaleDb require run `SELECT create_hypertable('TABLE', 'time')` to convert table to timescaledb.
    /// This MigrationOperationGenerator will automatically generate create_hypertable query when table create migration triggered.
    /// </summary>
    public class TimescaledbMigrationOperationGenerator : CSharpMigrationOperationGenerator
    {
        private readonly Dictionary<string, (string table, string key)> hyperTables;

        public TimescaledbMigrationOperationGenerator(CSharpMigrationOperationGeneratorDependencies dependencies) : base(dependencies)
        {
            hyperTables = Assembly.GetExecutingAssembly().GetTypes()
                .Where(x => x.GetInterfaces().Contains(typeof(IHyperTable)))
                .Select(x => Activator.CreateInstance(x) as IHyperTable)
                .Select(x => x.GetHyperTableKey())
                .ToDictionary(kv => kv.tableName, kv => kv);
        }
        protected override void Generate(CreateTableOperation operation, IndentedStringBuilder builder)
        {
            base.Generate(operation, builder);

            if (hyperTables.TryGetValue(operation.Name, out var keys))
            {
                builder.Append(";").AppendLine();
                GenerateHyperTable(keys.table, keys.key, builder);
            }
        }

        /// <summary>
        /// Auto execute create_hypertable query when Create Table executed.
        /// generated sample: migrationBuilder.Sql("SELECT create_hypertable('TABLE', 'time')");
        /// </summary>
        /// <param name="table"></param>
        /// <param name="key"></param>
        /// <param name="builder"></param>
        private void GenerateHyperTable(string table, string key, IndentedStringBuilder builder)
        {
            Console.WriteLine($"Creating hypertable migration query for table. table {table}, key {key}");
            builder.Append(@$"migrationBuilder.Sql(""SELECT create_hypertable('{table}', '{key}')"")");
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

    public static class IDbConnectionExtensions
    {
        public static async Task SeedAsync(this IDbConnection connection, IDbTransaction transaction)
        {
            var initData = Condition.GenerateRandomOfficeData(new DateTime(2021, 1, 1, 0, 0, 0), 10000);
            var rows = await Condition.InsertBulkAsync(connection, transaction, initData);
            Console.WriteLine(rows);

            var initData2 = Condition.GenerateRandomHomeData(new DateTime(2021, 1, 1, 0, 0, 0), 10000);
            var rows2 = await Condition.InsertBulkAsync(connection, transaction, initData2);
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

    public static class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Buffer<T>(this IEnumerable<T> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (count <= 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            IEnumerable<IEnumerable<T>> BufferImpl()
            {
                using (var enumerator = source.GetEnumerator())
                {
                    var list = new List<T>(count);
                    while (enumerator.MoveNext())
                    {
                        list.Add(enumerator.Current);
                        if (list.Count == count)
                        {
                            yield return list;
                            list = new List<T>(count);
                        }
                    }
                    if (list.Count != 0)
                        yield return list;
                }
            }
            return BufferImpl();
        }
    }

    public static class AttributeHelper
    {
        public static string GetTableName<T>()
        {
            return ((TableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute))).Name;
        }

        public static string[] GetColumns<T>()
        {
            var props = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);
            var columns = props.Select(x => ((ColumnAttribute)Attribute.GetCustomAttribute(x, typeof(ColumnAttribute))).Name).ToArray();
            return columns;
        }
    }
}