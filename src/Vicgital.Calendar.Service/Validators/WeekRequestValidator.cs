using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class WeekRequestValidator : AbstractValidator<WeekRequest>
    {
        public WeekRequestValidator()
        {
            RuleFor(request => request)
                .Must(request => request.Id > 0 || !string.IsNullOrWhiteSpace(request.Code))
                .WithMessage("Either Week ID or Code must be provided.");
        }
    }
}
