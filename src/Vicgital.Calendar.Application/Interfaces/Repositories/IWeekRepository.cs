using Vicgital.Calendar.Application.DTO;

namespace Vicgital.Calendar.Application.Interfaces.Repositories
{
    public interface IWeekRepository
    {
        Task<IReadOnlyList<WeekDTO>> CreateWeeksAsync(IEnumerable<WeekDTO> weeks, CancellationToken cancellationToken = default);
        Task<WeekDTO?> GetWeekAsync(string code, CancellationToken cancellationToken = default);
        Task<WeekDTO?> GetWeekAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WeekDTO>> GetWeeksByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WeekDTO>> GetWeeksByQuarterAsync(string quarterCode, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WeekDTO>> GetWeeksByQuarterAsync(int quarterId, CancellationToken cancellationToken);
        Task<WeekDTO?> GetWeekByDateAsync(DateTime date, CancellationToken cancellationToken = default);

    }
}
