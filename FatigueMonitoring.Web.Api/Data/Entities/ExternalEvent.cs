using System.ComponentModel.DataAnnotations;

namespace FatigueMonitoring.Web.Api.Data.Entities;

public sealed class ExternalEvent
{
    [Key]
    public string ExternalId { get; set; } = string.Empty;
    public string? Identity { get; set; }
    public string? Name { get; set; }
    public string? AlarmType { get; set; }
    public DateTime? DeviceTime { get; set; }
    public DateTime? ServerTime { get; set; }
    public string? Shift { get; set; }
    public DateTime? ShiftDate { get; set; }
    public int? Level { get; set; }
    public decimal? SpeedKph { get; set; }
    public bool IsFollowedUp { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GeofenceId { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceGroupName { get; set; }
    public string? ManualVerificationBy { get; set; }
    public DateTime? ManualVerificationTime { get; set; }
    public string? ManualVerificationMemo { get; set; }
    public int? ManualVerificationWaitingDuration { get; set; }
    public DateTime? UploadAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? Area { get; set; }
    public string? LocationLabel { get; set; }
    public string? OperatorName { get; set; }
}
