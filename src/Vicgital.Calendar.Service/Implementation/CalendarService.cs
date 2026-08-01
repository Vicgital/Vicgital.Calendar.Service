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
            var result = request.IdentifierCase == QuarterRequest.IdentifierOneofCase.Id
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
            var result = await _quarterComponent.GetQuarterByDateAsync(request.Date.ToDateOnly(), context.CancellationToken);
            return result.Unwrap().ToProto();
        }

        public async override Task<QuartersReply> CreateQuartersByYear(YearRequest request, ServerCallContext context)
        {
            var quarters = (await _quarterComponent.CreateQuartersByYear(request.Year, context.CancellationToken)).Unwrap();

            QuartersReply reply = new();
            reply.Quarters.AddRange(quarters.Select(q => q.ToProto()));

            return reply;
        }

        #endregion

        #region Week

        public async override Task<WeekModel> GetWeek(WeekRequest request, ServerCallContext context)
        {
            var result = request.IdentifierCase == WeekRequest.IdentifierOneofCase.Id
                ? await _weekComponent.GetWeekAsync(request.Id, context.CancellationToken)
                : await _weekComponent.GetWeekAsync(request.Code, context.CancellationToken);

            return result.Unwrap().ToProto();
        }

        public async override Task<WeeksReply> GetWeeksByQuarter(QuarterRequest request, ServerCallContext context)
        {
            WeeksReply reply = new();
            IReadOnlyList<Week> weeks = request.IdentifierCase == QuarterRequest.IdentifierOneofCase.Id
                ? await _weekComponent.GetWeeksByQuarterAsync(request.Id, context.CancellationToken)
                : await _weekComponent.GetWeeksByQuarterAsync(request.Code, context.CancellationToken);

            reply.Weeks.AddRange(weeks.Select(w => w.ToProto()));

            return reply;
        }

        public async override Task<WeekModel> GetWeekByDate(DateRequest request, ServerCallContext context)
        {
            var result = await _weekComponent.GetWeekByDateAsync(request.Date.ToDateOnly(), context.CancellationToken);
            return result.Unwrap().ToProto();
        }

        public async override Task<WeeksReply> CreateWeeksByQuarter(CreateWeeksByQuarterRequest request, ServerCallContext context)
        {
            var weeks = (await _weekComponent.CreateWeeksByQuarter(request.QuarterCode, context.CancellationToken)).Unwrap();

            WeeksReply reply = new();
            reply.Weeks.AddRange(weeks.Select(w => w.ToProto()));

            return reply;
        }

        #endregion

        #region Fortnight

        public async override Task<FortnightModel> GetFortnight(FortnightRequest request, ServerCallContext context)
        {
            var result = request.IdentifierCase == FortnightRequest.IdentifierOneofCase.Id
                ? await _fortnightComponent.GetFortnightAsync(request.Id, context.CancellationToken)
                : await _fortnightComponent.GetFortnightAsync(request.Code, context.CancellationToken);

            return result.Unwrap().ToProto();
        }

        public async override Task<FortnightsReply> GetFortnightsByYear(YearRequest request, ServerCallContext context)
        {
            FortnightsReply reply = new();

            var fortnights = await _fortnightComponent.GetFortnightsByYearAsync(request.Year, context.CancellationToken);
            reply.Fortnights.AddRange(fortnights.Select(f => f.ToProto()));

            return reply;
        }

        public async override Task<FortnightModel> GetFortnightByDate(DateRequest request, ServerCallContext context)
        {
            var result = await _fortnightComponent.GetFortnightByDateAsync(request.Date.ToDateOnly(), context.CancellationToken);
            return result.Unwrap().ToProto();
        }

        public async override Task<FortnightsReply> CreateFortnightsByYear(YearRequest request, ServerCallContext context)
        {
            var fortnights = (await _fortnightComponent.CreateFortnightsByYear(request.Year, context.CancellationToken)).Unwrap();

            FortnightsReply reply = new();
            reply.Fortnights.AddRange(fortnights.Select(f => f.ToProto()));

            return reply;
        }        

        #endregion

    }
}
