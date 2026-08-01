using Vicgital.Calendar.Domain.Helpers;
using Vicgital.Calendar.Domain.Tests.TestSupport;
using Xunit;

namespace Vicgital.Calendar.Domain.Tests.Helpers
{
    public class FortnightHelperTests
    {
        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildFortnightsByYear_ReturnsTwentyFourFortnights(int year)
        {
            var fortnights = FortnightHelper.BuildFortnightsByYear(year);

            Assert.Equal(24, fortnights.Count);
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildFortnightsByYear_CodesFollowExpectedPattern(int year)
        {
            var fortnights = FortnightHelper.BuildFortnightsByYear(year);

            var expectedCodes = Enumerable.Range(1, 12)
                .SelectMany(month => new[]
                {
                    $"{year}.{month:D2}.F1",
                    $"{year}.{month:D2}.F2"
                });

            Assert.Equal(expectedCodes, fortnights.Select(f => f.Code));
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildFortnightsByYear_HasNoGapsOrOverlapsWithinYear(int year)
        {
            var fortnights = FortnightHelper.BuildFortnightsByYear(year);

            for (var i = 1; i < fortnights.Count; i++)
                Assert.Equal(fortnights[i - 1].EndDate.AddDays(1), fortnights[i].StartDate);
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildFortnightsByYear_IsContinuousWithPreviousYear(int year)
        {
            var previousYearLastFortnight = FortnightHelper.BuildFortnightsByYear(year - 1)[^1];
            var thisYearFirstFortnight = FortnightHelper.BuildFortnightsByYear(year)[0];

            Assert.Equal(previousYearLastFortnight.EndDate.AddDays(1), thisYearFirstFortnight.StartDate);
        }

        [Theory]
        [MemberData(nameof(Years.Range), MemberType = typeof(Years))]
        public void BuildFortnightsByYear_EndDatesAreNeverOnAWeekend(int year)
        {
            var fortnights = FortnightHelper.BuildFortnightsByYear(year);

            Assert.All(fortnights, f => Assert.True(
                f.EndDate.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday),
                $"{f.Code} ends on {f.EndDate.DayOfWeek}"));
        }

        [Fact]
        public void BuildFortnightsByYear_Year2034StartsImmediatelyAfterYear2033Ends()
        {
            // Regression test for a real bug: Jan 1, 2034 is a Sunday. GetStartDate previously only
            // adjusted for a Monday Jan 1 (shifting the start back 2 days to match a Dec 31 that gets
            // pulled from Sunday back to Friday), but had no matching branch for Sunday - a Jan 1 that
            // falls on Sunday leaves the previous year's Dec 31 (a Saturday, pulled back to Friday by
            // GetSecondFortnightEndDate) uncovered by any fortnight. Fixed by adding a Sunday branch
            // to GetStartDate that pulls the start back 1 day.
            var year2033 = FortnightHelper.BuildFortnightsByYear(2033);
            var year2034 = FortnightHelper.BuildFortnightsByYear(2034);

            Assert.Equal(new DateOnly(2033, 12, 30), year2033[^1].EndDate);
            Assert.Equal(new DateOnly(2033, 12, 31), year2034[0].StartDate);
            Assert.Equal(year2033[^1].EndDate.AddDays(1), year2034[0].StartDate);
        }
    }
}
