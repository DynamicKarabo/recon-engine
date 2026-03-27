using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using ReconciliationEngine.Application.Commands;
using ReconciliationEngine.Application.Data;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Enums;
using ReconciliationEngine.Domain.Events;
using Xunit;

namespace ReconciliationEngine.Tests.Integration;

public class IngestTransactionCommandHandlerTests : IDisposable
{
    private readonly ReconciliationDbContext _context;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly IngestTransactionCommandHandler _handler;
    private readonly Guid _correlationId;

    public IngestTransactionCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ReconciliationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReconciliationDbContext(options);
        
        _eventPublisherMock = new Mock<IEventPublisher>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        
        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns((string s) => $"encrypted_{s}");

        var transactionRepository = new Infrastructure.Persistence.TransactionRepository(_context);
        var auditLogger = new Infrastructure.Persistence.AuditLogger(_context);

        _handler = new IngestTransactionCommandHandler(
            _context,
            transactionRepository,
            auditLogger,
            _eventPublisherMock.Object,
            _encryptionServiceMock.Object);

        _correlationId = Guid.NewGuid();
    }

    [Fact]
    public async Task Handle_FirstIngestion_ShouldWriteToDbWithPendingStatus()
    {
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.StatusCode.Should().Be(201);
        result.IsDuplicate.Should().BeFalse();

        var transaction = await _context.Transactions.FirstOrDefaultAsync();
        transaction.Should().NotBeNull();
        transaction!.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public async Task Handle_FirstIngestion_ShouldAddAuditLogEntry()
    {
        var command = CreateValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.EntityType.Should().Be("Transaction");
        auditLog.Action.Should().Be("Ingested");
    }

    [Fact]
    public async Task Handle_FirstIngestion_ShouldReturn201Created()
    {
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        result.StatusCode.Should().Be(201);
        result.TransactionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_FirstIngestion_ShouldPublishTransactionIngestedEvent()
    {
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(
                It.Is<TransactionIngestedEvent>(e =>
                    e.TransactionId == result.TransactionId &&
                    e.Source == command.Source &&
                    e.CorrelationId == command.CorrelationId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateIngestion_ShouldReturn200OkWithExistingRecord()
    {
        var command = CreateValidCommand();
        await _handler.Handle(command, CancellationToken.None);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.StatusCode.Should().Be(200);
        result.IsDuplicate.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DuplicateIngestion_ShouldNotCreateDuplicate()
    {
        var command = CreateValidCommand();
        await _handler.Handle(command, CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        var count = await _context.Transactions.CountAsync();
        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DuplicateIngestion_ShouldNotWriteNewAuditLog()
    {
        var command = CreateValidCommand();
        await _handler.Handle(command, CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        var auditLogCount = await _context.AuditLogs.CountAsync();
        auditLogCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DuplicateIngestion_ShouldNotPublishNewAsbEvent()
    {
        var command = CreateValidCommand();
        await _handler.Handle(command, CancellationToken.None);

        await _handler.Handle(command, CancellationToken.None);

        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<TransactionIngestedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FirstIngestion_ShouldLogCorrelationId()
    {
        var command = CreateValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog!.CorrelationId.Should().Be(_correlationId);
    }

    [Fact]
    public async Task Handle_DuplicateIngestion_ShouldPreserveOriginalCorrelationId()
    {
        var command = CreateValidCommand();
        await _handler.Handle(command, CancellationToken.None);

        var auditLog = await _context.AuditLogs.FirstOrDefaultAsync();
        auditLog!.CorrelationId.Should().Be(_correlationId);
    }

    [Fact]
    public async Task Handle_FirstIngestion_ShouldEncryptSensitiveFields()
    {
        var command = CreateValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        _encryptionServiceMock.Verify(
            x => x.Encrypt(It.Is<string>(s => s == command.AccountId)),
            Times.Once);
        _encryptionServiceMock.Verify(
            x => x.Encrypt(It.Is<string>(s => s == command.Description)),
            Times.Once);
    }

    private IngestTransactionCommand CreateValidCommand()
    {
        return new IngestTransactionCommand
        {
            Source = "BankFeedA",
            ExternalId = $"EXT-{Guid.NewGuid()}",
            Amount = 100.50m,
            Currency = "USD",
            TransactionDate = DateTime.UtcNow.Date,
            Description = "Test transaction",
            Reference = "REF-001",
            AccountId = "ACC-001",
            CorrelationId = _correlationId,
            PerformedBy = "test-user"
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
