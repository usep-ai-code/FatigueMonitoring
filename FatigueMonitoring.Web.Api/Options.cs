namespace FatigueMonitoring.Web.Api;

public sealed class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";

    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int PageSize { get; set; } = 100;
    public string FilterColumn { get; set; } = "manual_verification_is_true_alarm";
    public string FilterValue { get; set; } = "true";
    public string AuthHeaderName { get; set; } = "Authorization";
    public string AuthHeaderScheme { get; set; } = "Bearer";
    public string TokenHeaderName { get; set; } = "X-Token";
    public string PidHeaderName { get; set; } = "X-Pid";
    public int AuthCacheMinutes { get; set; } = 30;
}

public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    public int DelayedMinutes { get; set; } = 30;
    public int MaxActiveAlerts { get; set; } = 200;
    public int MaxDelayedAlerts { get; set; } = 100;
    public int MaxRecurrentUnits { get; set; } = 20;
    public int MaxHighRiskAreas { get; set; } = 20;
    public int OnlineWindowMinutes { get; set; } = 30;
    public int InitialLookbackMinutes { get; set; } = 60;
    public int SyncOverlapMinutes { get; set; } = 2;
}

public sealed class BackgroundJobOptions
{
    public const string SectionName = "BackgroundJob";

    public int IntervalSeconds { get; set; } = 20;
}

public sealed class SseOptions
{
    public const string SectionName = "Sse";

    public int DataIntervalSeconds { get; set; } = 5;
    public int HeartbeatSeconds { get; set; } = 15;
}

public sealed class AreaMappingOptions
{
    public const string SectionName = "AreaMapping";

    public string[] MiningKeywords { get; set; } = ["CSA", "Front", "Pit"];
    public string[] HaulingKeywords { get; set; } = ["IPD", "KM"];
    public string DefaultArea { get; set; } = "Hauling";
}
