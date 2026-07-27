

using Vicgital.Application.Shared.Exceptions;
using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Domain.Helpers;

namespace Vicgital.Calendar.Application.Components
{
    public class QuarterComponent(
        IQuarterRepository repository) : IQuarterComponent
    {
        private readonly IQuarterRepository _repository = repository;

        public async Task<Quarter> GetQuarterAsync(string code, CancellationToken cancellationToken = default)
        {
            var quarter = await _repository.GetQuarterAsync(code, cancellationToken);
            return quarter == null ? throw new NotFoundException($"Quarter with code {code} not found.") : QuarterDTO.MapFromDTO(quarter);
        }

        public async Task<Quarter> GetQuarterAsync(int id, CancellationToken cancellationToken = default)
        {
            var quarter = await _repository.GetQuarterAsync(id, cancellationToken);
            return quarter == null ? throw new NotFoundException($"Quarter with id {id} not found.") : QuarterDTO.MapFromDTO(quarter);
        }

        public async Task<Quarter> GetQuarterByDateAsync(DateOnly date, CancellationToken ct = default)
        {
            var quarter = await _repository.GetQuarterByDate(date.ToDateTime(TimeOnly.MinValue), ct);
            return quarter == null ? throw new NotFoundException($"Quarter for date {date} not found.") : QuarterDTO.MapFromDTO(quarter);
        }

        public async Task<IReadOnlyList<Quarter>> GetQuartersByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            var quarters = await _repository.GetQuartersByYearAsync(year, cancellationToken);
            return [.. quarters.Select(QuarterDTO.MapFromDTO)];
        }

        public async Task<IReadOnlyList<Quarter>> CreateQuartersByYear(int year, CancellationToken ct = default)
        {
            var yearQuarters = QuarterHelper.BuildQuartersByYear(year);

            var result = new List<Quarter>();
            foreach (var quarter in yearQuarters)
            {
                // check if the quarter already exists in the database
                var existingQuarter = await _repository.GetQuarterAsync(quarter.Code, ct);

                if (existingQuarter == null)
                {
                    var newQuarter = await _repository.CreateQuarterAsync(QuarterDTO.MapToDTO(quarter), ct);

                    result.Add(QuarterDTO.MapFromDTO(newQuarter));
                }
                else
                    throw new InvalidOperationException($"Quarter {quarter.Code} already exists in the database.");
            }

            return result;
        }


    }
}
