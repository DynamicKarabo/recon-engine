using FluentValidation;
using ReconciliationEngine.Application.Commands;

namespace ReconciliationEngine.Application.Validators;

public class IngestTransactionCommandValidator : AbstractValidator<IngestTransactionCommand>
{
    private static readonly HashSet<string> ValidCurrencies = new(StringComparer.OrdinalIgnoreCase)
    {
        "USD", "EUR", "GBP", "JPY", "AUD", "CAD", "CHF", "CNY", "HKD", "NZD",
        "SEK", "KRW", "SGD", "NOK", "MXN", "INR", "RUB", "ZAR", "TRY", "BRL",
        "TWD", "DKK", "PLN", "THB", "IDR", "HUF", "CZK", "ILS", "CLP", "PHP",
        "AED", "COP", "SAR", "MYR", "RON"
    };

    public IngestTransactionCommandValidator()
    {
        RuleFor(x => x.Source)
            .NotEmpty()
            .WithMessage("Source is required")
            .MaximumLength(100)
            .WithMessage("Source must not exceed 100 characters");

        RuleFor(x => x.ExternalId)
            .NotEmpty()
            .WithMessage("ExternalId is required")
            .MaximumLength(255)
            .WithMessage("ExternalId must not exceed 255 characters");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than 0");

        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Currency is required")
            .Length(3)
            .WithMessage("Currency must be a 3-letter ISO 4217 code")
            .Must(BeValidCurrency)
            .WithMessage("Currency must be a valid ISO 4217 code");

        RuleFor(x => x.TransactionDate)
            .NotEmpty()
            .WithMessage("TransactionDate is required")
            .LessThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("TransactionDate cannot be in the future");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters");

        RuleFor(x => x.Reference)
            .MaximumLength(255)
            .WithMessage("Reference must not exceed 255 characters");

        RuleFor(x => x.AccountId)
            .MaximumLength(255)
            .WithMessage("AccountId must not exceed 255 characters");
    }

    private static bool BeValidCurrency(string currency)
    {
        return ValidCurrencies.Contains(currency);
    }
}
