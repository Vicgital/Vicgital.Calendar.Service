using Grpc.Core;
using System.Runtime.CompilerServices;
using Vicgital.Application.Shared.Exceptions;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Application.Interfaces.Repositories;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Service.Definition;
using Vicgital.Calendar.Service.Helpers;

namespace Vicgital.Calendar.Service.Implementation
{
    public class CalendarService(
        ILogger<CalendarService> logger,
        IWeekComponent weekComponent,
        IQuarterComponent quarterComponent,
        IFortnightRepository fortnightRepository
        ) : Definition.Calendar.CalendarBase
    {
        private readonly ILogger<CalendarService> _logger = logger;
        private readonly IWeekComponent _weekComponent = weekComponent;
        private readonly IQuarterComponent _quarterComponent = quarterComponent;
        private readonly IFortnightRepository _fortnightRepository = fortnightRepository;

        #region Quarter

        public async override Task<QuarterModel> GetQuarter(QuarterRequest request, ServerCallContext context)
        {
            request.Validate();

            try
            {
                if (request.Id > 0)
                {
                    var quarter = await _quarterComponent.GetQuarterAsync(request.Id);
                    return new QuarterModel
                    {
                        Id = quarter.Id,
                        Code = quarter.Code,
                        StartDate = quarter.StartDate.ToString("MM/dd/yyyy"),
                        EndDate = quarter.EndDate.ToString("MM/dd/yyyy")
                    };

                }
                else if (!string.IsNullOrWhiteSpace(request.Code))
                {
                    var quarter = await _quarterComponent.GetQuarterAsync(request.Code);
                    return new QuarterModel
                    {
                        Id = quarter.Id,
                        Code = quarter.Code,
                        StartDate = quarter.StartDate.ToString("MM/dd/yyyy"),
                        EndDate = quarter.EndDate.ToString("MM/dd/yyyy")
                    };
                }
                else
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Either Quarter ID or Code must be provided."));

            }
            catch (Exception ex)
            {
                if (ex is NotFoundException)
                {
                    _logger.LogWarning(ex, "GetQuarter({request})", request);
                    throw new RpcException(new Status(StatusCode.NotFound, "Quarter not found."));
                }

                _logger.LogError(ex, "An error occurred while fetching the quarter.");
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while fetching the quarter."));
            }
        }

        public async override Task<QuartersReply> GetQuartersByYear(YearRequest request, ServerCallContext context)
        {
            request.Validate();

            try
            {
                QuartersReply reply = new();

                var quarters = await _quarterComponent.GetQuartersByYearAsync(request.Year);
                reply.Quarters.AddRange(quarters.Select(q => new QuarterModel
                {
                    Id = q.Id,
                    Code = q.Code,
                    StartDate = q.StartDate.ToString("MM/dd/yyyy"),
                    EndDate = q.EndDate.ToString("MM/dd/yyyy")
                }));

                return reply;
            }
            catch (Exception ex)
            {
                if (ex is NotFoundException)
                {
                    _logger.LogWarning(ex, "GetQuartersByYear({request})", request);
                    throw new RpcException(new Status(StatusCode.NotFound, "Quarters not found for the specified year."));
                }

                _logger.LogError(ex, "An error occurred while fetching the quarters.");
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while fetching the quarters."));
            }
        }

        public async override Task<QuarterModel> GetQuarterByDate(DateRequest request, ServerCallContext context)
        {
            request.Validate();

            try
            {
                var quarter = await _quarterComponent.GetQuarterByDateAsync(DateOnly.Parse(request.Date));
                return new QuarterModel
                {
                    Id = quarter.Id,
                    Code = quarter.Code,
                    StartDate = quarter.StartDate.ToString("MM/dd/yyyy"),
                    EndDate = quarter.EndDate.ToString("MM/dd/yyyy")
                };
            }
            catch (Exception ex)
            {
                if (ex is NotFoundException)
                {
                    _logger.LogWarning(ex, "GetQuarterByDate({request})", request);
                    throw new RpcException(new Status(StatusCode.NotFound, "Quarter not found for the specified date."));
                }

                _logger.LogError(ex, "An error occurred while fetching the quarter by date.");
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while fetching the quarter by date."));
            }
        }

        #endregion

        #region Week

        public async override Task<WeekModel> GetWeek(WeekRequest request, ServerCallContext context)
        {
            request.Validate();

            try
            {
                if (request.Id > 0)
                {
                    var week = await _weekComponent.GetWeekAsync(request.Id);
                    return new WeekModel
                    {
                        Id = week.Id,
                        Code = week.Code,
                        StartDate = week.StartDate.ToString("MM/dd/yyyy"),
                        EndDate = week.EndDate.ToString("MM/dd/yyyy")
                    };

                }
                else if (!string.IsNullOrWhiteSpace(request.Code))
                {
                    var week = await _weekComponent.GetWeekAsync(request.Code);
                    return new WeekModel
                    {
                        Id = week.Id,
                        Code = week.Code,
                        StartDate = week.StartDate.ToString("MM/dd/yyyy"),
                        EndDate = week.EndDate.ToString("MM/dd/yyyy")
                    };
                }
                else
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Either Week ID or Code must be provided."));
            }
            catch (Exception ex)
            {
                if (ex is NotFoundException)
                {
                    _logger.LogWarning(ex, "GetWeek({request})", request);
                    throw new RpcException(new Status(StatusCode.NotFound, "Week not found."));
                }

                _logger.LogError(ex, "An error occurred while fetching the week.");
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while fetching the week."));
            }
        }

        public async override Task<WeeksReply> GetWeeksByQuarter(QuarterRequest request, ServerCallContext context)
        {
            request.Validate();
            try
            {
                WeeksReply reply = new();
                IReadOnlyList<Week> weeks;

                if (request.Id > 0)
                {
                    weeks = await _weekComponent.GetWeeksByQuarterAsync(request.Id);
                }
                else if (!string.IsNullOrWhiteSpace(request.Code))
                {
                    weeks = await _weekComponent.GetWeeksByQuarterAsync(request.Code);
                }
                else
                    throw new RpcException(new Status(StatusCode.InvalidArgument, "Either Quarter ID or Code must be provided."));

                reply.Weeks.AddRange(weeks.Select(w => new WeekModel
                {
                    Id = w.Id,
                    Code = w.Code,
                    StartDate = w.StartDate.ToString("MM/dd/yyyy"),
                    EndDate = w.EndDate.ToString("MM/dd/yyyy")
                }));

                return reply;
            }
            catch (Exception ex)
            {
                if (ex is NotFoundException)
                {
                    _logger.LogWarning(ex, "GetWeeksByQuarter({request})", request);
                    throw new RpcException(new Status(StatusCode.NotFound, "Weeks not found for the specified quarter."));
                }
                _logger.LogError(ex, "An error occurred while fetching the weeks by quarter.");
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while fetching the weeks by quarter."));
            }
        }

        public async override Task<WeekModel> GetWeekByDate(DateRequest request, ServerCallContext context)
        {
            request.Validate();

            try
            {
                var week = await _weekComponent.GetWeekByDateAsync(DateOnly.Parse(request.Date));
                return new WeekModel
                {
                    Id = week.Id,
                    Code = week.Code,
                    StartDate = week.StartDate.ToString("MM/dd/yyyy"),
                    EndDate = week.EndDate.ToString("MM/dd/yyyy")
                };
            }
            catch (Exception ex)
            {
                if (ex is NotFoundException)
                {
                    _logger.LogWarning(ex, "GetWeekByDate({request})", request);
                    throw new RpcException(new Status(StatusCode.NotFound, "Week not found for the specified date."));
                }

                _logger.LogError(ex, "An error occurred while fetching the week by date.");
                throw new RpcException(new Status(StatusCode.Internal, "An error occurred while fetching the week by date."));
            }
        }

        #endregion

        #region Fortnight

        public override Task<FortnightModel> GetFortnight(FortnightRequest request, ServerCallContext context)
        {
            // TODO
            return base.GetFortnight(request, context);
        }

        public override Task<FortnightsReply> GetFortnightsByMonthAndYear(MonthRequest request, ServerCallContext context)
        {
            // TODO
            return base.GetFortnightsByMonthAndYear(request, context);
        }

        public override Task<FortnightsReply> GetFortnightsByYear(YearRequest request, ServerCallContext context)
        {
            // TODO
            return base.GetFortnightsByYear(request, context);
        }



        #endregion


    }
}
