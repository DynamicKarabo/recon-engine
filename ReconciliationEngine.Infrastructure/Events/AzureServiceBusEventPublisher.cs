using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Events;

namespace ReconciliationEngine.Infrastructure.Events;

public class AzureServiceBusEventPublisher : IEventPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger<AzureServiceBusEventPublisher> _logger;
    private const string TopicName = "reconciliation.transactions";

    public AzureServiceBusEventPublisher(
        ServiceBusClient serviceBusClient,
        ILogger<AzureServiceBusEventPublisher> logger)
    {
        _sender = serviceBusClient.CreateSender(TopicName);
        _logger = logger;
    }

    public async Task PublishAsync(TransactionIngestedEvent @event, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage
        {
            ContentType = "application/json",
            CorrelationId = @event.CorrelationId.ToString(),
            Subject = @event.GetType().Name
        };

        var envelope = new
        {
            eventType = "TransactionIngested",
            transactionId = @event.TransactionId,
            source = @event.Source,
            correlationId = @event.CorrelationId,
            occurredAt = @event.OccurredAt
        };

        message.Body = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(envelope));

        await _sender.SendMessageAsync(message, cancellationToken);
        
        _logger.LogInformation(
            "Published TransactionIngested event. TransactionId: {TransactionId}, CorrelationId: {CorrelationId}",
            @event.TransactionId,
            @event.CorrelationId);
    }

    public async Task PublishAsync(TransactionMatchedEvent @event, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage
        {
            ContentType = "application/json",
            CorrelationId = @event.CorrelationId.ToString(),
            Subject = @event.GetType().Name
        };

        var envelope = new
        {
            eventType = "TransactionMatched",
            reconciliationRecordId = @event.ReconciliationRecordId,
            transactionIds = @event.TransactionIds,
            matchMethod = @event.MatchMethod,
            confidenceScore = @event.ConfidenceScore,
            correlationId = @event.CorrelationId,
            occurredAt = @event.OccurredAt
        };

        message.Body = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(envelope));

        await _sender.SendMessageAsync(message, cancellationToken);
        
        _logger.LogInformation(
            "Published TransactionMatched event. ReconciliationRecordId: {ReconciliationRecordId}, CorrelationId: {CorrelationId}",
            @event.ReconciliationRecordId,
            @event.CorrelationId);
    }

    public async Task PublishAsync(ExceptionRaisedEvent @event, CancellationToken cancellationToken = default)
    {
        var message = new ServiceBusMessage
        {
            ContentType = "application/json",
            CorrelationId = @event.CorrelationId.ToString(),
            Subject = @event.GetType().Name
        };

        var envelope = new
        {
            eventType = "ExceptionRaised",
            exceptionRecordId = @event.ExceptionRecordId,
            transactionId = @event.TransactionId,
            category = @event.Category,
            correlationId = @event.CorrelationId,
            occurredAt = @event.OccurredAt
        };

        message.Body = BinaryData.FromBytes(JsonSerializer.SerializeToUtf8Bytes(envelope));

        await _sender.SendMessageAsync(message, cancellationToken);
        
        _logger.LogInformation(
            "Published ExceptionRaised event. ExceptionRecordId: {ExceptionRecordId}, CorrelationId: {CorrelationId}",
            @event.ExceptionRecordId,
            @event.CorrelationId);
    }
}
