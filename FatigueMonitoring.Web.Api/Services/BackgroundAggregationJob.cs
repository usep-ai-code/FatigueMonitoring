namespace FatigueMonitoring.Web.Api.Services;

public class BackgroundAggregationJob(
    IServiceProvider serviceProvider,
    ILogger<BackgroundAggregationJob> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5); // Run every 5 minutes

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background Aggregation Job started");

        // Wait a bit before first run to allow app to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Running data aggregation cycle at {Time}", DateTime.UtcNow);

                using var scope = serviceProvider.CreateScope();
                var aggregationService = scope.ServiceProvider.GetRequiredService<DataAggregationService>();
                
                await aggregationService.AggregateDataAsync();

                logger.LogInformation("Aggregation cycle completed. Next run in {Minutes} minutes", _interval.TotalMinutes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background aggregation job");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        logger.LogInformation("Background Aggregation Job stopped");
    }
}
