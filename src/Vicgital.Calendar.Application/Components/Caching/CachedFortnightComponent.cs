using Vicgital.Application.Shared.Results;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Core.Caching;
using Vicgital.Core.Caching.Abstractions;

namespace Vicgital.Calendar.Application.Components.Caching
{
    public sealed class CachedFortnightComponent(
        IFortnightComponent inner,
        ICacheService cache) : IFortnightComponent
    {
        private static readonly CacheEntryOptions Options = new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) };

        public Task<Result<Fortnight>> GetFortnightAsync(string code, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(FortnightCacheKeys.ByCode(code), ct => inner.GetFortnightAsync(code, ct), Options, cancellationToken);

        public Task<Result<Fortnight>> GetFortnightAsync(int id, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(FortnightCacheKeys.ById(id), ct => inner.GetFortnightAsync(id, ct), Options, cancellationToken);

        public Task<Result<Fortnight>> GetFortnightByDateAsync(DateOnly date, CancellationToken ct = default)
            => cache.GetOrCreateAsync(FortnightCacheKeys.ByDate(date), token => inner.GetFortnightByDateAsync(date, token), Options, ct);

        public Task<IReadOnlyList<Fortnight>> GetFortnightsByYearAsync(int year, CancellationToken cancellationToken = default)
            => cache.GetOrCreateAsync(FortnightCacheKeys.ByYear(year), ct => inner.GetFortnightsByYearAsync(year, ct), Options, cancellationToken);

        public async Task<Result<IReadOnlyList<Fortnight>>> CreateFortnightsByYear(int year, CancellationToken ct = default)
        {
            var created = await inner.CreateFortnightsByYear(year, ct);
            if (created.IsSuccess)
                cache.RemoveByPrefix(FortnightCacheKeys.Prefix);
            return created;
        }
    }
}
