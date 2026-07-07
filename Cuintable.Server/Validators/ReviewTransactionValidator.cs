using Cuintable.Server.DTOs.FinancialAdvisor;
using FluentValidation;

namespace Cuintable.Server.Validators;

public class ReviewTransactionValidator : AbstractValidator<ReviewTransactionRequest>
{
    public ReviewTransactionValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is "Recognized" or "NotMine")
            .WithMessage("Status must be 'Recognized' or 'NotMine'.");
    }
}
