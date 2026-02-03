namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Aggregated area distribution data
/// </summary>
public class AI_AreaDistribution_T
{
    public int Id { get; set; }
    
    // Area: Mining or Hauling
    public string Area { get; set; } = string.Empty;
    
    // GroupName from device (e.g., "IPD Kerinci", "CSA Hauling")
    public string GroupName { get; set; } = string.Empty;
    
    // Location within the area (e.g., "Manado - Front A", "KM 22") - kept for backward compatibility
    public string Location { get; set; } = string.Empty;
    
    // Count of open alerts in this group
    public int OpenAlertCount { get; set; }
    
    // Count of total alerts in this group
    public int TotalAlertCount { get; set; }
    
    // Timestamps
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
}
