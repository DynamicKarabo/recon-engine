using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReconciliationEngine.Application.Data;
using ReconciliationEngine.Domain.Enums;

namespace ReconciliationEngine.API.Jobs;

public class StaleExceptionAlertJob
{
    private readonly ReconciliationDbContext _context;
    private readonly ILogger<StaleExceptionAlertJob> _logger;

    public StaleExceptionAlertJob(
        ReconciliationDbContext context,
        ILogger<StaleExceptionAlertJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting StaleExceptionAlertJob execution");

        try
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-48);

            var staleExceptions = await _context.ExceptionRecords
                .Where(e => e.Status == ExceptionStatus.PendingReview || e.Status == ExceptionStatus.UnderReview)
                .Where(e => e.CreatedAt < cutoffDate)
                .Where(e => e.AssignedTo == null)
                .Select(e => new
                {
                    e.Id,
                    e.TransactionId,
                    e.Category,
                    e.CreatedAt
                })
                .ToListAsync();

            if (staleExceptions.Any())
            {
                _logger.LogWarning(
                    "Found {Count} stale exceptions older than 48 hours without a reviewer",
                    staleExceptions.Count);

                foreach (var exception in staleExceptions)
                {
                    _logger.LogWarning(
                        "Stale exception: Id={Id}, TransactionId={TransactionId}, Category={Category}, CreatedAt={CreatedAt}",
                        exception.Id,
                        exception.TransactionId,
                        exception.Category,
                        exception.CreatedAt);
                }

                await SendAlertAsync(staleExceptions.Count);
            }
            else
            {
                _logger.LogInformation("No stale exceptions found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing StaleExceptionAlertJob");
            throw;
        }

        _logger.LogInformation("Completed StaleExceptionAlertJob execution");
    }

    private Task SendAlertAsync(int exceptionCount)
    {
        _logger.LogCritical(
            "ALERT: {Count} exceptions have been pending review for more than 48 hours. " +
            "Please assign reviewers to these exceptions.",
            exceptionCount);

        return Task.CompletedTask;
    }
}
