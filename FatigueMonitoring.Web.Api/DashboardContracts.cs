namespace FatigueMonitoring.Web.Api;

public sealed record DashboardSnapshotDto(
    DateTimeOffset GeneratedAtUtc,
    DeviceHealthDto DeviceHealth,
    IReadOnlyList<AreaKpiDto> AreaKpis,
    IReadOnlyList<LocationDistributionDto> AreaDistribution,
    IReadOnlyList<AlertDto> ActiveAlerts,
    IReadOnlyList<AlertDto> DelayedAlerts,
    IReadOnlyList<RecurrentUnitDto> RecurrentUnits,
    IReadOnlyList<HighRiskAreaDto> HighRiskAreas);

public sealed record DeviceHealthDto(
    int TotalDevices,
    int OnlineDevices,
    int OfflineDevices,
    int CoveragePercent);

public sealed record AreaKpiDto(
    string Area,
    int TotalAlarms,
    int FollowedUp,
    int WaitingFollowUp);

public sealed record LocationDistributionDto(
    string Area,
    string Location,
    int OpenCount,
    int TotalCount);

public sealed record AlertDto(
    string Id,
    string Unit,
    string Operator,
    string Area,
    string Location,
    DateTimeOffset OpenedAtUtc,
    string Status,
    decimal SpeedKph,
    string AlarmType,
    double? Latitude,
    double? Longitude,
    int Occurrences);

public sealed record RecurrentUnitDto(
    string Unit,
    string Operator,
    string Area,
    int Events);

public sealed record HighRiskAreaDto(
    string Area,
    string Location,
    int Events);
