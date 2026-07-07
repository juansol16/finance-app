using Cuintable.Server.DTOs.CreditCards;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class CreateCreditCardValidator : AbstractValidator<CreateCreditCardRequest>
{
    public CreateCreditCardValidator()
    {
        RuleFor(x => x.Bank).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Nickname).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastFourDigits)
            .NotEmpty()
            .Length(4)
            .Matches(@"^\d{4}$").WithMessage("Must be exactly 4 digits.");
        RuleFor(x => x.CutoffDay)
            .InclusiveBetween(1, 31).When(x => x.CutoffDay.HasValue);
        RuleFor(x => x.PaymentDueDay)
            .InclusiveBetween(1, 31).When(x => x.PaymentDueDay.HasValue);
    }
}
