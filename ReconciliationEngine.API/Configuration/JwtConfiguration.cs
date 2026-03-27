namespace ReconciliationEngine.API.Configuration;

public class JwtConfiguration
{
    public const string SectionName = "Jwt";
    public string Authority { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public bool RequireHttpsMetadata { get; set; } = true;
}

public static class Roles
{
    public const string Operator = "Operator";
    public const string Admin = "Admin";
}
