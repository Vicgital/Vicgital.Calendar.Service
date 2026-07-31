using Vicgital.Calendar.Application.DTO;

namespace Vicgital.Calendar.Application.Interfaces.Repositories
{
    public interface IQuarterRepository
    {
        Task<IReadOnlyList<QuarterDTO>> CreateQuartersAsync(IEnumerable<QuarterDTO> quarters, CancellationToken cancellationToken = default);
        Task<QuarterDTO?> GetQuarterAsync(string code, CancellationToken cancellationToken = default);
        Task<QuarterDTO?> GetQuarterAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuarterDTO>> GetQuartersByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuarterDTO>> GetQuartersByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<QuarterDTO?> GetQuarterByDate(DateTime date, CancellationToken cancellationToken = default);


    }
}
