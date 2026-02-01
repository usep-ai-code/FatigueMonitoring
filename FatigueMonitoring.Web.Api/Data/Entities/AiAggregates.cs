namespace FatigueMonitoring.Web.Api.Data.Entities;

public sealed class AiKpi
{
    public int Id { get; set; }
    public string Area { get; set; } = string.Empty;
    public int TotalAlarms { get; set; }
    public int FollowedUp { get; set; }
    public int WaitingFollowUp { get; set; }
    public DateTime SnapshotAt { get; set; }
}

public sealed class AiActiveAlert
{
    public int Id { get; set; }
    public string AlarmId { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime? OpenedAt { get; set; }
    public int OpenMinutes { get; set; }
    public decimal? SpeedKph { get; set; }
    public string AlarmType { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public DateTime SnapshotAt { get; set; }
}

public sealed class AiAreaDistribution
{
    public int Id { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int OpenCount { get; set; }
    public DateTime SnapshotAt { get; set; }
}

public sealed class AiDelayedAlert
{
    public int Id { get; set; }
    public string AlarmId { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime? OpenedAt { get; set; }
    public int OpenMinutes { get; set; }
    public decimal? SpeedKph { get; set; }
    public string AlarmType { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string Status { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public DateTime SnapshotAt { get; set; }
}

public sealed class AiRecurrentUnit
{
    public int Id { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int EventsCount { get; set; }
    public DateTime SnapshotAt { get; set; }
}

public sealed class AiHighRiskArea
{
    public int Id { get; set; }
    public string Area { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int EventsCount { get; set; }
    public DateTime SnapshotAt { get; set; }
}

public sealed class AiDeviceHealth
{
    public int Id { get; set; }
    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int OfflineDevices { get; set; }
    public decimal Coverage { get; set; }
    public DateTime SnapshotAt { get; set; }
}
