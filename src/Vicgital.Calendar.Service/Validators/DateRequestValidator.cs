using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class DateRequestValidator : AbstractValidator<DateRequest>
    {
        public DateRequestValidator()
        {
            RuleFor(request => request.Date)
                .Must(date => DateTime.TryParse(date, out _))
                .WithMessage("Invalid date format provided.");
        }
    }
}
