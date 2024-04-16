namespace KCS.Server
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo MoscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
        public static DateTime ToMoscow(DateTime dateTime)
        {
            return TimeZoneInfo.ConvertTimeFromUtc(dateTime, MoscowTimeZone);
        }
        public static DateTime GetUnspecifiedUtc()
        {
            return DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
        }

        public static DateTime FromMoscow(DateTime dateTime)
        {
            return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeToUtc(dateTime, MoscowTimeZone), DateTimeKind.Unspecified);
        }
    }
}
