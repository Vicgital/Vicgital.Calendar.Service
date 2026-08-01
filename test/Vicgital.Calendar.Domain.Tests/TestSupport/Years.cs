using Xunit;

namespace Vicgital.Calendar.Domain.Tests.TestSupport
{
    internal static class Years
    {
        public const int First = 2020;
        public const int Last = 2100;

        public static TheoryData<int> Range()
        {
            var data = new TheoryData<int>();
            for (var year = First; year <= Last; year++)
                data.Add(year);

            return data;
        }
    }
}
