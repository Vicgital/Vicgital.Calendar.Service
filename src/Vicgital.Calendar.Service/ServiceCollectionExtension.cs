using FluentValidation;
using Vicgital.Calendar.Application.Components;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Calendar.Infrastructure.Repositories;
using Vicgital.Calendar.Service.Definition;
using Vicgital.Calendar.Service.Validators;
using Vicgital.Data.Sql.Extensions;
using Vicgital.Data.Sql.Helpers;

namespace Vicgital.Calendar.Service
{
    internal static class ServiceCollectionExtension
    {
        internal static void SetupServices(this IServiceCollection services)
        {
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

            // Add Validators
            services.AddScoped<IValidator<QuarterRequest>, QuarterRequestValidator>();
            services.AddScoped<IValidator<YearRequest>, YearRequestValidator>();
            services.AddScoped<IValidator<DateRequest>, DateRequestValidator>();
            services.AddScoped<IValidator<WeekRequest>, WeekRequestValidator>();
        }

        static string GetSqlConnectionString()
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
