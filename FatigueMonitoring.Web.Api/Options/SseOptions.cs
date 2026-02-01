namespace FatigueMonitoring.Web.Api.Options;

public sealed class SseOptions
{
    public const string SectionName = "Sse";

    public int HeartbeatSeconds { get; init; } = 15;
    public int RefreshSeconds { get; init; } = 10;
}
