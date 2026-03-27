using Azure.Messaging.ServiceBus;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace ReconciliationEngine.API.Jobs;

public class DeadLetterMonitorJob
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<DeadLetterMonitorJob> _logger;
    private readonly string _connectionString;

    public DeadLetterMonitorJob(
        ServiceBusClient serviceBusClient,
        ILogger<DeadLetterMonitorJob> logger,
        string connectionString)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
        _connectionString = connectionString;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting DeadLetterMonitorJob execution");

        try
        {
            var receiver = _serviceBusClient.CreateReceiver("reconciliation.transactions", new ServiceBusReceiverOptions
            {
                SubQueue = SubQueue.DeadLetter
            });

            var deadLetterMessages = await receiver.PeekMessagesAsync(100);

            if (deadLetterMessages.Any())
            {
                _logger.LogWarning(
                    "Found {Count} dead-letter messages in reconciliation.transactions queue",
                    deadLetterMessages.Count);

                foreach (var message in deadLetterMessages)
                {
                    _logger.LogWarning(
                        "Dead-letter message: SequenceNumber={SequenceNumber}, Subject={Subject}, CorrelationId={CorrelationId}",
                        message.SequenceNumber,
                        message.Subject,
                        message.CorrelationId);
                }

                await SendAlertAsync(deadLetterMessages.Count);
            }
            else
            {
                _logger.LogInformation("No dead-letter messages found");
            }

            await receiver.CloseAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing DeadLetterMonitorJob");
            throw;
        }

        _logger.LogInformation("Completed DeadLetterMonitorJob execution");
    }

    private Task SendAlertAsync(int messageCount)
    {
        _logger.LogCritical(
            "ALERT: {Count} messages in dead-letter queue require attention. " +
            "Please investigate and reprocess or discard these messages.",
            messageCount);

        return Task.CompletedTask;
    }
}
