using Grpc.Core;
using Vicgital.Application.Shared.Exceptions;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Service.Definition;
using Vicgital.Calendar.Service.Helpers;

namespace Vicgital.Calendar.Service.Implementation
{
    public class CalendarService(
        ILogger<CalendarService> logger,
        IWeekComponent weekComponent,
        IQuarterComponent quarterComponent,
        IFortnightComponent fortnightComponent
        ) : Definition.Calendar.CalendarBase
    {
        private readonly ILogger<CalendarService> _logger = logger;
        private readonly IWeekComponent _weekComponent = weekComponent;
        private readonly IQuarterComponent _quarterComponent = quarterComponent;
        private readonly IFortnightComponent _fortnightComponent = fortnightComponent;

        #region Quarter

        public async override Task<QuarterModel> GetQuarter(QuarterRequest request, ServerCallContext context)
        {
            request.Validate();

            try
            {
                var quarter = request.Id > 0
                    ? await _quarterComponent.GetQuarterAsync(request.Id)
                    : await _quarterComponent.GetQuarterAsync(request.Code);

                return quarter.ToProto();
            }
            catch (RpcException)
            {
                throw;
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
                reply.Quarters.AddRange(quarters.Select(q => q.ToProto()));

                return reply;
            }
            catch (RpcException)
            {
                throw;
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
                return quarter.ToProto();
            }
            catch (RpcException)
            {
                throw;
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
                var week = request.Id > 0
                    ? await _weekComponent.GetWeekAsync(request.Id)
                    : await _weekComponent.GetWeekAsync(request.Code);

                return week.ToProto();
            }
            catch (RpcException)
            {
                throw;
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
                IReadOnlyList<Week> weeks = request.Id > 0
                    ? await _weekComponent.GetWeeksByQuarterAsync(request.Id)
                    : await _weekComponent.GetWeeksByQuarterAsync(request.Code);

                reply.Weeks.AddRange(weeks.Select(w => w.ToProto()));

                return reply;
            }
            catch (RpcException)
            {
                throw;
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
                return week.ToProto();
            }
            catch (RpcException)
            {
                throw;
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
