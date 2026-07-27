using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Data.Sql.Abstractions;

namespace Vicgital.Calendar.Infrastructure.Repositories
{
    public class FortnightRepository(IDapperQueryExecutor dapper) : IFortnightRepository
    {
        private readonly IDapperQueryExecutor _dapper = dapper;

    }
}
