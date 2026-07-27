using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Helpers
{
    public static class Mapper
    {
        public static QuarterModel ToProto(this Quarter quarter) => new()
        {
            Id = quarter.Id,
            Code = quarter.Code,
            StartDate = quarter.StartDate.ToString("MM/dd/yyyy"),
            EndDate = quarter.EndDate.ToString("MM/dd/yyyy")
        };

        public static WeekModel ToProto(this Week week) => new()
        {
            Id = week.Id,
            Code = week.Code,
            StartDate = week.StartDate.ToString("MM/dd/yyyy"),
            EndDate = week.EndDate.ToString("MM/dd/yyyy")
        };

        public static FortnightModel ToProto(this Fortnight fortnight) => new()
        {
            Id = fortnight.Id,
            Code = fortnight.Code,
            StartDate = fortnight.StartDate.ToString("MM/dd/yyyy"),
            EndDate = fortnight.EndDate.ToString("MM/dd/yyyy")
        };
    }
}
