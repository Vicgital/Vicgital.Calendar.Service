using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Application.Interfaces.Components
{
    public interface IWeekComponent
    {
        Task<Week> GetWeekAsync(string code, CancellationToken cancellationToken = default);
        Task<Week> GetWeekAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Week>> GetWeeksByQuarterAsync(string quarterCode, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Week>> GetWeeksByQuarterAsync(int quarterId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Week>> CreateWeeksByQuarter(string quarterCode, CancellationToken ct = default);
        Task<Week> GetWeekByDateAsync(DateOnly date, CancellationToken ct = default);

    }
}
