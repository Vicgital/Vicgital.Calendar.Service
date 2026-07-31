using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class QuarterRequestValidator : AbstractValidator<QuarterRequest>
    {
        public QuarterRequestValidator()
        {
            RuleFor(request => request)
                .Must(request => request.IdentifierCase switch
                {
                    QuarterRequest.IdentifierOneofCase.Id => request.Id > 0,
                    QuarterRequest.IdentifierOneofCase.Code => !string.IsNullOrWhiteSpace(request.Code),
                    _ => false
                })
                .WithMessage("Either Quarter ID or Code must be provided.");
        }
    }
}
