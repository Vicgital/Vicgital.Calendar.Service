using Google.Type;
using Grpc.Core;
using Grpc.Net.Client;
using Vicgital.Calendar.Service.Definition;
using Vicgital.Core.Configuration;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

var config = ConfigurationBuilder.BuildConfiguration();
var calendarServiceUrl = config["CalendarService:Url"] ?? throw new InvalidOperationException("CalendarService:Url configuration value is not set.");

using var channel = GrpcChannel.ForAddress(calendarServiceUrl);
var client = new Calendar.CalendarClient(channel);

var yearsToCreate = new List<int> { 2032 };

foreach (var year in yearsToCreate)
{
    Console.WriteLine($"Creating quarters for year {year}");

    QuartersReply quartersReply;
    try
    {
        quartersReply = await client.CreateQuartersByYearAsync(new YearRequest { Year = year });
    }
    catch (RpcException ex)
    {
        Console.WriteLine($"Failed to create quarters for year {year}: {ex.Status.StatusCode} - {ex.Status.Detail}");
        continue;
    }

    foreach (var quarter in quartersReply.Quarters)
    {
        Console.WriteLine("----------- Quarter -----------");
        Console.WriteLine($"Quarter {quarter.Code}: {FormatDate(quarter.StartDate)} - {FormatDate(quarter.EndDate)}");
        Console.WriteLine($"Creating weeks for quarter {quarter.Code}");

        WeeksReply weeksReply;
        try
        {
            weeksReply = await client.CreateWeeksByQuarterAsync(new CreateWeeksByQuarterRequest { QuarterCode = quarter.Code });
        }
        catch (RpcException ex)
        {
            Console.WriteLine($"Failed to create weeks for quarter {quarter.Code}: {ex.Status.StatusCode} - {ex.Status.Detail}");
            continue;
        }

        foreach (var week in weeksReply.Weeks)
        {
            Console.WriteLine($"Week {week.Code}: {FormatDate(week.StartDate)} - {FormatDate(week.EndDate)}");
        }
    }
}

Console.ReadLine();

static string FormatDate(Date date) => $"{date.Year:D4}-{date.Month:D2}-{date.Day:D2}";
