using Vicgital.Application.Shared.Results;
using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Application.Interfaces.Components
{
    public interface IQuarterComponent
    {
        Task<Result<Quarter>> GetQuarterAsync(string code, CancellationToken cancellationToken = default);
        Task<Result<Quarter>> GetQuarterAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Quarter>> GetQuartersByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<Result<Quarter>> GetQuarterByDateAsync(DateOnly date, CancellationToken ct = default);
        Task<Result<IReadOnlyList<Quarter>>> CreateQuartersByYear(int year, CancellationToken ct = default);
    }
}
