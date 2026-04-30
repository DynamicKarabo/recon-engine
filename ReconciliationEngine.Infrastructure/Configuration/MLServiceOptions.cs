namespace ReconciliationEngine.Infrastructure.Configuration;

public class MLServiceOptions
{
    public const string SectionName = "MLService";
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 5;
    public decimal ConfidenceThreshold { get; set; } = 0.85m;
}
