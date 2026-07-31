using Google.Type;
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
            StartDate = quarter.StartDate.ToProtoDate(),
            EndDate = quarter.EndDate.ToProtoDate()
        };

        public static WeekModel ToProto(this Week week) => new()
        {
            Id = week.Id,
            Code = week.Code,
            StartDate = week.StartDate.ToProtoDate(),
            EndDate = week.EndDate.ToProtoDate()
        };

        public static FortnightModel ToProto(this Fortnight fortnight) => new()
        {
            Id = fortnight.Id,
            Code = fortnight.Code,
            StartDate = fortnight.StartDate.ToProtoDate(),
            EndDate = fortnight.EndDate.ToProtoDate()
        };

        public static Date ToProtoDate(this DateOnly date) => new()
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day
        };

        public static DateOnly ToDateOnly(this Date date) => new(date.Year, date.Month, date.Day);
    }
}
