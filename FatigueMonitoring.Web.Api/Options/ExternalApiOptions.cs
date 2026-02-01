namespace FatigueMonitoring.Web.Api.Options;

public sealed class ExternalApiOptions
{
    public const string SectionName = "ExternalApi";

    public string BaseUrl { get; init; } = "https://api-platform-integrator.transtrack.co";
    public string AuthPath { get; init; } = "/api/v1/vss/auth";
    public string EventsPath { get; init; } = "/api/v1/events/";
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
    public string FilterColumn { get; init; } = "manual_verification_is_true_alarm";
    public string FilterValue { get; init; } = "true";
    public int PageSize { get; init; } = 200;
}
