using FluentValidation;
using Google.Type;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class DateRequestValidator : AbstractValidator<DateRequest>
    {
        public DateRequestValidator()
        {
            RuleFor(request => request.Date)
                .Must(BeAValidDate)
                .WithMessage("A valid date must be provided.");
        }

        private static bool BeAValidDate(Date? date) =>
            date is not null
            && date.Year is >= 1 and <= 9999
            && date.Month is >= 1 and <= 12
            && date.Day >= 1
            && date.Day <= System.DateTime.DaysInMonth(date.Year, date.Month);
    }
}
