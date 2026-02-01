namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Aggregated active alerts for display in dashboard
/// </summary>
public class AI_ActiveAlert_T
{
    public int Id { get; set; }
    
    // Original event reference
    public Guid FatigueEventId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    
    // Unit info
    public string UnitName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    
    // Alert info
    public string AlertType { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Time info
    public DateTime EventTime { get; set; }
    public int OpenDurationMinutes { get; set; }
    
    // Status: Open, FollowedUp
    public string Status { get; set; } = "Open";
    
    // Additional info
    public decimal Speed { get; set; }
    public int AlertCountToday { get; set; }
    
    // Evidence
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    
    // Coordinates
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    
    // Timestamps
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
