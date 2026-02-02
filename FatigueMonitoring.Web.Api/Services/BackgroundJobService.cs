using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Services;

public class BackgroundJobSettings
{
    /// <summary>
    /// Interval between data fetches in seconds (default: 180 = 3 minutes)
    /// </summary>
    public int DataFetchIntervalSeconds { get; set; } = 180;
    
    /// <summary>
    /// Interval between SSE heartbeats in seconds
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    
    /// <summary>
    /// Initial start time for first data fetch (format: "yyyy-MM-dd HH:mm:ss")
    /// </summary>
    public string InitialStartTime { get; set; } = "2026-02-01 00:00:00";
    
    /// <summary>
    /// Window size in minutes for each fetch (default: 3 minutes)
    /// </summary>
    public int FetchWindowMinutes { get; set; } = 3;
}

public class BackgroundJobService(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobSettings> settings,
    ILogger<BackgroundJobService> logger) : BackgroundService
{
    private readonly BackgroundJobSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background job service starting. Fetch interval: {Interval}s",
            _settings.DataFetchIntervalSeconds);

        // Initial delay to let the application start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Starting data fetch and aggregation cycle");

                // Create a scope to resolve scoped services
                using var scope = scopeFactory.CreateScope();
                var aggregationService = scope.ServiceProvider.GetRequiredService<IDataAggregationService>();

                // Step 1: Fetch data from external API
                await aggregationService.FetchAndProcessEventsAsync(stoppingToken);

                // Step 2: Calculate aggregations
                await aggregationService.CalculateAggregationsAsync(stoppingToken);

                logger.LogInformation("Data fetch and aggregation cycle completed");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Background job service stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background job cycle");
            }

            // Wait for the next cycle
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.DataFetchIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("Background job service stopped");
    }
}
