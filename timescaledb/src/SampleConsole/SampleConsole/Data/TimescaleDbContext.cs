using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SampleConsole.Data;
using SampleConsole.Models;
using System;
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
    /// for dotnet-ef migrations
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
        public static IServiceCollection AddTimeScaleDbContext(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<TimescaleDbContext>(options =>
            {
                options.UseNpgsql(configuration.GetConnectionString("TimescaleDbContext"));
            });

            return services;
        }

        public static IServiceCollection AddTimeScaleDbConnection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<TimeScaleDbConnection>(new TimeScaleDbConnection(configuration));
            return services;
        }
    }

    public static class NpgsqlConnectionExtensions
    {
        public static async Task SeedAsync(this NpgsqlConnection connection)
        {
            var initData = Condition.InitialOfficeData(10000);
            var rows = await Condition.InsertBulkAsync(connection, initData);
            Console.WriteLine(rows);

            var initData2 = Condition.InitialHomeData(10000);
            var rows2 = await Condition.InsertBulkAsync(connection, initData2);
            Console.WriteLine(rows2);
        }
    }

    public static class TimescaleDbContextExtensions
    {
        public static void InitTables(this TimescaleDbContext context)
        {
            using var transaction = context.Database.BeginTransaction();
            context.Conditions.FromSqlRaw("SELECT create_hypertable('Conditions', 'Time');");
            context.SaveChanges();
        }

        public static void Seed(this TimescaleDbContext context)
        {
            using var transaction = context.Database.BeginTransaction();
            context.Conditions.AddRange(Condition.InitialOfficeData(10000));
            context.SaveChanges();
            context.Conditions.AddRange(Condition.InitialHomeData(10000));
            context.SaveChanges();

            transaction.Commit();
        }
    }
}