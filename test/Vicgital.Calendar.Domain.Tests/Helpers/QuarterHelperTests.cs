using Vicgital.Calendar.Domain.Helpers;
using Vicgital.Calendar.Domain.Tests.TestSupport;
using Xunit;

namespace Vicgital.Calendar.Domain.Tests.Helpers
{
    public class QuarterHelperTests
    {
        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildQuartersByYear_ReturnsFiveQuarters(int year)
        {
            var quarters = QuarterHelper.BuildQuartersByYear(year);

            Assert.Equal(5, quarters.Count);
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildQuartersByYear_CodesFollowExpectedPattern(int year)
        {
            var quarters = QuarterHelper.BuildQuartersByYear(year);

            Assert.Equal(
                [$"{year}.Q1", $"{year}.Q2", $"{year}.Q3", $"{year}.Q4", $"{year}.QF"],
                quarters.Select(q => q.Code));
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildQuartersByYear_AllQuartersStartOnMonday(int year)
        {
            var quarters = QuarterHelper.BuildQuartersByYear(year);

            Assert.All(quarters, q => Assert.Equal(DayOfWeek.Monday, q.StartDate.DayOfWeek));
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildQuartersByYear_Q1ThroughQ4SpanExactlyTwelveWeeks(int year)
        {
            var quarters = QuarterHelper.BuildQuartersByYear(year);

            foreach (var quarter in quarters.Take(4))
                Assert.Equal(84, quarter.EndDate.DayNumber - quarter.StartDate.DayNumber + 1);
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildQuartersByYear_FinalQuarterEndsOnSunday(int year)
        {
            var quarters = QuarterHelper.BuildQuartersByYear(year);

            Assert.Equal(DayOfWeek.Sunday, quarters[^1].EndDate.DayOfWeek);
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildQuartersByYear_HasNoGapsOrOverlapsWithinYear(int year)
        {
            var quarters = QuarterHelper.BuildQuartersByYear(year);

            for (var i = 1; i < quarters.Count; i++)
                Assert.Equal(quarters[i - 1].EndDate.AddDays(1), quarters[i].StartDate);
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildQuartersByYear_IsContinuousWithPreviousYear(int year)
        {
            var previousYearLastQuarter = QuarterHelper.BuildQuartersByYear(year - 1)[^1];
            var thisYearFirstQuarter = QuarterHelper.BuildQuartersByYear(year)[0];

            Assert.Equal(previousYearLastQuarter.EndDate.AddDays(1), thisYearFirstQuarter.StartDate);
        }
    }
}
