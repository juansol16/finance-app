using Cuintable.Server.DTOs.TaxPayments;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class UpdateTaxPaymentValidator : AbstractValidator<UpdateTaxPaymentRequest>
{
    public UpdateTaxPaymentValidator()
    {
        RuleFor(x => x.AmountDue)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required.");
    }
}
