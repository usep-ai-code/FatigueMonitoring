namespace FatigueMonitoring.Web.Api.Services;

public class BackgroundAggregationJob(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<BackgroundAggregationJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Get interval from configuration (default 3 minutes)
        var intervalMinutes = configuration.GetValue<int>("BackgroundJob:IntervalMinutes", 3);
        var interval = TimeSpan.FromMinutes(intervalMinutes);
        
        logger.LogInformation("Background Aggregation Job started with {Minutes} minute interval", intervalMinutes);

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

                logger.LogInformation("Aggregation cycle completed. Next run in {Minutes} minutes", interval.TotalMinutes);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in background aggregation job");
            }

            await Task.Delay(interval, stoppingToken);
        }

        logger.LogInformation("Background Aggregation Job stopped");
    }
}
