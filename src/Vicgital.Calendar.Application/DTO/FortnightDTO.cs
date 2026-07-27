using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Application.DTO
{
    public class FortnightDTO : CalendarBaseDTO
    {

        public static FortnightDTO MapToDTO(Fortnight fortnight)
        {
            return new FortnightDTO
            {
                Id = fortnight.Id,
                Code = fortnight.Code,
                StartDate = fortnight.StartDate.ToDateTime(new TimeOnly()),
                EndDate = fortnight.EndDate.ToDateTime(new TimeOnly())
            };
        }

        public static Fortnight MapFromDTO(FortnightDTO fortnightDTO)
        {
            return new Fortnight
            {
                Id = fortnightDTO.Id,
                Code = fortnightDTO.Code,
                StartDate = DateOnly.FromDateTime(fortnightDTO.StartDate),
                EndDate = DateOnly.FromDateTime(fortnightDTO.EndDate)
            };

        }
    }
}
