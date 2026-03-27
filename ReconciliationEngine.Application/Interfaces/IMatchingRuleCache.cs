using ReconciliationEngine.Domain.Entities;

namespace ReconciliationEngine.Application.Interfaces;

public interface IMatchingRuleCache
{
    IReadOnlyList<MatchingRule> GetRules();
    void Refresh(IEnumerable<MatchingRule> rules);
}
