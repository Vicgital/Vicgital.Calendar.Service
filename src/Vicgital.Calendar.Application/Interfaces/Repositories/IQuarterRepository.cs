using Vicgital.Calendar.Application.DTO;

namespace Vicgital.Calendar.Application.Interfaces.Repositories
{
    public interface IQuarterRepository
    {
        Task<QuarterDTO> CreateQuarterAsync(QuarterDTO quarter, CancellationToken cancellationToken = default);
        Task<QuarterDTO?> GetQuarterAsync(string code, CancellationToken cancellationToken = default);
        Task<QuarterDTO?> GetQuarterAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<QuarterDTO>> GetQuartersByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<QuarterDTO?> GetQuarterByDate(DateTime date, CancellationToken cancellationToken = default);


    }
}
