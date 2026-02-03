namespace FatigueMonitoring.Web.Api.DTOs;

#region Dashboard Response DTOs

public record DashboardDataDto(
    SummaryDto Summary,
    AreaSummaryDto AreaSummary,
    List<AreaDistributionDto> MiningDistribution,
    List<AreaDistributionDto> HaulingDistribution,
    List<ActiveAlertDto> ActiveAlerts,
    List<DelayedFollowUpDto> DelayedFollowUps,
    List<RecurrentUnitDto> RecurrentUnits,
    List<HighRiskAreaDto> HighRiskAreas,
    DateTime LastUpdated
);

public record SummaryDto(
    int TotalAlarms,
    int FollowedUp,
    int WaitingFollowUp
);

public record AreaSummaryDto(
    AreaStatsDto Mining,
    AreaStatsDto Hauling
);

public record AreaStatsDto(
    int Total,
    int Open,
    int Resolved
);

public record AreaDistributionDto(
    string GroupName,
    int Count
);

public record ActiveAlertDto(
    int Id,
    string ExternalId,
    string UnitName,
    string OperatorName,
    string AlertType,
    string Area,
    string Location,
    DateTime EventTime,
    string EventTimeFormatted,
    int OpenDurationMinutes,
    string Status,
    decimal Speed,
    int AlertCountToday,
    string? ImageUrl,
    string? VideoUrl,
    decimal Latitude,
    decimal Longitude
);

public record DelayedFollowUpDto(
    int Id,
    string ExternalId,
    string UnitName,
    string OperatorName,
    string AlertType,
    string Area,
    string Location,
    DateTime EventTime,
    string EventTimeFormatted,
    int DelayMinutes,
    decimal Speed,
    string? ImageUrl,
    string? VideoUrl,
    decimal Latitude,
    decimal Longitude
);

public record RecurrentUnitDto(
    int Id,
    string UnitName,
    string OperatorName,
    int EventCount,
    string PrimaryArea,
    string Status
);

public record HighRiskAreaDto(
    int Id,
    string Location,
    string Area,
    int EventCount,
    string RiskLevel
);

#endregion

#region SSE Event DTOs

public record SseEventDto(
    string EventType,
    object Data,
    DateTime Timestamp
);

public record HeartbeatDto(
    string Status,
    DateTime ServerTime
);

#endregion
