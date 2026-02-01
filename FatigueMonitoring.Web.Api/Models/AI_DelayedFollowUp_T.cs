namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Alerts with delayed follow up (>30 minutes)
/// </summary>
public class AI_DelayedFollowUp_T
{
    public int Id { get; set; }
    
    // Original event reference
    public Guid FatigueEventId { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    
    // Unit info
    public string UnitName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    
    // Location info
    public string Area { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    
    // Time info
    public DateTime EventTime { get; set; }
    public int DelayMinutes { get; set; }
    
    // Timestamps
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
