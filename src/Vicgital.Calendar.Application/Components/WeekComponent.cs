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
            var week = await _repository.GetWeekByDateAsync(date.ToDateTime(TimeOnly.MinValue), ct);
            return week == null
                ? Error.NotFound("week_not_found", $"Week for date {date} not found.")
                : WeekDTO.MapFromDTO(week);
        }

        public async Task<IReadOnlyList<Week>> CreateWeeksByQuarter(string quarterCode, CancellationToken ct = default)
        {
            var quarter = await _quarterRepository.GetQuarterAsync(quarterCode, ct) ?? throw new ArgumentException($"Quarter with code '{quarterCode}' not found.");

            var quarterWeeks = WeekHelper.BuildWeeksByQuarter(QuarterDTO.MapFromDTO(quarter));

            var result = new List<Week>();
            foreach (var week in quarterWeeks)
            {
                // Check if the week already exists in the repository
                var existingWeek = await _repository.GetWeekAsync(week.Code, ct);

                if (existingWeek == null)
                {
                    var newWeek = await _repository.CreateWeekAsync(WeekDTO.MapToDTO(week), ct);
                    result.Add(WeekDTO.MapFromDTO(newWeek));
                }
                else
                    throw new InvalidOperationException($"Week {week.Code} already exists in the database.");

            }

            return result;

        }

    }
}
