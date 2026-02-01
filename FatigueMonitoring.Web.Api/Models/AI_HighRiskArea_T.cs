namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// High risk areas with frequent fatigue events
/// </summary>
public class AI_HighRiskArea_T
{
    public int Id { get; set; }
    
    // Location info
    public string Location { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty; // Mining or Hauling
    
    // Event count
    public int EventCount { get; set; }
    
    // Risk level: Normal, High, Critical
    public string RiskLevel { get; set; } = "Normal";
    
    // Date range for counting
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    // Timestamps
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
