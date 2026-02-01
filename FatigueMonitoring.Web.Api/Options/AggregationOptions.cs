namespace FatigueMonitoring.Web.Api.Options;

public sealed class AggregationOptions
{
    public const string SectionName = "Aggregation";

    public int IntervalSeconds { get; init; } = 180;
    public int FetchWindowMinutes { get; init; } = 3;
    public int AggregationRangeHours { get; init; } = 24;
    public int DelayMinutesThreshold { get; init; } = 30;
    public string InitialStartTime { get; init; } = "2026-02-01 00:00:00";
}
