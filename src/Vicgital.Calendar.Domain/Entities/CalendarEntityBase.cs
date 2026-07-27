namespace Vicgital.Calendar.Domain.Entities
{
    public class CalendarEntityBase
    {
        public int Id { get; init; }
        public required string Code { get; init; }
        public required DateOnly StartDate { get; init; }
        public required DateOnly EndDate { get; init; }
    }
}
