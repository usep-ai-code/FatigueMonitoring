namespace FatigueMonitoring.Web.Api;

public sealed class SyncState
{
    public int Id { get; set; }
    public DateTime? LastEventFetchUtc { get; set; }
    public DateTime? LastAggregationUtc { get; set; }
}
