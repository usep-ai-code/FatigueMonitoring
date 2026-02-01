namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Aggregated dashboard summary (KPI) data
/// </summary>
public class AI_DashboardSummary_T
{
    public int Id { get; set; }
    
    // Filter type: All, Mining, Hauling
    public string FilterType { get; set; } = "All";
    
    // KPI values
    public int TotalAlarms { get; set; }
    public int FollowedUp { get; set; }
    public int WaitingFollowUp { get; set; }
    
    // Mining-specific
    public int MiningTotal { get; set; }
    public int MiningOpen { get; set; }
    public int MiningResolved { get; set; }
    
    // Hauling-specific
    public int HaulingTotal { get; set; }
    public int HaulingOpen { get; set; }
    public int HaulingResolved { get; set; }
    
    // Timestamps
    public DateTime LastCalculatedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
