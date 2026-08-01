using Dapper;
using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Data.Sql.Abstractions;

namespace Vicgital.Calendar.Infrastructure.Repositories
{
    public class FortnightRepository(IDapperQueryExecutor dapper) : IFortnightRepository
    {
        private readonly IDapperQueryExecutor _dapper = dapper;

        public async Task<IReadOnlyList<FortnightDTO>> CreateFortnightsAsync(IEnumerable<FortnightDTO> fortnights, CancellationToken cancellationToken = default)
        {
            var fortnightList = fortnights as IReadOnlyList<FortnightDTO> ?? [.. fortnights];
            if (fortnightList.Count == 0) return [];

            var parameters = new DynamicParameters();
            var rows = new string[fortnightList.Count];

            for (var i = 0; i < fortnightList.Count; i++)
            {
                rows[i] = $"(@Code{i}, @StartDate{i}, @EndDate{i})";
                parameters.Add($"Code{i}", fortnightList[i].Code);
                parameters.Add($"StartDate{i}", fortnightList[i].StartDate);
                parameters.Add($"EndDate{i}", fortnightList[i].EndDate);
            }

            var inserted = await _dapper.QueryAsync<FortnightDTO>($@"INSERT INTO [dbo].[Fortnight] (
                        [Code],
                        [StartDate],
                        [EndDate])
                        OUTPUT INSERTED.*
                        VALUES {string.Join(", ", rows)}", parameters, cancellationToken: cancellationToken);

            return [.. inserted];
        }

        public async Task<IReadOnlyList<FortnightDTO>> GetFortnightsByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var codeList = codes as IReadOnlyCollection<string> ?? [.. codes];
            if (codeList.Count == 0) return [];

            var fortnights = await _dapper.QueryAsync<FortnightDTO>(
                @"SELECT
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate]
                    FROM [dbo].[Fortnight]
                    WHERE [Code] IN @Codes", new { Codes = codeList }, cancellationToken: cancellationToken);

            return [.. fortnights];
        }

        public async Task<FortnightDTO?> GetFortnightAsync(string code, CancellationToken cancellationToken = default)
        {

            var fortnight = await _dapper.QuerySingleOrDefaultAsync<FortnightDTO>(
                @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Fortnight] 
                    WHERE [Code] = @Code", new { Code = code }, cancellationToken: cancellationToken);
            return fortnight;
        }

        public async Task<FortnightDTO?> GetFortnightAsync(int id, CancellationToken cancellationToken = default)
        {
            var fortnight = await _dapper.QuerySingleOrDefaultAsync<FortnightDTO>(
                @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Fortnight] WHERE [Id] = @Id", new { Id = id }, cancellationToken: cancellationToken);
            return fortnight;
        }

        public async Task<FortnightDTO?> GetFortnightByDate(DateTime date, CancellationToken cancellationToken = default)
        {
            var fortnight = await _dapper.QuerySingleOrDefaultAsync<FortnightDTO>(
                    @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Fortnight] WHERE [EndDate] >= @Date AND [StartDate] <= @Date", new { Date = date }, cancellationToken: cancellationToken);
            return fortnight;
        }

        public async Task<IReadOnlyList<FortnightDTO>> GetFortnightsByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            var fortnights = await _dapper.QueryAsync<FortnightDTO>(
                    @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Fortnight] WHERE YEAR([StartDate]) = @Year", new { Year = year }, cancellationToken: cancellationToken);

            return [.. fortnights];
        }


    }
}
