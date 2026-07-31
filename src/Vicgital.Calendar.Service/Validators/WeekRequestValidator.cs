using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class WeekRequestValidator : AbstractValidator<WeekRequest>
    {
        public WeekRequestValidator()
        {
            RuleFor(request => request)
                .Must(request => request.IdentifierCase switch
                {
                    WeekRequest.IdentifierOneofCase.Id => request.Id > 0,
                    WeekRequest.IdentifierOneofCase.Code => !string.IsNullOrWhiteSpace(request.Code),
                    _ => false
                })
                .WithMessage("Either Week ID or Code must be provided.");
        }
    }
}
