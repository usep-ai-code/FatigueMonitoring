namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Units/operators with recurrent fatigue events
/// </summary>
public class AI_RecurrentUnit_T
{
    public int Id { get; set; }
    
    // Unit info
    public string UnitName { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    
    // Event count
    public int EventCount { get; set; }
    
    // Area where most events occurred
    public string PrimaryArea { get; set; } = string.Empty;
    
    // Status: Monitoring, HighRisk
    public string Status { get; set; } = "Monitoring";
    
    // Date range for counting
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    // Timestamps
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
