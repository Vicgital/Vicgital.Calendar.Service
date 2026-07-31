namespace Vicgital.Calendar.Application.Components.Caching
{
    internal static class WeekCacheKeys
    {
        public const string Prefix = "week";

        public static string ById(int id) => $"{Prefix}:id:{id}";
        public static string ByCode(string code) => $"{Prefix}:code:{code}";
        public static string ByQuarterCode(string quarterCode) => $"{Prefix}:quarterCode:{quarterCode}";
        public static string ByQuarterId(int quarterId) => $"{Prefix}:quarterId:{quarterId}";
        public static string ByDate(DateOnly date) => $"{Prefix}:date:{date:O}";
    }
}
