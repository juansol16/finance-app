using Cuintable.Server.DTOs.TaxableExpenses;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class UpdateTaxableExpenseValidator : AbstractValidator<UpdateTaxableExpenseRequest>
{
    public UpdateTaxableExpenseValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.AmountMXN).GreaterThan(0);
        RuleFor(x => x.IvaMXN).InclusiveBetween(0, decimal.MaxValue).When(x => x.IvaMXN.HasValue);
        RuleFor(x => x.IvaMXN).LessThanOrEqualTo(x => x.AmountMXN).When(x => x.IvaMXN.HasValue);
        RuleFor(x => x.ValidationStatus).IsInEnum();
        RuleFor(x => x.ValidationComment).MaximumLength(500);
        RuleFor(x => x.Vendor).NotEmpty().MaximumLength(200);
    }
}
