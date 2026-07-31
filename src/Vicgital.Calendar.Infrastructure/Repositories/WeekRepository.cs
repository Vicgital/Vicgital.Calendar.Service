using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Data.Sql.Abstractions;

namespace Vicgital.Calendar.Infrastructure.Repositories
{
    public class WeekRepository(IDapperQueryExecutor dapper) : IWeekRepository
    {
        private readonly IDapperQueryExecutor _dapper = dapper;

        public async Task<WeekDTO> CreateWeekAsync(WeekDTO week, CancellationToken cancellationToken = default)
        {
            var newWeek = await _dapper.QuerySingleAsync<WeekDTO>(
                @"INSERT INTO [dbo].[Week] (
                    [QuarterId], 
                    [Code], 
                    [StartDate],
                    [EndDate])
                OUTPUT INSERTED.*
                VALUES (
                    @QuarterId, 
                    @Code, 
                    @StartDate, 
                    @EndDate)", new { week.QuarterId, week.Code, week.StartDate, week.EndDate }, cancellationToken: cancellationToken);

            return newWeek;

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
