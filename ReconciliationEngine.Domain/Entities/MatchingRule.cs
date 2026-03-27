using ReconciliationEngine.Domain.Common;

namespace ReconciliationEngine.Domain.Entities;

public class MatchingRule : Entity
{
    public string Description { get; private set; } = string.Empty;
    public int Priority { get; private set; }
    public bool IsActive { get; private set; }
    public string? ConfigJson { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private MatchingRule() { }

    public static MatchingRule Create(
        string id,
        string description,
        int priority,
        string? configJson)
    {
        return new MatchingRule
        {
            Id = Guid.Parse(id),
            Description = description,
            Priority = priority,
            IsActive = true,
            ConfigJson = configJson
        };
    }

    public void Update(string description, int priority, bool isActive, string? configJson)
    {
        Description = description;
        Priority = priority;
        IsActive = isActive;
        ConfigJson = configJson;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
