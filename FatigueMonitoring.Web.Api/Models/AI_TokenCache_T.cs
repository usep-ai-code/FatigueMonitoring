namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Cache for external API tokens
/// </summary>
public class AI_TokenCache_T
{
    public int Id { get; set; }
    public string TokenType { get; set; } = "ExternalApi";
    public string AccessToken { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Pid { get; set; } = string.Empty;
    public string CompanyId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
