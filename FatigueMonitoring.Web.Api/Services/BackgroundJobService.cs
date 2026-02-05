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
    /// Hour of day (0-23) when Mining data resets (default: 5 = 5:00 AM)
    /// </summary>
    public int MiningResetHour { get; set; } = 5;
    
    /// <summary>
    /// Hour of day (0-23) when Hauling data resets (default: 6 = 6:00 AM)
    /// </summary>
    public int HaulingResetHour { get; set; } = 6;
}

public class BackgroundJobService(
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobSettings> settings,
    ILogger<BackgroundJobService> logger) : BackgroundService
{
    private readonly BackgroundJobSettings _settings = settings.Value;
    private DateTime? _lastMiningResetDate = null;
    private DateTime? _lastHaulingResetDate = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Background job service starting. Fetch interval: {Interval}s",
            _settings.DataFetchIntervalSeconds);
        logger.LogInformation("Daily reset hours - Mining: {MiningHour}:00, Hauling: {HaulingHour}:00",
            _settings.MiningResetHour, _settings.HaulingResetHour);

        // Initial delay to let the application start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Check if daily reset is needed for each area
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
    /// Check if it's time for daily reset for each area and perform reset if needed.
    /// Mining resets at MiningResetHour (05:00), Hauling resets at HaulingResetHour (06:00).
    /// </summary>
    private async Task CheckAndPerformDailyResetAsync(CancellationToken cancellationToken)
    {
        // Current time in WIB (UTC+7)
        var nowWib = DateTime.UtcNow.AddHours(7);
        var currentHour = nowWib.Hour;
        var todayDate = nowWib.Date;
        
        // Check Mining reset (05:00)
        if (currentHour >= _settings.MiningResetHour)
        {
            if (!_lastMiningResetDate.HasValue || _lastMiningResetDate.Value.Date != todayDate)
            {
                logger.LogInformation("========================================");
                logger.LogInformation("MINING DAILY RESET TRIGGERED at {Time:yyyy-MM-dd HH:mm:ss} WIB", nowWib);
                logger.LogInformation("========================================");
                
                await PerformAreaResetAsync("Mining", _settings.MiningResetHour, nowWib, cancellationToken);
                _lastMiningResetDate = todayDate;
                
                logger.LogInformation("Mining reset completed. Next reset will be tomorrow at {Hour}:00", _settings.MiningResetHour);
            }
        }
        
        // Check Hauling reset (06:00)
        if (currentHour >= _settings.HaulingResetHour)
        {
            if (!_lastHaulingResetDate.HasValue || _lastHaulingResetDate.Value.Date != todayDate)
            {
                logger.LogInformation("========================================");
                logger.LogInformation("HAULING DAILY RESET TRIGGERED at {Time:yyyy-MM-dd HH:mm:ss} WIB", nowWib);
                logger.LogInformation("========================================");
                
                await PerformAreaResetAsync("Hauling", _settings.HaulingResetHour, nowWib, cancellationToken);
                _lastHaulingResetDate = todayDate;
                
                logger.LogInformation("Hauling reset completed. Next reset will be tomorrow at {Hour}:00", _settings.HaulingResetHour);
            }
        }
    }

    /// <summary>
    /// Perform the daily reset for a specific area - clear events and aggregation data for that area.
    /// </summary>
    private async Task PerformAreaResetAsync(string area, int resetHour, DateTime resetTime, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();
        
        try
        {
            var cutoffTime = resetTime.Date.AddHours(resetHour);
            
            // 1. Clear old fatigue events for this area (events before reset hour)
            var oldEvents = await dbContext.FatigueEvents
                .Where(e => e.Area == area && e.EventTime < cutoffTime)
                .ToListAsync(cancellationToken);
            dbContext.FatigueEvents.RemoveRange(oldEvents);
            logger.LogInformation("[{Area}] Cleared {Count} old fatigue events (before {CutoffTime:yyyy-MM-dd HH:mm:ss})", 
                area, oldEvents.Count, cutoffTime);
            
            // 2. Clear area distributions for this area
            var distributions = await dbContext.AreaDistributions
                .Where(d => d.Area == area)
                .ToListAsync(cancellationToken);
            dbContext.AreaDistributions.RemoveRange(distributions);
            logger.LogInformation("[{Area}] Cleared {Count} area distributions", area, distributions.Count);
            
            // 3. Clear active alerts for this area
            var activeAlerts = await dbContext.ActiveAlerts
                .Where(a => a.Area == area)
                .ToListAsync(cancellationToken);
            dbContext.ActiveAlerts.RemoveRange(activeAlerts);
            logger.LogInformation("[{Area}] Cleared {Count} active alerts", area, activeAlerts.Count);
            
            // 4. Clear delayed follow-ups for this area
            var delayedFollowUps = await dbContext.DelayedFollowUps
                .Where(d => d.Area == area)
                .ToListAsync(cancellationToken);
            dbContext.DelayedFollowUps.RemoveRange(delayedFollowUps);
            logger.LogInformation("[{Area}] Cleared {Count} delayed follow-ups", area, delayedFollowUps.Count);
            
            // 5. Clear recurrent units for this area
            var recurrentUnits = await dbContext.RecurrentUnits
                .Where(r => r.PrimaryArea == area)
                .ToListAsync(cancellationToken);
            dbContext.RecurrentUnits.RemoveRange(recurrentUnits);
            logger.LogInformation("[{Area}] Cleared {Count} recurrent units", area, recurrentUnits.Count);
            
            // 6. Clear high risk areas for this area
            var highRiskAreas = await dbContext.HighRiskAreas
                .Where(h => h.Area == area)
                .ToListAsync(cancellationToken);
            dbContext.HighRiskAreas.RemoveRange(highRiskAreas);
            logger.LogInformation("[{Area}] Cleared {Count} high risk areas", area, highRiskAreas.Count);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("[{Area}] Daily reset saved to database successfully", area);
            
            // Note: Dashboard summaries will be recalculated in the next aggregation cycle
            // We don't clear them here because they contain combined data for all areas
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[{Area}] Error performing daily reset", area);
            throw;
        }
    }
}
