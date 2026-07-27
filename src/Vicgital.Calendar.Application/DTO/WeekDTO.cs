using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Application.DTO
{
    public class WeekDTO : CalendarBaseDTO
    {
        public required int QuarterId { get; init; }

        public static WeekDTO MapToDTO(Week week)
        {
            return new WeekDTO
            {
                Id = week.Id,
                Code = week.Code,
                StartDate = week.StartDate.ToDateTime(new TimeOnly()),
                EndDate = week.EndDate.ToDateTime(new TimeOnly()),
                QuarterId = week.QuarterId
            };
        }

        public static Week MapFromDTO(WeekDTO weekDTO)
        {
            return new Week
            {
                Id = weekDTO.Id,
                Code = weekDTO.Code,
                StartDate = DateOnly.FromDateTime(weekDTO.StartDate),
                EndDate = DateOnly.FromDateTime(weekDTO.EndDate),
                QuarterId = weekDTO.QuarterId
            };
        }

    }
}
