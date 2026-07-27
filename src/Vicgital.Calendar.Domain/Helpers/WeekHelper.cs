using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Domain.Helpers
{
    public static class WeekHelper
    {
        public static IReadOnlyList<Week> BuildWeeksByQuarter(Quarter quarter)
        {
            if (quarter.StartDate.DayOfWeek != DayOfWeek.Monday)
                throw new ArgumentException("Quarter start date must be a Monday.");

            var currentStartDate = quarter.StartDate;

            var weeks = new List<Week>();
            var endDate = quarter.EndDate;
            var index = 1;
            while (currentStartDate <= endDate)
            {
                var currentEndDate = currentStartDate.AddDays(6);
                if (currentEndDate > endDate)
                {
                    currentEndDate = endDate;
                }

                weeks.Add(new Week
                {
                    QuarterId = quarter.Id,
                    Code = $"{quarter.Code}.W{index++}",
                    StartDate = currentStartDate,
                    EndDate = currentEndDate
                });
                currentStartDate = currentEndDate.AddDays(1);
            }

            if (currentStartDate.AddDays(-1) != quarter.EndDate)
                throw new ArgumentException("Generated weeks do not cover the entire quarter.");


            return weeks;
        }
    }
}
