namespace FatigueMonitoring.Web.Api.Models;

public sealed record DashboardSnapshotDto(
    DateTime SnapshotAt,
    IReadOnlyList<KpiDto> Kpis,
    IReadOnlyList<ActiveAlertDto> ActiveAlerts,
    IReadOnlyList<AreaDistributionDto> AreaDistributions,
    IReadOnlyList<DelayedAlertDto> DelayedAlerts,
    IReadOnlyList<RecurrentUnitDto> RecurrentUnits,
    IReadOnlyList<HighRiskAreaDto> HighRiskAreas,
    DeviceHealthDto? DeviceHealth);

public sealed record DashboardStatusDto(
    DateTime? LastProcessedAt,
    DateTime? LastAggregationAt,
    DateTime? LastSnapshotAt,
    int ActiveAlerts,
    int DelayedAlerts,
    int TotalAlerts,
    int FollowedUpAlerts);

public sealed record KpiDto(
    string Area,
    int TotalAlarms,
    int FollowedUp,
    int WaitingFollowUp);

public sealed record ActiveAlertDto(
    string AlarmId,
    string Unit,
    string Operator,
    string Area,
    string Location,
    DateTime? OpenedAt,
    int OpenMinutes,
    decimal? SpeedKph,
    string AlarmType,
    double? Latitude,
    double? Longitude,
    string Status,
    int EventCount);

public sealed record AreaDistributionDto(
    string Area,
    string Location,
    int OpenCount);

public sealed record DelayedAlertDto(
    string AlarmId,
    string Unit,
    string Operator,
    string Area,
    string Location,
    DateTime? OpenedAt,
    int OpenMinutes,
    decimal? SpeedKph,
    string AlarmType,
    double? Latitude,
    double? Longitude,
    string Status,
    int EventCount);

public sealed record RecurrentUnitDto(
    string Unit,
    string Operator,
    string Area,
    int EventsCount);

public sealed record HighRiskAreaDto(
    string Area,
    string Location,
    int EventsCount);

public sealed record DeviceHealthDto(
    int TotalDevices,
    int OnlineDevices,
    int OfflineDevices,
    decimal Coverage);
