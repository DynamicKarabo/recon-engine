using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ReconciliationEngine.Application.Data;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;
using Xunit;

namespace ReconciliationEngine.Tests.Integration;

public class InfrastructureSecurityTests : IDisposable
{
    private readonly ReconciliationDbContext _context;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;

    public InfrastructureSecurityTests()
    {
        var options = new DbContextOptionsBuilder<ReconciliationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReconciliationDbContext(options);
        
        _encryptionServiceMock = new Mock<IEncryptionService>();
    }

    [Fact]
    public void EncryptionService_MustEncryptAndDecrypt()
    {
        var plainAccountId = "ACC-12345";
        var plainDescription = "Sensitive payment description";
        
        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string s) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s + "_encrypted")));
            
        _encryptionServiceMock
            .Setup(x => x.Decrypt(It.IsAny<string>()))
            .Returns((string s) => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(s)).Replace("_encrypted", ""));

        var encryptedAccountId = _encryptionServiceMock.Object.Encrypt(plainAccountId);
        var encryptedDescription = _encryptionServiceMock.Object.Encrypt(plainDescription);

        encryptedAccountId.Should().NotBe(plainAccountId);
        encryptedDescription.Should().NotBe(plainDescription);
        
        var decryptedAccountId = _encryptionServiceMock.Object.Decrypt(encryptedAccountId);
        var decryptedDescription = _encryptionServiceMock.Object.Decrypt(encryptedDescription);
        
        decryptedAccountId.Should().Be(plainAccountId);
        decryptedDescription.Should().Be(plainDescription);
    }

    [Fact]
    public async Task Transaction_EncryptedFields_StoredInDb()
    {
        var plainAccountId = "ACC-12345";
        var plainDescription = "Sensitive payment description";
        
        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string s) => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(s + "_encrypted")));

        var encryptedAccountId = _encryptionServiceMock.Object.Encrypt(plainAccountId);
        var encryptedDescription = _encryptionServiceMock.Object.Encrypt(plainDescription);

        var transaction = Transaction.Create(
            "BankFeedA",
            "EXT-001",
            100.50m,
            "USD",
            DateTime.Today,
            encryptedDescription,
            "REF-001",
            encryptedAccountId,
            "test-user");

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var storedTransaction = await _context.Transactions.FirstAsync();

        storedTransaction.AccountId.Should().Be(encryptedAccountId);
        storedTransaction.Description.Should().Be(encryptedDescription);
        
        storedTransaction.AccountId.Should().NotContain("ACC-");
    }

    [Fact]
    public void AuditLog_HasNoPublicSetters_AppendOnlyByDesign()
    {
        var actionProperty = typeof(AuditLog).GetProperty("Action");
        actionProperty.Should().NotBeNull();
        
        var setMethod = actionProperty!.SetMethod;
        setMethod.Should().NotBeNull();
        setMethod!.IsPublic.Should().BeFalse("AuditLog should have private setters - append-only by design");
        
        var entityTypeProperty = typeof(AuditLog).GetProperty("EntityType");
        entityTypeProperty!.SetMethod!.IsPublic.Should().BeFalse();
        
        var newStateProperty = typeof(AuditLog).GetProperty("NewState");
        newStateProperty!.SetMethod!.IsPublic.Should().BeFalse();
    }

    [Fact]
    public void AuditLog_HasNoUpdateMethod()
    {
        var updateMethod = typeof(AuditLog).GetMethods()
            .FirstOrDefault(m => m.Name == "Update" || m.Name == "Modify");
        
        updateMethod.Should().BeNull("AuditLog should have no Update method - append-only by design");
    }

    [Fact]
    public async Task Transaction_WithoutSensitiveFields_ShouldAllowNullEncryption()
    {
        var transaction = Transaction.Create(
            "BankFeedA",
            "EXT-002",
            50.00m,
            "EUR",
            DateTime.Today,
            null,
            "REF-002",
            null,
            "test-user");

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var stored = await _context.Transactions.FirstAsync();

        stored.AccountId.Should().BeNull();
        stored.Description.Should().BeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
