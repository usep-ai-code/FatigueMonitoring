using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.DTOs;
using FatigueMonitoring.Web.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Services;

public interface IDataAggregationService
{
    Task FetchAndProcessEventsAsync(CancellationToken cancellationToken = default);
    Task CalculateAggregationsAsync(CancellationToken cancellationToken = default);
    Task<DashboardDataDto> GetDashboardDataAsync(string areaFilter = "All", CancellationToken cancellationToken = default);
}

public class DataAggregationService(
    IExternalApiService externalApiService,
    IServiceScopeFactory scopeFactory,
    IOptions<BackgroundJobSettings> jobSettings,
    ILogger<DataAggregationService> logger) : IDataAggregationService
{
    private const int DelayThresholdMinutes = 30;
    private const string SyncTypeExternalApi = "ExternalApiEvents";
    private readonly BackgroundJobSettings _jobSettings = jobSettings.Value;

    public async Task FetchAndProcessEventsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting data fetch from external API");

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();

        try
        {
            // Get or create sync state
            var (syncState, isFirstSync) = await GetOrCreateSyncStateAsync(dbContext, cancellationToken);
            
            // Mark sync as in progress
            syncState.LastSyncStatus = "InProgress";
            syncState.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            // Current time in WIB (UTC+7)
            var nowWib = DateTime.UtcNow.AddHours(7);
            
            // Calculate date range
            var startDate = syncState.LastSyncTime;
            DateTime endDate;
            
            if (isFirstSync)
            {
                // First sync: fetch from InitialStartTime to NOW
                endDate = nowWib;
                logger.LogInformation("First sync detected. Fetching all data from {StartDate} to {EndDate} (WIB)",
                    startDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    endDate.ToString("yyyy-MM-dd HH:mm:ss"));
            }
            else
            {
                // Subsequent syncs: fetch from last sync time + window minutes
                endDate = startDate.AddMinutes(_jobSettings.FetchWindowMinutes);
                
                // Don't fetch future data - cap at current time
                if (endDate > nowWib)
                {
                    endDate = nowWib;
                }
            }

            // If start date is already at or past current time, skip this fetch
            if (startDate >= nowWib)
            {
                logger.LogInformation("Already caught up to current time. No new data to fetch.");
                syncState.LastSyncStatus = "Success";
                syncState.LastSyncError = null;
                syncState.UpdatedAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }

            logger.LogInformation("Fetching events from {StartDate} to {EndDate} (WIB)", 
                startDate.ToString("yyyy-MM-dd HH:mm:ss"), 
                endDate.ToString("yyyy-MM-dd HH:mm:ss"));

            // Get all true alarms in the time window
            var allEvents = new List<EventData>();
            int page = 1;
            int totalPages = 1;

            do
            {
                var response = await externalApiService.GetEventsAsync(
                    startDate, endDate, page, 100, 
                    "manual_verification_is_true_alarm,level", "true|3",
                    cancellationToken);

                if (response?.Success == true && response.Data?.List != null)
                {
                    allEvents.AddRange(response.Data.List);
                    totalPages = response.Data.Pagination.TotalPages;
                    page++;
                }
                else
                {
                    if (response?.Success != true)
                    {
                        logger.LogWarning("API returned unsuccessful response: {Message}", response?.Message);
                    }
                    break;
                }
            } while (page <= totalPages && page <= 20); // Limit to 20 pages max

            logger.LogInformation("Fetched {Count} events from external API for period {StartDate} to {EndDate}", 
                allEvents.Count, startDate.ToString("yyyy-MM-dd HH:mm:ss"), endDate.ToString("yyyy-MM-dd HH:mm:ss"));

            // Process and store events
            foreach (var eventData in allEvents)
            {
                await ProcessEventAsync(dbContext, eventData, cancellationToken);
            }

            // Update sync state with new last sync time
            syncState.LastSyncTime = endDate;
            syncState.LastSyncRecordCount = allEvents.Count;
            syncState.LastSyncStatus = "Success";
            syncState.LastSyncError = null;
            syncState.UpdatedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Successfully processed {Count} events. Next sync will start from {NextStart}", 
                allEvents.Count, endDate.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching and processing events");
            
            // Update sync state with error
            try
            {
                var syncState = await dbContext.SyncStates
                    .FirstOrDefaultAsync(s => s.SyncType == SyncTypeExternalApi, cancellationToken);
                
                if (syncState != null)
                {
                    syncState.LastSyncStatus = "Failed";
                    syncState.LastSyncError = ex.Message;
                    syncState.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
            }
            catch (Exception innerEx)
            {
                logger.LogError(innerEx, "Failed to update sync state with error");
            }
        }
    }

    private async Task<(AI_SyncState_T syncState, bool isFirstSync)> GetOrCreateSyncStateAsync(
        FatigueMonitoringDbContext dbContext, 
        CancellationToken cancellationToken)
    {
        var syncState = await dbContext.SyncStates
            .FirstOrDefaultAsync(s => s.SyncType == SyncTypeExternalApi, cancellationToken);

        bool isFirstSync = false;

        if (syncState == null)
        {
            isFirstSync = true;
            
            // Parse initial start time from settings
            DateTime initialStartTime;
            if (!DateTime.TryParseExact(_jobSettings.InitialStartTime, "yyyy-MM-dd HH:mm:ss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out initialStartTime))
            {
                // Default to 24 hours ago if parsing fails
                initialStartTime = DateTime.UtcNow.AddHours(7).AddHours(-24); // WIB
                logger.LogWarning("Failed to parse InitialStartTime '{InitialStartTime}', using default: {DefaultTime}",
                    _jobSettings.InitialStartTime, initialStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
            }

            syncState = new AI_SyncState_T
            {
                SyncType = SyncTypeExternalApi,
                LastSyncTime = initialStartTime,
                LastSyncRecordCount = 0,
                LastSyncStatus = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.SyncStates.Add(syncState);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Created new sync state with initial start time: {InitialStartTime}", 
                initialStartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        return (syncState, isFirstSync);
    }

    private async Task ProcessEventAsync(FatigueMonitoringDbContext dbContext, EventData eventData, CancellationToken cancellationToken)
    {
        // Check if event already exists
        var existingEvent = await dbContext.FatigueEvents
            .FirstOrDefaultAsync(e => e.ExternalId == eventData.Id, cancellationToken);

        var (area, location) = DetermineAreaAndLocation(eventData);

        // Extract image and video URLs from alarm files
        string? imageUrl = null;
        string? videoUrl = null;
        
        if (eventData.AlarmFile != null)
        {
            foreach (var file in eventData.AlarmFile)
            {
                if (file.DownUrl.Contains(".jpg", StringComparison.OrdinalIgnoreCase) ||
                    file.DownUrl.Contains(".png", StringComparison.OrdinalIgnoreCase) ||
                    file.DownUrl.Contains(".jpeg", StringComparison.OrdinalIgnoreCase))
                {
                    imageUrl = file.DownUrl;
                }
                else if (file.DownUrl.Contains(".mp4", StringComparison.OrdinalIgnoreCase) ||
                         file.DownUrl.Contains(".avi", StringComparison.OrdinalIgnoreCase))
                {
                    videoUrl = file.DownUrl;
                }
            }
        }

        if (existingEvent == null)
        {
            // Create new event
            var newEvent = new AI_FatigueEvent_T
            {
                Id = Guid.NewGuid(),
                ExternalId = eventData.Id,
                Identity = eventData.Identity ?? string.Empty,
                AlarmName = eventData.Name ?? "Unknown",
                AlarmType = eventData.AlarmType ?? string.Empty,
                EventTime = ParseDateTime(eventData.Time),
                ServerTime = ParseDateTime(eventData.ServerTime),
                Shift = eventData.Shift ?? string.Empty,
                ShiftDate = ParseDateTime(eventData.ShiftDate),
                Level = eventData.Level ?? 0,
                Speed = eventData.Speed ?? 0,
                IsFollowedUp = eventData.IsFollowedUp,
                Latitude = eventData.Latitude ?? 0,
                Longitude = eventData.Longitude ?? 0,
                GeofenceId = eventData.GeofenceId,
                DeviceId = eventData.DeviceId ?? string.Empty,
                DriverId = eventData.DriverId,
                ManualVerificationBy = eventData.ManualVerificationBy,
                ManualVerificationTime = string.IsNullOrEmpty(eventData.ManualVerificationTime) 
                    ? null : ParseDateTime(eventData.ManualVerificationTime),
                ManualVerificationMemo = eventData.ManualVerificationMemo,
                ManualVerificationWaitingDuration = eventData.ManualVerificationWaitingDuration,
                DeviceImei = eventData.Device?.Imei ?? string.Empty,
                UnitName = eventData.Device?.Name ?? string.Empty,
                GroupName = eventData.Device?.GroupName ?? string.Empty,
                Area = area,
                Location = location,
                ImageUrl = imageUrl,
                VideoUrl = videoUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.FatigueEvents.Add(newEvent);
            logger.LogDebug("Added new event: {ExternalId} - {UnitName}", eventData.Id, eventData.Device?.Name);
        }
        else
        {
            // Update existing event
            existingEvent.IsFollowedUp = eventData.IsFollowedUp;
            existingEvent.ManualVerificationBy = eventData.ManualVerificationBy;
            existingEvent.ManualVerificationTime = string.IsNullOrEmpty(eventData.ManualVerificationTime)
                ? null : ParseDateTime(eventData.ManualVerificationTime);
            existingEvent.ManualVerificationMemo = eventData.ManualVerificationMemo;
            existingEvent.ManualVerificationWaitingDuration = eventData.ManualVerificationWaitingDuration;
            existingEvent.ImageUrl = imageUrl ?? existingEvent.ImageUrl;
            existingEvent.VideoUrl = videoUrl ?? existingEvent.VideoUrl;
            existingEvent.UpdatedAt = DateTime.UtcNow;
            logger.LogDebug("Updated existing event: {ExternalId}", eventData.Id);
        }
    }

    private static (string area, string location) DetermineAreaAndLocation(EventData eventData)
    {
        var groupName = eventData.Device?.GroupName ?? string.Empty;
        var unitName = eventData.Device?.Name ?? string.Empty;
        var latitude = eventData.Latitude ?? 0m;
        var longitude = eventData.Longitude ?? 0m;
        
        // Determine area based on unit prefix or group name
        string area;
        string location;

        if (unitName.StartsWith("H", StringComparison.OrdinalIgnoreCase) || 
            groupName.Contains("CSA", StringComparison.OrdinalIgnoreCase))
        {
            area = "Hauling";
            // Generate KM location based on coordinates or use a default pattern
            var kmValue = Math.Abs((int)(longitude * 10) % 60);
            location = $"KM {kmValue}";
        }
        else if (unitName.StartsWith("D", StringComparison.OrdinalIgnoreCase) ||
                 groupName.Contains("IPD", StringComparison.OrdinalIgnoreCase) ||
                 groupName.Contains("Kerinci", StringComparison.OrdinalIgnoreCase))
        {
            area = "Mining";
            // Generate front/pit location based on coordinates
            var frontLetters = new[] { "A", "B", "C", "D" };
            var frontIndex = Math.Abs((int)(latitude * 10) % frontLetters.Length);
            location = $"Front {frontLetters[frontIndex]}";
        }
        else
        {
            // Default assignment based on coordinates
            area = latitude < -2.2m ? "Hauling" : "Mining";
            location = area == "Mining" ? "Front A" : "KM 10";
        }

        return (area, location);
    }

    private static DateTime ParseDateTime(string dateString)
    {
        if (string.IsNullOrEmpty(dateString))
            return DateTime.UtcNow;

        // Try multiple formats
        if (DateTime.TryParse(dateString, out var result))
            return result;

        // Handle format like "2026-02-01 09:11:58"
        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd HH:mm:ss", 
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out result))
            return result;

        return DateTime.UtcNow;
    }

    public async Task CalculateAggregationsAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting aggregation calculation");

        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();

        var now = DateTime.UtcNow;
        var todayStart = now.Date;

        // Get all events for today
        var todayEvents = await dbContext.FatigueEvents
            .Where(e => e.EventTime >= todayStart)
            .ToListAsync(cancellationToken);

        // Calculate summary
        await CalculateDashboardSummaryAsync(dbContext, todayEvents, cancellationToken);

        // Calculate area distributions
        await CalculateAreaDistributionsAsync(dbContext, todayEvents, cancellationToken);

        // Calculate active alerts
        await CalculateActiveAlertsAsync(dbContext, todayEvents, now, cancellationToken);

        // Calculate delayed follow-ups
        await CalculateDelayedFollowUpsAsync(dbContext, todayEvents, now, cancellationToken);

        // Calculate recurrent units
        await CalculateRecurrentUnitsAsync(dbContext, todayEvents, cancellationToken);

        // Calculate high risk areas
        await CalculateHighRiskAreasAsync(dbContext, todayEvents, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Aggregation calculation completed. Processed {Count} events for today.", todayEvents.Count);
    }

    private async Task CalculateDashboardSummaryAsync(
        FatigueMonitoringDbContext dbContext, 
        List<AI_FatigueEvent_T> events,
        CancellationToken cancellationToken)
    {
        // Clear old summaries
        var oldSummaries = await dbContext.DashboardSummaries.ToListAsync(cancellationToken);
        dbContext.DashboardSummaries.RemoveRange(oldSummaries);

        var miningEvents = events.Where(e => e.Area == "Mining").ToList();
        var haulingEvents = events.Where(e => e.Area == "Hauling").ToList();

        var summary = new AI_DashboardSummary_T
        {
            FilterType = "All",
            TotalAlarms = events.Count,
            FollowedUp = events.Count(e => e.IsFollowedUp),
            WaitingFollowUp = events.Count(e => !e.IsFollowedUp),
            MiningTotal = miningEvents.Count,
            MiningOpen = miningEvents.Count(e => !e.IsFollowedUp),
            MiningResolved = miningEvents.Count(e => e.IsFollowedUp),
            HaulingTotal = haulingEvents.Count,
            HaulingOpen = haulingEvents.Count(e => !e.IsFollowedUp),
            HaulingResolved = haulingEvents.Count(e => e.IsFollowedUp),
            LastCalculatedAt = DateTime.UtcNow
        };

        dbContext.DashboardSummaries.Add(summary);
    }

    private async Task CalculateAreaDistributionsAsync(
        FatigueMonitoringDbContext dbContext,
        List<AI_FatigueEvent_T> events,
        CancellationToken cancellationToken)
    {
        // Clear old distributions
        var oldDistributions = await dbContext.AreaDistributions.ToListAsync(cancellationToken);
        dbContext.AreaDistributions.RemoveRange(oldDistributions);

        var distributions = events
            .Where(e => !e.IsFollowedUp)
            .GroupBy(e => new { e.Area, e.Location })
            .Select(g => new AI_AreaDistribution_T
            {
                Area = g.Key.Area,
                Location = g.Key.Location,
                OpenAlertCount = g.Count(),
                TotalAlertCount = events.Count(e => e.Area == g.Key.Area && e.Location == g.Key.Location),
                LastCalculatedAt = DateTime.UtcNow
            })
            .ToList();

        dbContext.AreaDistributions.AddRange(distributions);
    }

    private async Task CalculateActiveAlertsAsync(
        FatigueMonitoringDbContext dbContext,
        List<AI_FatigueEvent_T> events,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Clear old active alerts
        var oldAlerts = await dbContext.ActiveAlerts.ToListAsync(cancellationToken);
        dbContext.ActiveAlerts.RemoveRange(oldAlerts);

        var openEvents = events.Where(e => !e.IsFollowedUp).ToList();

        foreach (var evt in openEvents)
        {
            var alertCountToday = events.Count(e => e.UnitName == evt.UnitName);
            var openDuration = (int)(now - evt.EventTime).TotalMinutes;

            var alert = new AI_ActiveAlert_T
            {
                FatigueEventId = evt.Id,
                ExternalId = evt.ExternalId,
                UnitName = evt.UnitName,
                OperatorName = evt.ManualVerificationBy ?? "Unknown",
                AlertType = evt.AlarmName,
                Area = evt.Area,
                Location = evt.Location,
                EventTime = evt.EventTime,
                OpenDurationMinutes = Math.Max(0, openDuration),
                Status = "Open",
                Speed = evt.Speed,
                AlertCountToday = alertCountToday,
                ImageUrl = evt.ImageUrl,
                VideoUrl = evt.VideoUrl,
                Latitude = evt.Latitude,
                Longitude = evt.Longitude,
                LastCalculatedAt = DateTime.UtcNow
            };

            dbContext.ActiveAlerts.Add(alert);
        }
    }

    private async Task CalculateDelayedFollowUpsAsync(
        FatigueMonitoringDbContext dbContext,
        List<AI_FatigueEvent_T> events,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Clear old delayed follow-ups
        var oldDelayed = await dbContext.DelayedFollowUps.ToListAsync(cancellationToken);
        dbContext.DelayedFollowUps.RemoveRange(oldDelayed);

        var delayedEvents = events
            .Where(e => !e.IsFollowedUp && (now - e.EventTime).TotalMinutes > DelayThresholdMinutes)
            .ToList();

        foreach (var evt in delayedEvents)
        {
            var delayed = new AI_DelayedFollowUp_T
            {
                FatigueEventId = evt.Id,
                ExternalId = evt.ExternalId,
                UnitName = evt.UnitName,
                OperatorName = evt.ManualVerificationBy ?? "Unknown",
                Area = evt.Area,
                Location = evt.Location,
                EventTime = evt.EventTime,
                DelayMinutes = (int)(now - evt.EventTime).TotalMinutes,
                LastCalculatedAt = DateTime.UtcNow
            };

            dbContext.DelayedFollowUps.Add(delayed);
        }
    }

    private async Task CalculateRecurrentUnitsAsync(
        FatigueMonitoringDbContext dbContext,
        List<AI_FatigueEvent_T> events,
        CancellationToken cancellationToken)
    {
        // Clear old recurrent units
        var oldRecurrent = await dbContext.RecurrentUnits.ToListAsync(cancellationToken);
        dbContext.RecurrentUnits.RemoveRange(oldRecurrent);

        var recurrentUnits = events
            .GroupBy(e => e.UnitName)
            .Where(g => g.Count() > 1)
            .Select(g => new AI_RecurrentUnit_T
            {
                UnitName = g.Key,
                OperatorName = g.First().ManualVerificationBy ?? "Unknown",
                DeviceId = g.First().DeviceId,
                EventCount = g.Count(),
                PrimaryArea = g.GroupBy(e => e.Area)
                    .OrderByDescending(ag => ag.Count())
                    .First().Key,
                Status = g.Count() >= 3 ? "HighRisk" : "Monitoring",
                FromDate = DateTime.UtcNow.Date,
                ToDate = DateTime.UtcNow,
                LastCalculatedAt = DateTime.UtcNow
            })
            .OrderByDescending(r => r.EventCount)
            .ToList();

        dbContext.RecurrentUnits.AddRange(recurrentUnits);
    }

    private async Task CalculateHighRiskAreasAsync(
        FatigueMonitoringDbContext dbContext,
        List<AI_FatigueEvent_T> events,
        CancellationToken cancellationToken)
    {
        // Clear old high risk areas
        var oldHighRisk = await dbContext.HighRiskAreas.ToListAsync(cancellationToken);
        dbContext.HighRiskAreas.RemoveRange(oldHighRisk);

        var highRiskAreas = events
            .GroupBy(e => new { e.Location, e.Area })
            .Select(g => new AI_HighRiskArea_T
            {
                Location = g.Key.Location,
                Area = g.Key.Area,
                EventCount = g.Count(),
                RiskLevel = g.Count() >= 5 ? "Critical" : (g.Count() >= 3 ? "High" : "Normal"),
                FromDate = DateTime.UtcNow.Date,
                ToDate = DateTime.UtcNow,
                LastCalculatedAt = DateTime.UtcNow
            })
            .OrderByDescending(h => h.EventCount)
            .ToList();

        dbContext.HighRiskAreas.AddRange(highRiskAreas);
    }

    public async Task<DashboardDataDto> GetDashboardDataAsync(string areaFilter = "All", CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();

        // Get summary
        var summary = await dbContext.DashboardSummaries.FirstOrDefaultAsync(cancellationToken);

        // Get area distributions
        var distributions = await dbContext.AreaDistributions.ToListAsync(cancellationToken);
        var miningDistribution = distributions
            .Where(d => d.Area == "Mining")
            .Select(d => new AreaDistributionDto(d.Location, d.OpenAlertCount))
            .ToList();
        var haulingDistribution = distributions
            .Where(d => d.Area == "Hauling")
            .Select(d => new AreaDistributionDto(d.Location, d.OpenAlertCount))
            .ToList();

        // Get active alerts
        var activeAlertsQuery = dbContext.ActiveAlerts.AsQueryable();
        if (areaFilter != "All")
        {
            activeAlertsQuery = activeAlertsQuery.Where(a => a.Area == areaFilter);
        }
        var activeAlerts = await activeAlertsQuery
            .OrderByDescending(a => a.EventTime)
            .Select(a => new ActiveAlertDto(
                a.Id,
                a.ExternalId,
                a.UnitName,
                a.OperatorName,
                a.AlertType,
                a.Area,
                a.Location,
                a.EventTime,
                a.EventTime.ToString("HH:mm:ss"),
                a.OpenDurationMinutes,
                a.Status,
                a.Speed,
                a.AlertCountToday,
                a.ImageUrl,
                a.VideoUrl,
                a.Latitude,
                a.Longitude
            ))
            .ToListAsync(cancellationToken);

        // Get delayed follow-ups
        var delayedQuery = dbContext.DelayedFollowUps.AsQueryable();
        if (areaFilter != "All")
        {
            delayedQuery = delayedQuery.Where(d => d.Area == areaFilter);
        }
        var delayed = await delayedQuery
            .OrderByDescending(d => d.DelayMinutes)
            .Select(d => new DelayedFollowUpDto(
                d.Id,
                d.ExternalId,
                d.UnitName,
                d.OperatorName,
                d.Area,
                d.Location,
                d.EventTime,
                d.DelayMinutes
            ))
            .ToListAsync(cancellationToken);

        // Get recurrent units
        var recurrentQuery = dbContext.RecurrentUnits.AsQueryable();
        if (areaFilter != "All")
        {
            recurrentQuery = recurrentQuery.Where(r => r.PrimaryArea == areaFilter);
        }
        var recurrent = await recurrentQuery
            .OrderByDescending(r => r.EventCount)
            .Select(r => new RecurrentUnitDto(
                r.Id,
                r.UnitName,
                r.OperatorName,
                r.EventCount,
                r.PrimaryArea,
                r.Status
            ))
            .ToListAsync(cancellationToken);

        // Get high risk areas
        var highRiskQuery = dbContext.HighRiskAreas.AsQueryable();
        if (areaFilter != "All")
        {
            highRiskQuery = highRiskQuery.Where(h => h.Area == areaFilter);
        }
        var highRisk = await highRiskQuery
            .OrderByDescending(h => h.EventCount)
            .Select(h => new HighRiskAreaDto(
                h.Id,
                h.Location,
                h.Area,
                h.EventCount,
                h.RiskLevel
            ))
            .ToListAsync(cancellationToken);

        // Calculate filtered summary
        SummaryDto filteredSummary;
        if (areaFilter == "All")
        {
            filteredSummary = new SummaryDto(
                summary?.TotalAlarms ?? 0,
                summary?.FollowedUp ?? 0,
                summary?.WaitingFollowUp ?? 0
            );
        }
        else if (areaFilter == "Mining")
        {
            filteredSummary = new SummaryDto(
                summary?.MiningTotal ?? 0,
                summary?.MiningResolved ?? 0,
                summary?.MiningOpen ?? 0
            );
        }
        else
        {
            filteredSummary = new SummaryDto(
                summary?.HaulingTotal ?? 0,
                summary?.HaulingResolved ?? 0,
                summary?.HaulingOpen ?? 0
            );
        }

        var areaSummary = new AreaSummaryDto(
            new AreaStatsDto(summary?.MiningTotal ?? 0, summary?.MiningOpen ?? 0, summary?.MiningResolved ?? 0),
            new AreaStatsDto(summary?.HaulingTotal ?? 0, summary?.HaulingOpen ?? 0, summary?.HaulingResolved ?? 0)
        );

        return new DashboardDataDto(
            filteredSummary,
            areaSummary,
            miningDistribution,
            haulingDistribution,
            activeAlerts,
            delayed,
            recurrent,
            highRisk,
            summary?.LastCalculatedAt ?? DateTime.UtcNow
        );
    }
}
