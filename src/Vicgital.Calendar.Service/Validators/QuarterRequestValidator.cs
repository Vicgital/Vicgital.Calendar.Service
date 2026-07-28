using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class QuarterRequestValidator : AbstractValidator<QuarterRequest>
    {
        public QuarterRequestValidator()
        {
            RuleFor(request => request)
                .Must(request => request.Id > 0 || !string.IsNullOrWhiteSpace(request.Code))
                .WithMessage("Either Quarter ID or Code must be provided.");
        }
    }
}
