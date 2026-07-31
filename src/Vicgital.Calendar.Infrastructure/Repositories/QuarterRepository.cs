using Dapper;
using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Data.Sql.Abstractions;

namespace Vicgital.Calendar.Infrastructure.Repositories
{
    public class QuarterRepository(IDapperQueryExecutor dapper) : IQuarterRepository
    {
        private readonly IDapperQueryExecutor _dapper = dapper;

        public async Task<IReadOnlyList<QuarterDTO>> CreateQuartersAsync(IEnumerable<QuarterDTO> quarters, CancellationToken cancellationToken = default)
        {
            var quarterList = quarters as IReadOnlyList<QuarterDTO> ?? [.. quarters];
            if (quarterList.Count == 0) return [];

            var parameters = new DynamicParameters();
            var rows = new string[quarterList.Count];

            for (var i = 0; i < quarterList.Count; i++)
            {
                rows[i] = $"(@Code{i}, @StartDate{i}, @EndDate{i})";
                parameters.Add($"Code{i}", quarterList[i].Code);
                parameters.Add($"StartDate{i}", quarterList[i].StartDate);
                parameters.Add($"EndDate{i}", quarterList[i].EndDate);
            }

            var inserted = await _dapper.QueryAsync<QuarterDTO>($@"INSERT INTO [dbo].[Quarter] (
                        [Code],
                        [StartDate],
                        [EndDate])
                        OUTPUT INSERTED.*
                        VALUES {string.Join(", ", rows)}", parameters, cancellationToken: cancellationToken);

            return [.. inserted];
        }

        public async Task<IReadOnlyList<QuarterDTO>> GetQuartersByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var codeList = codes as IReadOnlyCollection<string> ?? [.. codes];
            if (codeList.Count == 0) return [];

            var quarters = await _dapper.QueryAsync<QuarterDTO>(
                @"SELECT
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate]
                    FROM [dbo].[Quarter]
                    WHERE [Code] IN @Codes", new { Codes = codeList }, cancellationToken: cancellationToken);

            return [.. quarters];
        }

        public async Task<QuarterDTO?> GetQuarterAsync(string code, CancellationToken cancellationToken = default)
        {

            var quarter = await _dapper.QuerySingleOrDefaultAsync<QuarterDTO>(
                @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Quarter] 
                    WHERE [Code] = @Code", new { Code = code }, cancellationToken: cancellationToken);
            return quarter;
        }

        public async Task<QuarterDTO?> GetQuarterAsync(int id, CancellationToken cancellationToken = default)
        {
            var quarter = await _dapper.QuerySingleOrDefaultAsync<QuarterDTO>(
                @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Quarter] WHERE [Id] = @Id", new { Id = id }, cancellationToken: cancellationToken);
            return quarter;
        }

        public async Task<QuarterDTO?> GetQuarterByDate(DateTime date, CancellationToken cancellationToken = default)
        {
            var quarter = await _dapper.QuerySingleOrDefaultAsync<QuarterDTO>(
                    @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Quarter] WHERE [EndDate] >= @Date AND [StartDate] <= @Date", new { Date = date }, cancellationToken: cancellationToken);
            return quarter;
        }

        public async Task<IReadOnlyList<QuarterDTO>> GetQuartersByYearAsync(int year, CancellationToken cancellationToken = default)
        {
            var quarters = await _dapper.QueryAsync<QuarterDTO>(
                    @"SELECT 
                     [Id]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] 
                    FROM [dbo].[Quarter] WHERE YEAR([StartDate]) = @Year", new { Year = year }, cancellationToken: cancellationToken);

            return [.. quarters];
        }


    }
}
