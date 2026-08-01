using FluentValidation;
using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Validators
{
    public class FortnightRequestValidator : AbstractValidator<FortnightRequest>
    {
        public FortnightRequestValidator()
        {
            RuleFor(request => request)
                .Must(request => request.IdentifierCase switch
                {
                    FortnightRequest.IdentifierOneofCase.Id => request.Id > 0,
                    FortnightRequest.IdentifierOneofCase.Code => !string.IsNullOrWhiteSpace(request.Code),
                    _ => false
                })
                .WithMessage("Either Fortnight ID or Code must be provided.");
        }
    }
}
