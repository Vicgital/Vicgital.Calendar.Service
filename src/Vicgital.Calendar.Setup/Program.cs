using Microsoft.Extensions.DependencyInjection;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Setup;
using Vicgital.Core.Configuration;


var yearsToCreate = new List<int> { 2025, 2026, 2027, 2028, 2029, 2030 };

var services = new ServiceCollection();
services.SetupServices(ConfigurationBuilder.BuildConfiguration());

var serviceProvider = services.BuildServiceProvider();
var quarterComponent = serviceProvider.GetRequiredService<IQuarterComponent>();
var weekComponent = serviceProvider.GetRequiredService<IWeekComponent>();

foreach (var year in yearsToCreate)
{
    Console.WriteLine($"Creating quarters for year {year}");
    var quarters = await quarterComponent.CreateQuartersByYear(year);
    foreach (var quarter in quarters)
    {
        Console.WriteLine("----------- Quarter -----------");
        Console.WriteLine($"Quarter {quarter.Code}: {quarter.StartDate} - {quarter.EndDate}");
        Console.WriteLine($"Creating weeks for quarter {quarter.Code}");

        var weeks = await weekComponent.CreateWeeksByQuarter(quarter.Code);
        foreach (var week in weeks)
        {
            Console.WriteLine($"Week {week.Code}: {week.StartDate} - {week.EndDate}");
        }
    }
}










