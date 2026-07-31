using Grpc.Core;
using Vicgital.Calendar.Application.Interfaces.Components;
using Vicgital.Calendar.Domain.Entities;
using Vicgital.Calendar.Service.Definition;
using Vicgital.Calendar.Service.Helpers;

namespace Vicgital.Calendar.Service.Implementation
{
    public class CalendarService(
        IWeekComponent weekComponent,
        IQuarterComponent quarterComponent,
        IFortnightComponent fortnightComponent
        ) : Definition.Calendar.CalendarBase
    {
        private readonly IWeekComponent _weekComponent = weekComponent;
        private readonly IQuarterComponent _quarterComponent = quarterComponent;
        private readonly IFortnightComponent _fortnightComponent = fortnightComponent;

        #region Quarter

        public async override Task<QuarterModel> GetQuarter(QuarterRequest request, ServerCallContext context)
        {
            var result = request.Id > 0
                ? await _quarterComponent.GetQuarterAsync(request.Id, context.CancellationToken)
                : await _quarterComponent.GetQuarterAsync(request.Code, context.CancellationToken);

            return result.Unwrap().ToProto();
        }

        public async override Task<QuartersReply> GetQuartersByYear(YearRequest request, ServerCallContext context)
        {
            QuartersReply reply = new();

            var quarters = await _quarterComponent.GetQuartersByYearAsync(request.Year, context.CancellationToken);
            reply.Quarters.AddRange(quarters.Select(q => q.ToProto()));

            return reply;
        }

        public async override Task<QuarterModel> GetQuarterByDate(DateRequest request, ServerCallContext context)
        {
            var result = await _quarterComponent.GetQuarterByDateAsync(DateOnly.Parse(request.Date), context.CancellationToken);
            return result.Unwrap().ToProto();
        }

        #endregion

        #region Week

        public async override Task<WeekModel> GetWeek(WeekRequest request, ServerCallContext context)
        {
            var result = request.Id > 0
                ? await _weekComponent.GetWeekAsync(request.Id, context.CancellationToken)
                : await _weekComponent.GetWeekAsync(request.Code, context.CancellationToken);

            return result.Unwrap().ToProto();
        }

        public async override Task<WeeksReply> GetWeeksByQuarter(QuarterRequest request, ServerCallContext context)
        {
            WeeksReply reply = new();
            IReadOnlyList<Week> weeks = request.Id > 0
                ? await _weekComponent.GetWeeksByQuarterAsync(request.Id, context.CancellationToken)
                : await _weekComponent.GetWeeksByQuarterAsync(request.Code, context.CancellationToken);

            reply.Weeks.AddRange(weeks.Select(w => w.ToProto()));

            return reply;
        }

        public async override Task<WeekModel> GetWeekByDate(DateRequest request, ServerCallContext context)
        {
            var result = await _weekComponent.GetWeekByDateAsync(DateOnly.Parse(request.Date), context.CancellationToken);
            return result.Unwrap().ToProto();
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
