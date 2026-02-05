using FatigueMonitoring.Web.Api.Data;
using Microsoft.EntityFrameworkCore;
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
    
    /// <summary>
    /// Hour of day (0-23) when data resets for the new day (default: 18 = 6:00 PM)
    /// </summary>
    public int DailyResetHour { get; set; } = 18;
}

public class BackgroundJobService(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobSettings> settings,
    ILogger<BackgroundJobService> logger) : BackgroundService
{
    private readonly BackgroundJobSettings _settings = settings.Value;
    private DateTime? _lastResetDate = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background job service starting. Fetch interval: {Interval}s, Daily reset at: {ResetHour}:00",
            _settings.DataFetchIntervalSeconds, _settings.DailyResetHour);

        // Initial delay to let the application start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if daily reset is needed (at configured hour, e.g., 18:00)
                await CheckAndPerformDailyResetAsync(stoppingToken);
                
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

    /// <summary>
    /// Check if it's time for daily reset and perform reset if needed.
    /// Reset happens once per day at the configured hour (e.g., 18:00).
    /// </summary>
    private async Task CheckAndPerformDailyResetAsync(CancellationToken cancellationToken)
    {
        // Current time in WIB (UTC+7)
        var nowWib = DateTime.UtcNow.AddHours(7);
        var currentHour = nowWib.Hour;
        var todayResetDate = nowWib.Date;
        
        // If current hour matches reset hour and we haven't reset today yet
        if (currentHour >= _settings.DailyResetHour)
        {
            // Check if we already reset today
            if (_lastResetDate.HasValue && _lastResetDate.Value.Date == todayResetDate)
            {
                return; // Already reset today
            }
            
            logger.LogInformation("========================================");
            logger.LogInformation("DAILY RESET TRIGGERED at {Time:yyyy-MM-dd HH:mm:ss} WIB", nowWib);
            logger.LogInformation("========================================");
            
            await PerformDailyResetAsync(nowWib, cancellationToken);
            _lastResetDate = todayResetDate;
            
            logger.LogInformation("Daily reset completed. Next reset will be tomorrow at {Hour}:00", _settings.DailyResetHour);
        }
    }

    /// <summary>
    /// Perform the daily reset - clear sync state and aggregation tables.
    /// </summary>
    private async Task PerformDailyResetAsync(DateTime resetTime, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();
        
        try
        {
            // 1. Reset sync state to start fresh from the new day's reset time
            var syncState = await dbContext.SyncStates
                .Where(s => s.SyncType == "ExternalApiEvents")
                .OrderBy(s => s.Id)
                .FirstOrDefaultAsync(cancellationToken);
                
            if (syncState != null)
            {
                // Set LastSyncTime to today's reset time (e.g., today at 18:00:00)
                var newStartTime = resetTime.Date.AddHours(_settings.DailyResetHour);
                syncState.LastSyncTime = newStartTime;
                syncState.LastSyncRecordCount = 0;
                syncState.LastSyncStatus = "Reset";
                syncState.LastSyncError = null;
                syncState.UpdatedAt = DateTime.UtcNow;
                
                logger.LogInformation("Reset sync state. New start time: {StartTime:yyyy-MM-dd HH:mm:ss}", newStartTime);
            }
            
            // 2. Clear aggregation tables (they will be recalculated)
            var summaries = await dbContext.DashboardSummaries.ToListAsync(cancellationToken);
            dbContext.DashboardSummaries.RemoveRange(summaries);
            logger.LogInformation("Cleared {Count} dashboard summaries", summaries.Count);
            
            var distributions = await dbContext.AreaDistributions.ToListAsync(cancellationToken);
            dbContext.AreaDistributions.RemoveRange(distributions);
            logger.LogInformation("Cleared {Count} area distributions", distributions.Count);
            
            var activeAlerts = await dbContext.ActiveAlerts.ToListAsync(cancellationToken);
            dbContext.ActiveAlerts.RemoveRange(activeAlerts);
            logger.LogInformation("Cleared {Count} active alerts", activeAlerts.Count);
            
            var delayedFollowUps = await dbContext.DelayedFollowUps.ToListAsync(cancellationToken);
            dbContext.DelayedFollowUps.RemoveRange(delayedFollowUps);
            logger.LogInformation("Cleared {Count} delayed follow-ups", delayedFollowUps.Count);
            
            var recurrentUnits = await dbContext.RecurrentUnits.ToListAsync(cancellationToken);
            dbContext.RecurrentUnits.RemoveRange(recurrentUnits);
            logger.LogInformation("Cleared {Count} recurrent units", recurrentUnits.Count);
            
            var highRiskAreas = await dbContext.HighRiskAreas.ToListAsync(cancellationToken);
            dbContext.HighRiskAreas.RemoveRange(highRiskAreas);
            logger.LogInformation("Cleared {Count} high risk areas", highRiskAreas.Count);
            
            // 3. Optionally clear old fatigue events (events from previous day)
            // Keep only events from today onwards
            var cutoffTime = resetTime.Date.AddHours(_settings.DailyResetHour);
            var oldEvents = await dbContext.FatigueEvents
                .Where(e => e.EventTime < cutoffTime)
                .ToListAsync(cancellationToken);
            dbContext.FatigueEvents.RemoveRange(oldEvents);
            logger.LogInformation("Cleared {Count} old fatigue events (before {CutoffTime:yyyy-MM-dd HH:mm:ss})", 
                oldEvents.Count, cutoffTime);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Daily reset saved to database successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error performing daily reset");
            throw;
        }
    }
}
