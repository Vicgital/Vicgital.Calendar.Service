namespace Vicgital.Calendar.Application.Components.Caching
{
    internal static class FortnightCacheKeys
    {
        public const string Prefix = "fortnight";

        public static string ById(int id) => $"{Prefix}:id:{id}";
        public static string ByCode(string code) => $"{Prefix}:code:{code}";
        public static string ByYear(int year) => $"{Prefix}:year:{year}";
        public static string ByDate(DateOnly date) => $"{Prefix}:date:{date:O}";
    }
}
