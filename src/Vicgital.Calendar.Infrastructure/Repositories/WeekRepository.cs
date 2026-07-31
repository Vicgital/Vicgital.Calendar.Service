using Dapper;
using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Data.Sql.Abstractions;

namespace Vicgital.Calendar.Infrastructure.Repositories
{
    public class WeekRepository(IDapperQueryExecutor dapper) : IWeekRepository
    {
        private readonly IDapperQueryExecutor _dapper = dapper;

        public async Task<IReadOnlyList<WeekDTO>> CreateWeeksAsync(IEnumerable<WeekDTO> weeks, CancellationToken cancellationToken = default)
        {            
            var weekList = weeks as IReadOnlyList<WeekDTO> ?? [.. weeks];
            if (weekList.Count == 0) return [];

            var parameters = new DynamicParameters();
            var rows = new string[weekList.Count];

            for (var i = 0; i < weekList.Count; i++)
            {
                rows[i] = $"(@QuarterId{i}, @Code{i}, @StartDate{i}, @EndDate{i})";
                parameters.Add($"QuarterId{i}", weekList[i].QuarterId);
                parameters.Add($"Code{i}", weekList[i].Code);
                parameters.Add($"StartDate{i}", weekList[i].StartDate);
                parameters.Add($"EndDate{i}", weekList[i].EndDate);
            }

            var inserted = await _dapper.QueryAsync<WeekDTO>(
                $@"INSERT INTO [dbo].[Week] (
                    [QuarterId],
                    [Code],
                    [StartDate],
                    [EndDate])
                OUTPUT INSERTED.*
                VALUES {string.Join(", ", rows)}", parameters, cancellationToken: cancellationToken);

            return [.. inserted];
        }

        public async Task<IReadOnlyList<WeekDTO>> GetWeeksByCodesAsync(IEnumerable<string> codes, CancellationToken cancellationToken = default)
        {
            var codeList = codes as IReadOnlyCollection<string> ?? [.. codes];
            if (codeList.Count == 0) return [];

            return [.. (await _dapper.QueryAsync<WeekDTO>(
                @"SELECT
                     [Id]
                    ,[QuarterId]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] FROM [dbo].[Week] WHERE [Code] IN @Codes",
                new { Codes = codeList }, cancellationToken: cancellationToken))];
        }

        public async Task<WeekDTO?> GetWeekAsync(string code, CancellationToken cancellationToken = default)
        {
            return await _dapper.QueryFirstOrDefaultAsync<WeekDTO?>(
                @"SELECT 
                     [Id]
                    ,[QuarterId]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] FROM [dbo].[Week] WHERE [Code] = @Code",
                new { Code = code }, cancellationToken: cancellationToken);
        }

        public async Task<WeekDTO?> GetWeekAsync(int id, CancellationToken cancellationToken = default)
        {
            return await _dapper.QueryFirstOrDefaultAsync<WeekDTO?>(
                @"SELECT 
                     [Id]
                    ,[QuarterId]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] FROM [dbo].[Week] WHERE [Id] = @Id",
                new { Id = id }, cancellationToken: cancellationToken);
        }

        public async Task<WeekDTO?> GetWeekByDateAsync(DateTime date, CancellationToken cancellationToken = default)
        {
            return await _dapper.QueryFirstOrDefaultAsync<WeekDTO?>(
                @"SELECT 
                     [Id]
                    ,[QuarterId]
                    ,[Code]
                    ,[StartDate]
                    ,[EndDate] FROM [dbo].[Week] WHERE [EndDate] >= @Date AND [StartDate] <= @Date", new { Date = date }, cancellationToken: cancellationToken);

        }

        public async Task<IReadOnlyList<WeekDTO>> GetWeeksByQuarterAsync(string quarterCode, CancellationToken cancellationToken = default)
        {
            return [.. (await _dapper.QueryAsync<WeekDTO>(
                @"SELECT 
                     [W].[Id]
                    ,[W].[QuarterId]
                    ,[W].[Code]
                    ,[W].[StartDate]
                    ,[W].[EndDate] FROM [dbo].[Week] AS [W]
                  INNER JOIN [dbo].[Quarter] AS [Q] ON [W].[QuarterId] = [Q].[Id]
                  WHERE [Q].[Code] = @QuarterCode
                  ORDER BY [W].[StartDate]",
                new { QuarterCode = quarterCode }, cancellationToken: cancellationToken))];
        }

        public async Task<IReadOnlyList<WeekDTO>> GetWeeksByQuarterAsync(int quarterId, CancellationToken cancellationToken = default)
        {
            return [.. (await _dapper.QueryAsync<WeekDTO>(
                @"SELECT 
                     [W].[Id]
                    ,[W].[QuarterId]
                    ,[W].[Code]
                    ,[W].[StartDate]
                    ,[W].[EndDate] FROM [dbo].[Week] AS [W]
                  INNER JOIN [dbo].[Quarter] AS [Q] ON [W].[QuarterId] = [Q].[Id]
                  WHERE [Q].[Id] = @QuarterId
                  ORDER BY [W].[StartDate]",
                new { QuarterId = quarterId }, cancellationToken: cancellationToken))];
        }



    }
}
