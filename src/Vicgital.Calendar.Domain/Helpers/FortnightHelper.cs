using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Domain.Helpers
{
    public static class FortnightHelper
    {
        public static List<Fortnight> BuildFortnightsByYear(int year)
        {
            var result = new List<Fortnight>();
            DateOnly startDate = GetStartDate(year);

            for (int i = 1; i <= 12; i++)
            {
                Fortnight monthFortnight1 = new()
                {
                    Code = $"{year}.{(i < 10 ? $"0{i}" : i)}.F1",
                    StartDate = startDate,
                    EndDate = GetFirstFortnightEndDate(year, i),
                };
                
                Fortnight monthFortnight2 = new()
                {
                    Code = $"{year}.{(i < 10 ? $"0{i}" : i)}.F2",
                    StartDate = GetFirstFortnightEndDate(year, i).AddDays(1),
                    EndDate = GetSecondFortnightEndDate(year,i),
                };


                result.Add(monthFortnight1);
                result.Add(monthFortnight2);
                startDate = monthFortnight2.EndDate.AddDays(1);
            }

            return result;
        }

        private static DateOnly GetStartDate(int year)
        {
            DateOnly result = new(year, 1, 1);
            // if it's a monday, that means the last week of the previous week falls on a friday,
            // so make the first day the last saturday of the previous year (because the last friday of the previous year was pay day)
            if (result.DayOfWeek == DayOfWeek.Monday)
                result = result.AddDays(-2);

            // if it's a sunday, the previous year's Dec 31 is a saturday, which GetSecondFortnightEndDate pulls back
            // to the preceding friday for the same payday reason - shift the start back a day to match, or Dec 31 is left uncovered
            if (result.DayOfWeek == DayOfWeek.Sunday)
                result = result.AddDays(-1);

            return result;
        }

        private static DateOnly GetFirstFortnightEndDate(int year, int month)
        {
            DateOnly result = new(year, month, 15);
            if (result.DayOfWeek == DayOfWeek.Saturday)
                result = result.AddDays(-1);
            if (result.DayOfWeek == DayOfWeek.Sunday)
                result = result.AddDays(-2);

            return result;
        }

        private static DateOnly GetSecondFortnightEndDate(int year, int month)
        {

            int lastDay = DateTime.DaysInMonth(year, month);

            DateOnly result = new(year, month, lastDay);
            if (result.DayOfWeek == DayOfWeek.Saturday)
                result = result.AddDays(-1);
            if (result.DayOfWeek == DayOfWeek.Sunday)
                result = result.AddDays(-2);

            return result;

        }


    }
}
