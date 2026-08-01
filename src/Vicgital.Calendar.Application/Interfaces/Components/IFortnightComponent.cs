using Vicgital.Application.Shared.Results;
using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Application.Interfaces.Components
{
    public interface IFortnightComponent
    {
        Task<Result<Fortnight>> GetFortnightAsync(string code, CancellationToken cancellationToken = default);
        Task<Result<Fortnight>> GetFortnightAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Fortnight>> GetFortnightsByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<Result<Fortnight>> GetFortnightByDateAsync(DateOnly date, CancellationToken ct = default);
        Task<Result<IReadOnlyList<Fortnight>>> CreateFortnightsByYear(int year, CancellationToken ct = default);
    }
}
