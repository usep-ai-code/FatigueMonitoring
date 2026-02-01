namespace FatigueMonitoring.Web.Api;

public sealed class RawEvent
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string? AlarmName { get; set; }
    public string? AlarmType { get; set; }
    public DateTime DeviceTimeUtc { get; set; }
    public DateTime? ServerTimeUtc { get; set; }
    public bool IsFollowedUp { get; set; }
    public decimal SpeedKph { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? DeviceGroup { get; set; }
    public string? OperatorName { get; set; }
    public string? ManualVerificationMemo { get; set; }
    public string? GeofenceName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
