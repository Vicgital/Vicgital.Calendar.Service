using FluentValidation;
using Vicgital.Calendar.Application.Components;
using Vicgital.Calendar.Application.Components.Caching;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Calendar.Infrastructure.Repositories;
using Vicgital.Calendar.Service.Definition;
using Vicgital.Calendar.Service.Validators;
using Vicgital.Core.Caching.Abstractions;
using Vicgital.Core.Caching.InMemory.Extensions;
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

            // Add Caching
            services.AddVicgitalInMemoryCaching();

            // Add Components (decorated with a caching layer, since quarters/weeks are effectively immutable reference data)
            services.AddScoped<QuarterComponent>();
            services.AddScoped<IQuarterComponent>(sp => new CachedQuarterComponent(sp.GetRequiredService<QuarterComponent>(), sp.GetRequiredService<ICacheService>()));

            services.AddScoped<WeekComponent>();
            services.AddScoped<IWeekComponent>(sp => new CachedWeekComponent(sp.GetRequiredService<WeekComponent>(), sp.GetRequiredService<ICacheService>()));

            services.AddScoped<FortnightComponent>();
            services.AddScoped<IFortnightComponent>(sp => new CachedFortnightComponent(sp.GetRequiredService<FortnightComponent>(), sp.GetRequiredService<ICacheService>()));

            // Add Validators
            services.AddScoped<IValidator<QuarterRequest>, QuarterRequestValidator>();
            services.AddScoped<IValidator<FortnightRequest>, FortnightRequestValidator>();
            services.AddScoped<IValidator<YearRequest>, YearRequestValidator>();
            services.AddScoped<IValidator<DateRequest>, DateRequestValidator>();
            services.AddScoped<IValidator<WeekRequest>, WeekRequestValidator>();
            services.AddScoped<IValidator<CreateWeeksByQuarterRequest>, CreateWeeksByQuarterRequestValidator>();
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
