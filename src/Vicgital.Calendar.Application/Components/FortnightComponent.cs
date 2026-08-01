using Vicgital.Application.Shared.Results;
using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Domain.Helpers;

namespace Vicgital.Calendar.Application.Components
{
    public class FortnightComponent(
        IFortnightRepository repository) : IFortnightComponent
    {
        private readonly IFortnightRepository _repository = repository;

        public async Task<Result<Fortnight>> GetFortnightAsync(string code, CancellationToken cancellationToken = default)
        {
            var fortnight = await _repository.GetFortnightAsync(code, cancellationToken);
            return fortnight == null
                ? Error.NotFound("fortnight_not_found", $"Fortnight with code {code} not found.")
                : FortnightDTO.MapFromDTO(fortnight);
        }

        public async Task<Result<Fortnight>> GetFortnightAsync(int id, CancellationToken cancellationToken = default)
        {
            var fortnight = await _repository.GetFortnightAsync(id, cancellationToken);
            return fortnight == null
                ? Error.NotFound("fortnight_not_found", $"Fortnight with id {id} not found.")
                : FortnightDTO.MapFromDTO(fortnight);
        }

        public async Task<Result<Fortnight>> GetFortnightByDateAsync(DateOnly date, CancellationToken ct = default)
        {
            var fortnight = await _repository.GetFortnightByDate(date.ToDateTime(new TimeOnly()), ct);
            return fortnight == null
                ? Error.NotFound("fortnight_not_found", $"Fortnight for date {date} not found.")
                : FortnightDTO.MapFromDTO(fortnight);
        }

        public async Task<IReadOnlyList<Fortnight>> GetFortnightsByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            var fortnights = await _repository.GetFortnightsByYearAsync(year, cancellationToken);
            return [.. fortnights.Select(FortnightDTO.MapFromDTO)];
        }

        public async Task<Result<IReadOnlyList<Fortnight>>> CreateFortnightsByYear(int year, CancellationToken ct = default)
        {
            var yearFortnights = FortnightHelper.BuildFortnightsByYear(year);

            var existingCodes = await _repository.GetFortnightsByCodesAsync(yearFortnights.Select(q => q.Code), ct);
            if (existingCodes.Count > 0)
                return Error.Conflict("fortnights_exist", $"Fortnight(s) {string.Join(", ", existingCodes.Select(q => q.Code))} already exist in the database.");

            var created = await _repository.CreateFortnightsAsync(yearFortnights.Select(FortnightDTO.MapToDTO), ct);

            IReadOnlyList<Fortnight> result = [.. created.Select(FortnightDTO.MapFromDTO)];
            return Result<IReadOnlyList<Fortnight>>.Success(result);
        }


    }
}
