using Cuintable.Server.DTOs.TaxPayments;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class CreateTaxPaymentValidator : AbstractValidator<CreateTaxPaymentRequest>
{
    public CreateTaxPaymentValidator()
    {
        RuleFor(x => x.PeriodMonth)
            .InclusiveBetween(1, 12).WithMessage("Month must be between 1 and 12.");

        RuleFor(x => x.PeriodYear)
            .InclusiveBetween(2000, 2100).WithMessage("Year must be between 2000 and 2100.");

        RuleFor(x => x.AmountDue)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required.");
    }
}
