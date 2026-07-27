using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Application.DTO
{
    public class QuarterDTO : CalendarBaseDTO
    {
        public static QuarterDTO MapToDTO(Quarter quarter)
        {
            return new QuarterDTO
            {
                Id = quarter.Id,
                Code = quarter.Code,
                StartDate = quarter.StartDate.ToDateTime(new TimeOnly()),
                EndDate = quarter.EndDate.ToDateTime(new TimeOnly())
            };
        }

        public static Quarter MapFromDTO(QuarterDTO quarterDTO)
        {
            return new Quarter
            {
                Id = quarterDTO.Id,
                Code = quarterDTO.Code,
                StartDate = DateOnly.FromDateTime(quarterDTO.StartDate),
                EndDate = DateOnly.FromDateTime(quarterDTO.EndDate)
            };
        }


    }
}
