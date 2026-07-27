using Vicgital.Calendar.Domain.Entities;

namespace Vicgital.Calendar.Application.Interfaces.Components
{
    public interface IQuarterComponent
    {
        Task<Quarter> GetQuarterAsync(string code, CancellationToken cancellationToken = default);
        Task<Quarter> GetQuarterAsync(int id, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Quarter>> GetQuartersByYearAsync(int year, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<Quarter>> CreateQuartersByYear(int year, CancellationToken ct = default);
        Task<Quarter> GetQuarterByDateAsync(DateOnly date, CancellationToken ct = default);
    }
}
