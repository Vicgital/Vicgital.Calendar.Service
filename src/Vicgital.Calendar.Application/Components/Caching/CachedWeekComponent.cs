using Vicgital.Application.Shared.Results;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Core.Caching;
using Vicgital.Core.Caching.Abstractions;

namespace Vicgital.Calendar.Application.Components.Caching
{
    public sealed class CachedWeekComponent(
        IWeekComponent inner,
        ICacheService cache) : IWeekComponent
    {
        private static readonly CacheEntryOptions Options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };

        public Task<Result<Week>> GetWeekAsync(string code, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(WeekCacheKeys.ByCode(code), ct => inner.GetWeekAsync(code, ct), Options, cancellationToken);

        public Task<Result<Week>> GetWeekAsync(int id, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(WeekCacheKeys.ById(id), ct => inner.GetWeekAsync(id, ct), Options, cancellationToken);

        public Task<IReadOnlyList<Week>> GetWeeksByQuarterAsync(string quarterCode, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(WeekCacheKeys.ByQuarterCode(quarterCode), ct => inner.GetWeeksByQuarterAsync(quarterCode, ct), Options, cancellationToken);

        public Task<IReadOnlyList<Week>> GetWeeksByQuarterAsync(int quarterId, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(WeekCacheKeys.ByQuarterId(quarterId), ct => inner.GetWeeksByQuarterAsync(quarterId, ct), Options, cancellationToken);

        public Task<Result<Week>> GetWeekByDateAsync(DateOnly date, CancellationToken ct = default)
            => cache.GetOrCreateAsync(WeekCacheKeys.ByDate(date), token => inner.GetWeekByDateAsync(date, token), Options, ct);

        public async Task<IReadOnlyList<Week>> CreateWeeksByQuarter(string quarterCode, CancellationToken ct = default)
        {
            var created = await inner.CreateWeeksByQuarter(quarterCode, ct);
            cache.RemoveByPrefix(WeekCacheKeys.Prefix);
            return created;
        }
    }
}
