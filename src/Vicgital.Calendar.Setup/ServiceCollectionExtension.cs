using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vicgital.Calendar.Application.Components;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Calendar.Infrastructure.Repositories;
using Vicgital.Core.Configuration.Extensions;
using Vicgital.Core.Logging.Serilog.Configuration;
using Vicgital.Core.Logging.Serilog.Extensions;
using Vicgital.Data.Sql.Extensions;
using Vicgital.Data.Sql.Helpers;

namespace Vicgital.Calendar.Setup
{
    internal static class ServiceCollectionExtension
    {
        internal static void SetupServices(this ServiceCollection services, IConfiguration config)
        {
            services.AddAppConfiguration(config);

            // Create Logger
            var loggerConfiguration = LoggerConfigurationBuilder.BuildDefault(Serilog.Events.LogEventLevel.Information);
            var logger = loggerConfiguration.CreateLogger();
            services.AddSerilogLogging(logger);

            // Add Database
            services.AddVicgitalDataSqlDapper(GetSqlConnectionString());

            // Add Respositories
            services.AddScoped<IQuarterRepository, QuarterRepository>();
            services.AddScoped<IWeekRepository, WeekRepository>();
            services.AddScoped<IFortnightRepository, FortnightRepository>();

            // Add Components
            services.AddScoped<IQuarterComponent, QuarterComponent>();
            services.AddScoped<IWeekComponent, WeekComponent>();
            services.AddScoped<IFortnightComponent, FortnightComponent>();

        }

        private static string GetSqlConnectionString()
        {
            var connectionString = SqlDbConnectionStringHelper.GetSqlDbConnectionString(
                        Environment.GetEnvironmentVariable("SQLDB_SERVER") ?? throw new InvalidOperationException("SQLDB_SERVER environment variable is not set."),
                        Data.Sql.Enums.Databases.LifeOS,
                        "",
                        Environment.GetEnvironmentVariable("SQLDB_USERNAME") ?? throw new InvalidOperationException("SQLDB_USERNAME environment variable is not set."),
                        Environment.GetEnvironmentVariable("SQLDB_PASSWORD") ?? throw new InvalidOperationException("SQLDB_PASSWORD environment variable is not set.")
                        );
            return connectionString;
        }
    }
}
