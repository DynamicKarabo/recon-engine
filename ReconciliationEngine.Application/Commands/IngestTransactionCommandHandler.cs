using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;
using ReconciliationEngine.Domain.Events;
using ReconciliationEngine.Application.Data;

namespace ReconciliationEngine.Application.Commands;

public class IngestTransactionCommandHandler : IRequestHandler<IngestTransactionCommand, IngestTransactionResult>
{
    private readonly ReconciliationDbContext _context;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditLogger _auditLogger;
    private readonly IEventPublisher _eventPublisher;
    private readonly IEncryptionService _encryptionService;

    public IngestTransactionCommandHandler(
        ReconciliationDbContext context,
        ITransactionRepository transactionRepository,
        IAuditLogger auditLogger,
        IEventPublisher eventPublisher,
        IEncryptionService encryptionService)
    {
        _context = context;
        _transactionRepository = transactionRepository;
        _auditLogger = auditLogger;
        _eventPublisher = eventPublisher;
        _encryptionService = encryptionService;
    }

    public async Task<IngestTransactionResult> Handle(IngestTransactionCommand request, CancellationToken cancellationToken)
    {
        var existingTransaction = await _transactionRepository.GetBySourceAndExternalIdAsync(
            request.Source,
            request.ExternalId,
            cancellationToken);

        if (existingTransaction != null)
        {
            return new IngestTransactionResult
            {
                TransactionId = existingTransaction.Id,
                IsDuplicate = true,
                StatusCode = 200
            };
        }

        var encryptedAccountId = !string.IsNullOrEmpty(request.AccountId)
            ? _encryptionService.Encrypt(request.AccountId)
            : null;

        var encryptedDescription = !string.IsNullOrEmpty(request.Description)
            ? _encryptionService.Encrypt(request.Description)
            : null;

        var transaction = Transaction.Create(
            request.Source,
            request.ExternalId,
            request.Amount,
            request.Currency.ToUpperInvariant(),
            request.TransactionDate,
            encryptedDescription,
            request.Reference,
            encryptedAccountId,
            request.PerformedBy);

        await _transactionRepository.AddAsync(transaction, cancellationToken);

        var newState = JsonSerializer.Serialize(new
        {
            transaction.Id,
            transaction.Source,
            transaction.ExternalId,
            transaction.Amount,
            transaction.Currency,
            transaction.TransactionDate,
            transaction.Status,
            transaction.IngestedAt,
            transaction.IngestedBy
        });

        await _auditLogger.LogAsync(
            "Transaction",
            transaction.Id,
            "Ingested",
            null,
            newState,
            request.PerformedBy,
            request.CorrelationId,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var domainEvent = new TransactionIngestedEvent(
            transaction.Id,
            transaction.Source,
            request.CorrelationId);

        await _eventPublisher.PublishAsync(domainEvent, cancellationToken);

        return new IngestTransactionResult
        {
            TransactionId = transaction.Id,
            IsDuplicate = false,
            StatusCode = 201
        };
    }
}
