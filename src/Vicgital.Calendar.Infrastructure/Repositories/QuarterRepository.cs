using Vicgital.Calendar.Application.DTO;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Data.Sql.Abstractions;

namespace Vicgital.Calendar.Infrastructure.Repositories
{
    public class QuarterRepository(IDapperQueryExecutor dapper) : IQuarterRepository
    {
        private readonly IDapperQueryExecutor _dapper = dapper;

        public async Task<QuarterDTO> CreateQuarterAsync(QuarterDTO quarter, CancellationToken cancellationToken = default)
        {
            var newQuarter =
                await _dapper.QuerySingleAsync<QuarterDTO>(@"INSERT INTO [dbo].[Quarter] (
                        [Code],
                        [StartDate],
                        [EndDate]) 
                        OUTPUT INSERTED.*
                        VALUES(
                        @Code, 
                        @StartDate, 
                        @EndDate)", new
                {
                    quarter.Code,
                    quarter.StartDate,
                    quarter.EndDate,
                }, cancellationToken: cancellationToken);

            return newQuarter;
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
