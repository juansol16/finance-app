using Cuintable.Server.DTOs.Expenses;
using Cuintable.Server.Models;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class CreateExpenseValidator : AbstractValidator<CreateExpenseRequest>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.AmountMXN).GreaterThan(0);

        RuleFor(x => x.CreditCardId)
            .NotNull().When(x => x.Category == ExpenseCategory.PagoTarjeta)
            .WithMessage("Credit card is required for card payments.");

        RuleFor(x => x.CreditCardId)
            .Null().When(x => x.Category != ExpenseCategory.PagoTarjeta)
            .WithMessage("Credit card only applies to card payments.");
    }
}
