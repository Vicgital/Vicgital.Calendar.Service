using Vicgital.Application.Shared.Results;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Core.Caching;
using Vicgital.Core.Caching.Abstractions;

namespace Vicgital.Calendar.Application.Components.Caching
{
    public sealed class CachedQuarterComponent(
        IQuarterComponent inner,
        ICacheService cache) : IQuarterComponent
    {
        private static readonly CacheEntryOptions Options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };

        public Task<Result<Quarter>> GetQuarterAsync(string code, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(QuarterCacheKeys.ByCode(code), ct => inner.GetQuarterAsync(code, ct), Options, cancellationToken);

        public Task<Result<Quarter>> GetQuarterAsync(int id, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(QuarterCacheKeys.ById(id), ct => inner.GetQuarterAsync(id, ct), Options, cancellationToken);

        public Task<Result<Quarter>> GetQuarterByDateAsync(DateOnly date, CancellationToken ct = default)
            => cache.GetOrCreateAsync(QuarterCacheKeys.ByDate(date), token => inner.GetQuarterByDateAsync(date, token), Options, ct);

        public Task<IReadOnlyList<Quarter>> GetQuartersByYearAsync(int year, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(QuarterCacheKeys.ByYear(year), ct => inner.GetQuartersByYearAsync(year, ct), Options, cancellationToken);

        public async Task<IReadOnlyList<Quarter>> CreateQuartersByYear(int year, CancellationToken ct = default)
        {
            var created = await inner.CreateQuartersByYear(year, ct);
            cache.RemoveByPrefix(QuarterCacheKeys.Prefix);
            return created;
        }
    }
}
