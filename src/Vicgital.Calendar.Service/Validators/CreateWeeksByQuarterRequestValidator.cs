using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class CreateWeeksByQuarterRequestValidator : AbstractValidator<CreateWeeksByQuarterRequest>
    {
        public CreateWeeksByQuarterRequestValidator()
        {
            RuleFor(request => request.QuarterCode)
                .NotEmpty()
                .WithMessage("Quarter code must be provided.");
        }
    }
}
