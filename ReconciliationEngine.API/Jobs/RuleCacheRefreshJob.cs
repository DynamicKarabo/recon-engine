using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReconciliationEngine.Application.Data;
using ReconciliationEngine.Application.Interfaces;

namespace ReconciliationEngine.API.Jobs;

public class RuleCacheRefreshJob
{
    private readonly ReconciliationDbContext _context;
    private readonly IMatchingRuleCache _ruleCache;
    private readonly ILogger<RuleCacheRefreshJob> _logger;

    public RuleCacheRefreshJob(
        ReconciliationDbContext context,
        IMatchingRuleCache ruleCache,
        ILogger<RuleCacheRefreshJob> logger)
    {
        _context = context;
        _ruleCache = ruleCache;
        _logger = logger;
    }

    [DisableConcurrentExecution(timeoutInSeconds: 60)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting RuleCacheRefreshJob execution");

        try
        {
            var rules = await _context.MatchingRules
                .Where(r => r.IsActive)
                .OrderBy(r => r.Priority)
                .ToListAsync();

            _ruleCache.Refresh(rules);

            _logger.LogInformation(
                "Refreshed matching rule cache with {Count} active rules",
                rules.Count);

            foreach (var rule in rules)
            {
                _logger.LogDebug(
                    "Rule: Id={Id}, Description={Description}, Priority={Priority}",
                    rule.Id,
                    rule.Description,
                    rule.Priority);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing RuleCacheRefreshJob");
            throw;
        }

        _logger.LogInformation("Completed RuleCacheRefreshJob execution");
    }
}
