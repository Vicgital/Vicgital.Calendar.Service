namespace Vicgital.Calendar.Application.DTO
{
    public class CalendarBaseDTO
    {
        public int Id { get; init; }
        public required string Code { get; init; }
        public required DateTime StartDate { get; init; }
        public required DateTime EndDate { get; init; }
    }
}
