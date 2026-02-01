using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("RawEvents")]
public class RawEvent(string id, string identity, string name, string alarmType)
{
    [Key]
    public string Id { get; set; } = id;
    public string Identity { get; set; } = identity;
    public string Name { get; set; } = name;
    public string AlarmType { get; set; } = alarmType;
    public DateTime Time { get; set; }
    public DateTime ServerTime { get; set; }
    public string? Shift { get; set; }
    public DateTime? ShiftDate { get; set; }
    public int Level { get; set; }
    public double Speed { get; set; }
    public bool IsFollowedUp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? GeofenceId { get; set; }
    public string? DeviceId { get; set; }
    public string? DriverId { get; set; }
    public string? ManualVerificationBy { get; set; }
    public DateTime? ManualVerificationTime { get; set; }
    public string? ManualVerificationMemo { get; set; }
    public bool TakeType { get; set; }
    public int ManualVerificationWaitingDuration { get; set; }
    public int Satellites { get; set; }
    public DateTime UploadAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? DeviceImei { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceGroupName { get; set; }
    public string? DriverName { get; set; }
    public string? GeofenceName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
