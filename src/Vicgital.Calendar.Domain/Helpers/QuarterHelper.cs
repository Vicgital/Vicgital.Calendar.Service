using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Domain.Helpers
{
    public static class QuarterHelper
    {
        public static List<Quarter> BuildQuartersByYear(int year)
        {
            DateOnly firstDayOfTheYear = new(year, 1, 1);
            DateOnly startDate = firstDayOfTheYear;
            bool startDateSet = false;

            // set the start date to the first quarter of the year, which is the first Monday of the year
            while (!startDateSet)
            {
                if (firstDayOfTheYear.DayOfWeek == DayOfWeek.Monday)
                {
                    startDate = firstDayOfTheYear;
                    startDateSet = true;
                }
                else
                    firstDayOfTheYear = firstDayOfTheYear.AddDays(1);
            }

            // now we have the start date of the first quarter, we can create the 4 quarters of the year            
            List<Quarter> quarters = [];
            for (int i = 0; i < 4; i++)
            {

                Quarter quarter = new()
                {
                    Code = $"{year}.Q{i + 1}",
                    StartDate = startDate,
                    EndDate = startDate.AddDays(83) // (7 Days * 12 Weeks) - 1 (startdate) = 83 Days 
                };
                quarters.Add(quarter);
                // set the start date for the next quarter to be the day after the end date of the current quarter
                startDate = quarter.EndDate.AddDays(1);
            }

            // now we have the 4 quarters of the year, add the final year quarter, they're the last remaining weeks of the year

            Quarter finalQuarter = new()
            {
                Code = $"{year}.QF",
                StartDate = startDate,
                EndDate = DetermineFinalQuarterEndDate(startDate)
            };

            quarters.Add(finalQuarter);

            return quarters;
        }

        private static DateOnly DetermineFinalQuarterEndDate(DateOnly startDate)
        {
            // the final quarter end date is the first Sunday of the following year
            if (startDate.DayOfWeek != DayOfWeek.Monday)
                throw new InvalidOperationException("Start date must be a Monday.");

            if (startDate.Month != 12)
                throw new InvalidOperationException("Start date of final quarter must be in December.");

            var endDate = new DateOnly(startDate.Year, 12, 31);

            if (endDate.DayOfWeek == DayOfWeek.Sunday)
                return endDate;
            else
            {
                var setEndDate = false;
                while (!setEndDate)
                {
                    endDate = endDate.AddDays(1);
                    if (endDate.DayOfWeek == DayOfWeek.Sunday)
                        setEndDate = true;
                }
            }

            return endDate;

        }
    }
}
