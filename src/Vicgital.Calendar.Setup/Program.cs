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
    var quartersResult = await quarterComponent.CreateQuartersByYear(year);
    if (!quartersResult.IsSuccess)
    {
        Console.WriteLine($"Failed to create quarters for year {year}: {quartersResult.FirstError!.Message}");
        continue;
    }

    foreach (var quarter in quartersResult.Value)
    {
        Console.WriteLine("----------- Quarter -----------");
        Console.WriteLine($"Quarter {quarter.Code}: {quarter.StartDate} - {quarter.EndDate}");
        Console.WriteLine($"Creating weeks for quarter {quarter.Code}");

        var weeksResult = await weekComponent.CreateWeeksByQuarter(quarter.Code);
        if (!weeksResult.IsSuccess)
        {
            Console.WriteLine($"Failed to create weeks for quarter {quarter.Code}: {weeksResult.FirstError!.Message}");
            continue;
        }

        foreach (var week in weeksResult.Value)
        {
            Console.WriteLine($"Week {week.Code}: {week.StartDate} - {week.EndDate}");
        }
    }
}










