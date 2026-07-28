using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class YearRequestValidator : AbstractValidator<YearRequest>
    {
        public YearRequestValidator()
        {
            RuleFor(request => request.Year)
                .GreaterThan(0)
                .WithMessage("Invalid year provided.");
        }
    }
}
