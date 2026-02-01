namespace FatigueMonitoring.Web.Api.Options;

public sealed class AggregationOptions
{
    public const string SectionName = "Aggregation";

    public int IntervalSeconds { get; init; } = 30;
    public int RangeHours { get; init; } = 24;
    public int DelayMinutesThreshold { get; init; } = 30;
}
