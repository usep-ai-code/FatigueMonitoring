using Microsoft.Extensions.Logging;

namespace FatigueMonitoring.Web.Api;

public sealed class TimeZoneProvider(ILogger<TimeZoneProvider> logger)
{
    public TimeZoneInfo Jakarta { get; } = ResolveTimeZone(logger);

    private static TimeZoneInfo ResolveTimeZone(ILogger logger)
    {
        var candidates = new[] { "Asia/Jakarta", "SE Asia Standard Time" };
        foreach (var candidate in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch (TimeZoneNotFoundException)
            {
                // Try next ID.
            }
            catch (InvalidTimeZoneException)
            {
                // Try next ID.
            }
        }

        logger.LogWarning("Falling back to UTC timezone. Asia/Jakarta not found.");
        return TimeZoneInfo.Utc;
    }
}
