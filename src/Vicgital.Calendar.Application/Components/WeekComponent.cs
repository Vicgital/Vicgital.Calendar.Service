using Vicgital.Application.Shared.Results;
using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Domain.Helpers;

namespace Vicgital.Calendar.Application.Components
{
    public class WeekComponent(
        IQuarterRepository quarterRepository,
        IWeekRepository repository
        ) : IWeekComponent
    {
        private readonly IWeekRepository _repository = repository;
        private readonly IQuarterRepository _quarterRepository = quarterRepository;

        public async Task<Result<Week>> GetWeekAsync(string code, CancellationToken cancellationToken = default)
        {
            var week = await _repository.GetWeekAsync(code, cancellationToken);
            return week == null
                ? Error.NotFound("week_not_found", $"Week with code {code} not found.")
                : WeekDTO.MapFromDTO(week);
        }

        public async Task<Result<Week>> GetWeekAsync(int id, CancellationToken cancellationToken = default)
        {
            var week = await _repository.GetWeekAsync(id, cancellationToken);
            return week == null
                ? Error.NotFound("week_not_found", $"Week with ID {id} not found.")
                : WeekDTO.MapFromDTO(week);
        }

        public async Task<IReadOnlyList<Week>> GetWeeksByQuarterAsync(string quarterCode, CancellationToken cancellationToken = default)
        {
            var weeks = await _repository.GetWeeksByQuarterAsync(quarterCode, cancellationToken);
            return [.. weeks.Select(WeekDTO.MapFromDTO)];
        }

        public async Task<IReadOnlyList<Week>> GetWeeksByQuarterAsync(int quarterId, CancellationToken cancellationToken = default)
        {
            var weeks = await _repository.GetWeeksByQuarterAsync(quarterId, cancellationToken);
            return [.. weeks.Select(WeekDTO.MapFromDTO)];
        }

        public async Task<Result<Week>> GetWeekByDateAsync(DateOnly date, CancellationToken ct = default)
        {
            var week = await _repository.GetWeekByDateAsync(date.ToDateTime(new TimeOnly()), ct);
            return week == null
                ? Error.NotFound("week_not_found", $"Week for date {date} not found.")
                : WeekDTO.MapFromDTO(week);
        }

        public async Task<Result<IReadOnlyList<Week>>> CreateWeeksByQuarter(string quarterCode, CancellationToken ct = default)
        {
            var quarter = await _quarterRepository.GetQuarterAsync(quarterCode, ct);
            if (quarter == null)
                return Error.NotFound("quarter_not_found", $"Quarter with code '{quarterCode}' not found.");

            var quarterWeeks = WeekHelper.BuildWeeksByQuarter(QuarterDTO.MapFromDTO(quarter));

            var existingCodes = await _repository.GetWeeksByCodesAsync(quarterWeeks.Select(w => w.Code), ct);
            if (existingCodes.Count > 0)
                return Error.Conflict("weeks_exist", $"Week(s) {string.Join(", ", existingCodes.Select(w => w.Code))} already exist in the database.");

            var created = await _repository.CreateWeeksAsync(quarterWeeks.Select(WeekDTO.MapToDTO), ct);

            IReadOnlyList<Week> result = [.. created.Select(WeekDTO.MapFromDTO)];
            return Result<IReadOnlyList<Week>>.Success(result);
        }

    }
}
