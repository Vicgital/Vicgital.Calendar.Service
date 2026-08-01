using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Domain.Helpers;
using Vicgital.Calendar.Domain.Tests.TestSupport;
using Xunit;

namespace Vicgital.Calendar.Domain.Tests.Helpers
{
    public class WeekHelperTests
    {
        [Fact]
        public void BuildWeeksByQuarter_ThrowsArgumentException_WhenStartDateIsNotMonday()
        {
            var quarter = new Quarter
            {
                Code = "2026.Q1",
                StartDate = new DateOnly(2026, 1, 6), // a Tuesday
                EndDate = new DateOnly(2026, 3, 29)
            };

            Assert.Throws<ArgumentException>(() => WeekHelper.BuildWeeksByQuarter(quarter));
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildWeeksByQuarter_CoversTheEntireQuarterWithNoGapsOrOverlaps(int year)
        {
            foreach (var quarter in QuarterHelper.BuildQuartersByYear(year))
            {
                var weeks = WeekHelper.BuildWeeksByQuarter(quarter);

                Assert.Equal(quarter.StartDate, weeks[0].StartDate);
                Assert.Equal(quarter.EndDate, weeks[^1].EndDate);

                for (var i = 1; i < weeks.Count; i++)
                    Assert.Equal(weeks[i - 1].EndDate.AddDays(1), weeks[i].StartDate);
            }
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildWeeksByQuarter_CodesFollowExpectedPattern(int year)
        {
            foreach (var quarter in QuarterHelper.BuildQuartersByYear(year))
            {
                var weeks = WeekHelper.BuildWeeksByQuarter(quarter);

                Assert.Equal(
                    Enumerable.Range(1, weeks.Count).Select(i => $"{quarter.Code}.W{i}"),
                    weeks.Select(w => w.Code));
            }
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildWeeksByQuarter_EveryWeekIsAtMostSevenDays(int year)
        {
            foreach (var quarter in QuarterHelper.BuildQuartersByYear(year))
            {
                var weeks = WeekHelper.BuildWeeksByQuarter(quarter);

                Assert.All(weeks, w => Assert.InRange(w.EndDate.DayNumber - w.StartDate.DayNumber + 1, 1, 7));
            }
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildWeeksByQuarter_AllWeeksExceptPossiblyTheLastAreFullSevenDayWeeks(int year)
        {
            foreach (var quarter in QuarterHelper.BuildQuartersByYear(year))
            {
                var weeks = WeekHelper.BuildWeeksByQuarter(quarter);

                foreach (var week in weeks.Take(weeks.Count - 1))
                    Assert.Equal(7, week.EndDate.DayNumber - week.StartDate.DayNumber + 1);
            }
        }
    }
}
