using Vicgital.Calendar.Application.DTO;

namespace Vicgital.Calendar.Application.Interfaces.Repositories
{
    public interface IFortnightRepository
    {
        Task<IReadOnlyList<FortnightDTO>> CreateFortnightsAsync(IEnumerable<FortnightDTO> fortnights, CancellationToken cancellationToken = default);
        Task<FortnightDTO?> GetFortnightAsync(string code, CancellationToken cancellationToken = default);
        Task<FortnightDTO?> GetFortnightAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FortnightDTO>> GetFortnightsByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<FortnightDTO>> GetFortnightsByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<FortnightDTO?> GetFortnightByDate(DateTime date, CancellationToken cancellationToken = default);
    }
}
