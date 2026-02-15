using Cuintable.Server.DTOs.TaxPayments;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class MarkAsPaidValidator : AbstractValidator<MarkAsPaidRequest>
{
    public MarkAsPaidValidator()
    {
        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Payment date is required.");
    }
}
