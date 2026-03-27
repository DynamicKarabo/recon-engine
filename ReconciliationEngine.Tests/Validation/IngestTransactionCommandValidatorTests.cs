using FluentAssertions;
using FluentValidation.TestHelper;
using ReconciliationEngine.Application.Commands;
using ReconciliationEngine.Application.Validators;
using Xunit;

namespace ReconciliationEngine.Tests.Validation;

public class IngestTransactionCommandValidatorTests
{
    private readonly IngestTransactionCommandValidator _validator;

    public IngestTransactionCommandValidatorTests()
    {
        _validator = new IngestTransactionCommandValidator();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void IngestTransactionCommand_WithInvalidAmount_ShouldHaveError(decimal amount)
    {
        var command = CreateValidCommand();
        command.Amount = amount;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Amount)
            .WithErrorMessage("Amount must be greater than 0");
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("USDD")]
    [InlineData("US")]
    [InlineData("ABCD")]
    [InlineData("")]
    public void IngestTransactionCommand_WithInvalidCurrency_ShouldHaveError(string currency)
    {
        var command = CreateValidCommand();
        command.Currency = currency;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be a valid ISO 4217 code");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("ZAR")]
    [InlineData("jpy")]
    public void IngestTransactionCommand_WithValidCurrency_ShouldNotHaveError(string currency)
    {
        var command = CreateValidCommand();
        command.Currency = currency;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Currency);
    }

    [Fact]
    public void IngestTransactionCommand_WithCurrencyExceeding3Chars_ShouldHaveError()
    {
        var command = CreateValidCommand();
        command.Currency = "USDD";

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency must be a 3-letter ISO 4217 code");
    }

    [Fact]
    public void IngestTransactionCommand_WithSourceExceeding255Chars_ShouldHaveError()
    {
        var command = CreateValidCommand();
        command.Source = new string('A', 256);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Source)
            .WithErrorMessage("Source must not exceed 100 characters");
    }

    [Fact]
    public void IngestTransactionCommand_WithExternalIdExceeding255Chars_ShouldHaveError()
    {
        var command = CreateValidCommand();
        command.ExternalId = new string('A', 256);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ExternalId)
            .WithErrorMessage("ExternalId must not exceed 255 characters");
    }

    [Fact]
    public void IngestTransactionCommand_WithFutureTransactionDate_ShouldHaveError()
    {
        var command = CreateValidCommand();
        command.TransactionDate = DateTime.UtcNow.Date.AddDays(1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.TransactionDate)
            .WithErrorMessage("TransactionDate cannot be in the future");
    }

    [Fact]
    public void IngestTransactionCommand_WithTodayTransactionDate_ShouldNotHaveError()
    {
        var command = CreateValidCommand();
        command.TransactionDate = DateTime.UtcNow.Date;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.TransactionDate);
    }

    [Fact]
    public void IngestTransactionCommand_WithPastTransactionDate_ShouldNotHaveError()
    {
        var command = CreateValidCommand();
        command.TransactionDate = DateTime.UtcNow.Date.AddDays(-30);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.TransactionDate);
    }

    [Fact]
    public void IngestTransactionCommand_WithValidCommand_ShouldNotHaveAnyErrors()
    {
        var command = CreateValidCommand();

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void IngestTransactionCommand_WithEmptySource_ShouldHaveError()
    {
        var command = CreateValidCommand();
        command.Source = string.Empty;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Source)
            .WithErrorMessage("Source is required");
    }

    [Fact]
    public void IngestTransactionCommand_WithEmptyExternalId_ShouldHaveError()
    {
        var command = CreateValidCommand();
        command.ExternalId = string.Empty;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.ExternalId)
            .WithErrorMessage("ExternalId is required");
    }

    [Fact]
    public void IngestTransactionCommand_WithEmptyCurrency_ShouldHaveError()
    {
        var command = CreateValidCommand();
        command.Currency = string.Empty;

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Currency)
            .WithErrorMessage("Currency is required");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(1000.50)]
    [InlineData(999999999.99)]
    public void IngestTransactionCommand_WithPositiveAmount_ShouldNotHaveError(decimal amount)
    {
        var command = CreateValidCommand();
        command.Amount = amount;

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Amount);
    }

    private static IngestTransactionCommand CreateValidCommand()
    {
        return new IngestTransactionCommand
        {
            Source = "BankFeedA",
            ExternalId = "EXT-001",
            Amount = 100.50m,
            Currency = "USD",
            TransactionDate = DateTime.UtcNow.Date,
            Description = "Test transaction",
            Reference = "REF-001",
            AccountId = "ACC-001",
            CorrelationId = Guid.NewGuid(),
            PerformedBy = "test-user"
        };
    }
}
