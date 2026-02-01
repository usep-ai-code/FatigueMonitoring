using Microsoft.EntityFrameworkCore;
using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Models;

namespace FatigueMonitoring.Web.Api.Services;

public class DataAggregationService(
    ApplicationDbContext context, 
    ExternalApiService externalApiService,
    IConfiguration configuration,
    ILogger<DataAggregationService> logger)
{
    private const string SYNC_KEY = "EventsSync";
    
    public async Task AggregateDataAsync()
    {
        logger.LogInformation("Starting data aggregation at {Time}", DateTime.UtcNow);

        try
        {
            // 1. Get last sync time
            var syncMetadata = await GetOrCreateSyncMetadataAsync();
            
            // Update status to Running
            syncMetadata.Status = "Running";
            syncMetadata.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // 2. Calculate time range (last 3 minutes from last sync)
            var startDate = syncMetadata.LastSyncTime;
            var endDate = startDate.AddMinutes(3);
            
            // Don't fetch future data
            if (endDate > DateTime.UtcNow)
            {
                endDate = DateTime.UtcNow;
            }

            logger.LogInformation("Fetching events from {StartDate} to {EndDate}", startDate, endDate);

            // 3. Fetch raw data from external API
            var allEvents = await externalApiService.GetEventsAsync(startDate, endDate);
            
            logger.LogInformation("Fetched {Count} events from external API", allEvents.Count);
            
            if (allEvents.Count > 0)
            {
                await SaveRawEventsAsync(allEvents);
            }

            // 4. Aggregate data into AI_ tables
            await AggregateActiveAlertsAsync();
            await AggregateDashboardStatsAsync();
            await AggregateAreaDistributionAsync();
            await AggregateRecurrentUnitsAsync();
            await AggregateHighRiskAreasAsync();

            // 5. Update sync metadata
            syncMetadata.LastSyncTime = endDate;
            syncMetadata.NextSyncTime = endDate.AddMinutes(3);
            syncMetadata.Status = "Success";
            syncMetadata.ErrorMessage = null;
            syncMetadata.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            logger.LogInformation("Data aggregation completed successfully. Next sync at: {NextSync}", syncMetadata.NextSyncTime);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during data aggregation");
            
            // Update sync metadata with error
            var syncMetadata = await context.SyncMetadata
                .FirstOrDefaultAsync(s => s.SyncKey == SYNC_KEY);
            
            if (syncMetadata != null)
            {
                syncMetadata.Status = "Failed";
                syncMetadata.ErrorMessage = ex.Message;
                syncMetadata.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync();
            }
            
            throw;
        }
    }

    private async Task<SyncMetadata> GetOrCreateSyncMetadataAsync()
    {
        var syncMetadata = await context.SyncMetadata
            .FirstOrDefaultAsync(s => s.SyncKey == SYNC_KEY);

        if (syncMetadata == null)
        {
            // Get initial start time from configuration
            var initialStartTimeStr = configuration["BackgroundJob:InitialStartTime"] ?? "2026-02-01 00:00:00";
            
            if (!DateTime.TryParse(initialStartTimeStr, out var initialStartTime))
            {
                initialStartTime = DateTime.UtcNow.AddDays(-1); // Default to 1 day ago
                logger.LogWarning("Invalid InitialStartTime in configuration. Using default: {DefaultTime}", initialStartTime);
            }
            else
            {
                // Convert to UTC if not already
                if (initialStartTime.Kind == DateTimeKind.Unspecified)
                {
                    initialStartTime = DateTime.SpecifyKind(initialStartTime, DateTimeKind.Utc);
                }
                else if (initialStartTime.Kind == DateTimeKind.Local)
                {
                    initialStartTime = initialStartTime.ToUniversalTime();
                }
            }

            syncMetadata = new SyncMetadata
            {
                SyncKey = SYNC_KEY,
                LastSyncTime = initialStartTime,
                NextSyncTime = initialStartTime.AddMinutes(3),
                Status = "Initialized",
                UpdatedAt = DateTime.UtcNow
            };

            context.SyncMetadata.Add(syncMetadata);
            await context.SaveChangesAsync();
            
            logger.LogInformation("Created initial sync metadata with start time: {StartTime}", initialStartTime);
        }

        return syncMetadata;
    }

    private async Task SaveRawEventsAsync(List<EventItem> events)
    {
        foreach (var evt in events)
        {
            if (string.IsNullOrEmpty(evt.Id)) continue;

            var existing = await context.RawEvents.FindAsync(evt.Id);
            if (existing == null)
            {
                var rawEvent = new RawEvent(
                    evt.Id,
                    evt.Identity ?? "",
                    evt.Name ?? "",
                    evt.Alarm_Type ?? "")
                {
                    Time = DateTime.TryParse(evt.Time, out var time) ? time : DateTime.UtcNow,
                    ServerTime = DateTime.TryParse(evt.Server_Time, out var serverTime) ? serverTime : DateTime.UtcNow,
                    Shift = evt.Shift,
                    ShiftDate = DateTime.TryParse(evt.Shift_Date, out var shiftDate) ? shiftDate : null,
                    Level = evt.Level,
                    Speed = evt.Speed,
                    IsFollowedUp = evt.Is_Followed_Up,
                    Latitude = evt.Latitude,
                    Longitude = evt.Longitude,
                    GeofenceId = evt.Geofence_Id,
                    DeviceId = evt.Device_Id,
                    DriverId = evt.Driver_Id,
                    ManualVerificationBy = evt.Manual_Verification_By,
                    ManualVerificationTime = DateTime.TryParse(evt.Manual_Verification_Time, out var verTime) ? verTime : null,
                    ManualVerificationMemo = evt.Manual_Verification_Memo,
                    TakeType = evt.TakeType,
                    ManualVerificationWaitingDuration = evt.Manual_Verification_Waiting_Duration,
                    Satellites = evt.Satellites,
                    UploadAt = DateTime.TryParse(evt.Upload_At, out var uploadAt) ? uploadAt : DateTime.UtcNow,
                    UpdatedAt = DateTime.TryParse(evt.Updated_At, out var updatedAt) ? updatedAt : DateTime.UtcNow,
                    DeviceImei = evt.Device?.Imei,
                    DeviceName = evt.Device?.Name,
                    DeviceGroupName = evt.Device?.Group_Name,
                    DriverName = evt.Driver?.Name,
                    GeofenceName = evt.Geofence?.Name
                };

                context.RawEvents.Add(rawEvent);
            }
            else
            {
                // Update follow-up status if changed
                existing.IsFollowedUp = evt.Is_Followed_Up;
                existing.UpdatedAt = DateTime.UtcNow;
            }
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Saved {Count} raw events", events.Count);
    }

    private async Task AggregateActiveAlertsAsync()
    {
        // Clear existing data
        context.AI_ActiveAlert_T.RemoveRange(context.AI_ActiveAlert_T);

        var today = DateTime.UtcNow.Date;
        var openEvents = await context.RawEvents
            .Where(e => !e.IsFollowedUp && e.Time >= today)
            .OrderByDescending(e => e.Time)
            .ToListAsync();

        var alerts = new List<AI_ActiveAlert_T>();

        foreach (var evt in openEvents)
        {
            var area = DetermineArea(evt.DeviceGroupName ?? "");
            var location = DetermineLocation(evt.DeviceGroupName ?? "", evt.GeofenceName);
            
            // Count events for this unit today
            var unitEventCount = await context.RawEvents
                .CountAsync(e => e.DeviceId == evt.DeviceId && e.Time >= today);

            var openDuration = (int)(DateTime.UtcNow - evt.Time).TotalMinutes;

            alerts.Add(new AI_ActiveAlert_T
            {
                EventId = evt.Id,
                Unit = evt.DeviceName ?? "Unknown",
                Operator = evt.DriverName ?? "Unknown",
                Type = evt.Name,
                Area = area,
                Location = location,
                Time = evt.Time,
                Status = "Open",
                Speed = evt.Speed,
                Count = unitEventCount,
                Latitude = evt.Latitude,
                Longitude = evt.Longitude,
                OpenDurationMinutes = openDuration
            });
        }

        context.AI_ActiveAlert_T.AddRange(alerts);
        await context.SaveChangesAsync();
        logger.LogInformation("Aggregated {Count} active alerts", alerts.Count);
    }

    private async Task AggregateDashboardStatsAsync()
    {
        // Clear existing data
        context.AI_DashboardStats_T.RemoveRange(context.AI_DashboardStats_T);

        var today = DateTime.UtcNow.Date;
        var todayEvents = await context.RawEvents
            .Where(e => e.Time >= today)
            .ToListAsync();

        // All stats
        var allStats = new AI_DashboardStats_T
        {
            Area = "All",
            TotalAlarms = todayEvents.Count,
            FollowedUp = todayEvents.Count(e => e.IsFollowedUp),
            WaitingFollowUp = todayEvents.Count(e => !e.IsFollowedUp)
        };

        // Mining stats
        var miningEvents = todayEvents.Where(e => DetermineArea(e.DeviceGroupName ?? "") == "Mining").ToList();
        var miningStats = new AI_DashboardStats_T
        {
            Area = "Mining",
            TotalAlarms = miningEvents.Count,
            FollowedUp = miningEvents.Count(e => e.IsFollowedUp),
            WaitingFollowUp = miningEvents.Count(e => !e.IsFollowedUp)
        };

        // Hauling stats
        var haulingEvents = todayEvents.Where(e => DetermineArea(e.DeviceGroupName ?? "") == "Hauling").ToList();
        var haulingStats = new AI_DashboardStats_T
        {
            Area = "Hauling",
            TotalAlarms = haulingEvents.Count,
            FollowedUp = haulingEvents.Count(e => e.IsFollowedUp),
            WaitingFollowUp = haulingEvents.Count(e => !e.IsFollowedUp)
        };

        context.AI_DashboardStats_T.AddRange([allStats, miningStats, haulingStats]);
        await context.SaveChangesAsync();
        logger.LogInformation("Aggregated dashboard stats");
    }

    private async Task AggregateAreaDistributionAsync()
    {
        // Clear existing data
        context.AI_AreaDistribution_T.RemoveRange(context.AI_AreaDistribution_T);

        var today = DateTime.UtcNow.Date;
        var openEvents = await context.RawEvents
            .Where(e => !e.IsFollowedUp && e.Time >= today)
            .ToListAsync();

        var distributions = openEvents
            .GroupBy(e => new { Area = DetermineArea(e.DeviceGroupName ?? ""), Location = DetermineLocation(e.DeviceGroupName ?? "", e.GeofenceName) })
            .Select(g => new AI_AreaDistribution_T
            {
                Area = g.Key.Area,
                Location = g.Key.Location,
                AlertCount = g.Count()
            })
            .ToList();

        context.AI_AreaDistribution_T.AddRange(distributions);
        await context.SaveChangesAsync();
        logger.LogInformation("Aggregated {Count} area distributions", distributions.Count);
    }

    private async Task AggregateRecurrentUnitsAsync()
    {
        // Clear existing data
        context.AI_RecurrentUnit_T.RemoveRange(context.AI_RecurrentUnit_T);

        var last7Days = DateTime.UtcNow.AddDays(-7);
        var recurrentUnits = await context.RawEvents
            .Where(e => e.Time >= last7Days)
            .GroupBy(e => new { e.DeviceId, e.DeviceName, e.DriverName })
            .Select(g => new
            {
                g.Key.DeviceId,
                g.Key.DeviceName,
                g.Key.DriverName,
                EventCount = g.Count(),
                Area = g.First().DeviceGroupName
            })
            .Where(x => x.EventCount > 1)
            .OrderByDescending(x => x.EventCount)
            .ToListAsync();

        var units = recurrentUnits.Select(u => new AI_RecurrentUnit_T
        {
            Unit = u.DeviceName ?? "Unknown",
            OperatorName = u.DriverName ?? "Unknown",
            Area = DetermineArea(u.Area ?? ""),
            EventCount = u.EventCount,
            Status = "Active"
        }).ToList();

        context.AI_RecurrentUnit_T.AddRange(units);
        await context.SaveChangesAsync();
        logger.LogInformation("Aggregated {Count} recurrent units", units.Count);
    }

    private async Task AggregateHighRiskAreasAsync()
    {
        // Clear existing data
        context.AI_HighRiskArea_T.RemoveRange(context.AI_HighRiskArea_T);

        var last7Days = DateTime.UtcNow.AddDays(-7);
        var riskAreas = await context.RawEvents
            .Where(e => e.Time >= last7Days)
            .ToListAsync();

        var areas = riskAreas
            .GroupBy(e => new { Area = DetermineArea(e.DeviceGroupName ?? ""), Location = DetermineLocation(e.DeviceGroupName ?? "", e.GeofenceName) })
            .Select(g => new AI_HighRiskArea_T
            {
                Location = g.Key.Location,
                Area = g.Key.Area,
                EventCount = g.Count()
            })
            .OrderByDescending(x => x.EventCount)
            .ToList();

        context.AI_HighRiskArea_T.AddRange(areas);
        await context.SaveChangesAsync();
        logger.LogInformation("Aggregated {Count} high risk areas", areas.Count);
    }

    private static string DetermineArea(string groupName)
    {
        // Determine if Mining or Hauling based on group name
        // Mining: IPD, Kerinci, CSA (haulers at pit), etc.
        // Hauling: HD, DT (dump trucks on road)
        
        if (string.IsNullOrEmpty(groupName))
            return "Mining";

        groupName = groupName.ToUpper();
        
        // Mining indicators
        if (groupName.Contains("IPD") || 
            groupName.Contains("KERINCI") || 
            groupName.Contains("PIT") ||
            groupName.Contains("FRONT") ||
            groupName.StartsWith("D3-") ||
            groupName.StartsWith("EX-"))
            return "Mining";

        // Hauling indicators  
        if (groupName.Contains("CSA") ||
            groupName.Contains("KM") ||
            groupName.StartsWith("HD-") ||
            groupName.StartsWith("H"))
            return "Hauling";

        return "Mining"; // Default
    }

    private static string DetermineLocation(string groupName, string? geofenceName)
    {
        if (!string.IsNullOrEmpty(geofenceName))
            return geofenceName;

        var area = DetermineArea(groupName);
        
        if (area == "Mining")
        {
            // Extract pit/front info
            if (groupName.Contains("IPD"))
                return $"IPD {groupName.Split(' ').LastOrDefault() ?? ""}".Trim();
            if (groupName.Contains("KERINCI"))
                return "Kerinci-Riau";
            return "Pit Area";
        }
        else
        {
            // For hauling, use KM designation or group name
            if (groupName.Contains("CSA"))
                return groupName;
            return "Hauling Route";
        }
    }
}
