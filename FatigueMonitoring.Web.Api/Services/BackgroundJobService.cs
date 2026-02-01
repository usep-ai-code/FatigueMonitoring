using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Services;

public class BackgroundJobSettings
{
    public int DataFetchIntervalSeconds { get; set; } = 60;
    public int HeartbeatIntervalSeconds { get; set; } = 30;
}

public class BackgroundJobService(
    IDataAggregationService aggregationService,
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
