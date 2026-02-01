namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Raw fatigue event data from external API
/// </summary>
public class AI_FatigueEvent_T
{
    public Guid Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string Identity { get; set; } = string.Empty;
    public string AlarmName { get; set; } = string.Empty;
    public string AlarmType { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public DateTime ServerTime { get; set; }
    public string Shift { get; set; } = string.Empty;
    public DateTime ShiftDate { get; set; }
    public int Level { get; set; }
    public decimal Speed { get; set; }
    public bool IsFollowedUp { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string? GeofenceId { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public string? DriverId { get; set; }
    public string? ManualVerificationBy { get; set; }
    public DateTime? ManualVerificationTime { get; set; }
    public string? ManualVerificationMemo { get; set; }
    public int? ManualVerificationWaitingDuration { get; set; }
    
    // Device info
    public string DeviceImei { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    
    // Computed fields
    public string Area { get; set; } = string.Empty; // Mining or Hauling (based on group_name)
    public string Location { get; set; } = string.Empty; // Derived from group_name/geofence
    
    // Evidence URLs
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
