using Vicgital.Calendar.Application.DTO;

namespace Vicgital.Calendar.Application.Interfaces.Repositories
{
    public interface IWeekRepository
    {
        Task<WeekDTO> CreateWeekAsync(WeekDTO week, CancellationToken cancellationToken = default);
        Task<WeekDTO?> GetWeekAsync(string code, CancellationToken cancellationToken = default);
        Task<WeekDTO?> GetWeekAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WeekDTO>> GetWeeksByQuarterAsync(string quarterCode, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<WeekDTO>> GetWeeksByQuarterAsync(int quarterId, CancellationToken cancellationToken);
        Task<WeekDTO?> GetWeekByDateAsync(DateTime date, CancellationToken cancellationToken = default);
        
    }
}
