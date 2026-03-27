using ReconciliationEngine.Application.Interfaces;
using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Infrastructure.Cache;

public class MatchingRuleCache : IMatchingRuleCache
{
    private List<MatchingRule> _rules = new();
    private readonly object _lock = new();

    public IReadOnlyList<MatchingRule> GetRules()
    {
        lock (_lock)
        {
            return _rules.AsReadOnly();
        }
    }

    public void Refresh(IEnumerable<MatchingRule> rules)
    {
        lock (_lock)
        {
            _rules = rules.OrderBy(r => r.Priority).ToList();
        }
    }
}
