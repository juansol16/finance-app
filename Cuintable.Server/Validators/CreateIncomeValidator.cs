using Cuintable.Server.DTOs.Incomes;
using Cuintable.Server.Models;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class CreateIncomeValidator : AbstractValidator<CreateIncomeRequest>
{
    public CreateIncomeValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Source is required.")
            .MaximumLength(200);

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.");

        RuleFor(x => x.AmountMXN)
            .GreaterThan(0).WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid income type.");

        RuleFor(x => x.ExchangeRate)
            .GreaterThan(0).When(x => x.ExchangeRate.HasValue)
            .WithMessage("Exchange rate must be greater than zero.");

        RuleFor(x => x.AmountUSD)
            .GreaterThan(0).When(x => x.AmountUSD.HasValue)
            .WithMessage("USD amount must be greater than zero.");
    }
}
