using System.Globalization;
using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Data.Entities;
using FatigueMonitoring.Web.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Services;

public sealed class AggregationWorker(
    ILogger<AggregationWorker> logger,
    IServiceScopeFactory scopeFactory,
    ExternalApiClient apiClient,
    IOptions<AggregationOptions> options) : BackgroundService
{
    private static readonly TimeZoneInfo JakartaTimeZone = ResolveJakartaTimeZone();
    private readonly AggregationOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Aggregation worker started.");

        await RunAggregationAsync(stoppingToken);

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAggregationAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Aggregation worker stopped.");
        }
    }

    private async Task RunAggregationAsync(CancellationToken cancellationToken)
    {
        var nowLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, JakartaTimeZone);
        var rangeStart = nowLocal.LocalDateTime.AddHours(-_options.RangeHours);
        var rangeEnd = nowLocal.LocalDateTime;

        IReadOnlyList<ExternalEventDto> events;
        try
        {
            events = await apiClient.GetEventsAsync(rangeStart, rangeEnd, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch external events.");
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();

        var mappedEvents = events
            .Where(e => !string.IsNullOrWhiteSpace(e.ExternalId))
            .Select(MapExternalEvent)
            .ToList();

        await UpsertRawEventsAsync(dbContext, mappedEvents, cancellationToken);
        await UpdateAggregateTablesAsync(dbContext, mappedEvents, nowLocal.LocalDateTime, cancellationToken);
    }

    private static async Task UpsertRawEventsAsync(
        DashboardDbContext dbContext,
        IReadOnlyList<ExternalEvent> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return;
        }

        var externalIds = events.Select(e => e.ExternalId).ToArray();
        var existingMap = await dbContext.ExternalEvents
            .Where(e => externalIds.Contains(e.ExternalId))
            .ToDictionaryAsync(e => e.ExternalId, cancellationToken);

        foreach (var incoming in events)
        {
            if (existingMap.TryGetValue(incoming.ExternalId, out var existing))
            {
                existing.Identity = incoming.Identity;
                existing.Name = incoming.Name;
                existing.AlarmType = incoming.AlarmType;
                existing.DeviceTime = incoming.DeviceTime;
                existing.ServerTime = incoming.ServerTime;
                existing.Shift = incoming.Shift;
                existing.ShiftDate = incoming.ShiftDate;
                existing.Level = incoming.Level;
                existing.SpeedKph = incoming.SpeedKph;
                existing.IsFollowedUp = incoming.IsFollowedUp;
                existing.Latitude = incoming.Latitude;
                existing.Longitude = incoming.Longitude;
                existing.GeofenceId = incoming.GeofenceId;
                existing.DeviceId = incoming.DeviceId;
                existing.DeviceName = incoming.DeviceName;
                existing.DeviceGroupName = incoming.DeviceGroupName;
                existing.ManualVerificationBy = incoming.ManualVerificationBy;
                existing.ManualVerificationTime = incoming.ManualVerificationTime;
                existing.ManualVerificationMemo = incoming.ManualVerificationMemo;
                existing.ManualVerificationWaitingDuration = incoming.ManualVerificationWaitingDuration;
                existing.UploadAt = incoming.UploadAt;
                existing.UpdatedAt = incoming.UpdatedAt;
                existing.Area = incoming.Area;
                existing.LocationLabel = incoming.LocationLabel;
                existing.OperatorName = incoming.OperatorName;
            }
            else
            {
                dbContext.ExternalEvents.Add(incoming);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateAggregateTablesAsync(
        DashboardDbContext dbContext,
        IReadOnlyList<ExternalEvent> events,
        DateTime snapshotAt,
        CancellationToken cancellationToken)
    {
        var activeEvents = events.Where(e => !e.IsFollowedUp).ToList();

        var unitCounts = events
            .Where(e => !string.IsNullOrWhiteSpace(e.DeviceName))
            .GroupBy(e => e.DeviceName!)
            .ToDictionary(group => group.Key, group => group.Count());

        var activeAlerts = activeEvents.Select(e => CreateActiveAlert(e, snapshotAt, unitCounts)).ToList();
        var delayedAlerts = activeAlerts
            .Where(a => a.OpenMinutes > _options.DelayMinutesThreshold)
            .Select(a => new AiDelayedAlert
            {
                AlarmId = a.AlarmId,
                Unit = a.Unit,
                Operator = a.Operator,
                Area = a.Area,
                Location = a.Location,
                OpenedAt = a.OpenedAt,
                OpenMinutes = a.OpenMinutes,
                SpeedKph = a.SpeedKph,
                AlarmType = a.AlarmType,
                Latitude = a.Latitude,
                Longitude = a.Longitude,
                Status = a.Status,
                EventCount = a.EventCount,
                SnapshotAt = snapshotAt
            })
            .ToList();

        var kpis = BuildKpis(events, snapshotAt);
        var areaDistribution = BuildAreaDistribution(activeAlerts, snapshotAt);
        var recurrentUnits = BuildRecurrentUnits(events, snapshotAt);
        var highRiskAreas = BuildHighRiskAreas(events, snapshotAt);
        var deviceHealth = BuildDeviceHealth(events, snapshotAt);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.AiKpis.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiActiveAlerts.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiAreaDistributions.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiDelayedAlerts.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiRecurrentUnits.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiHighRiskAreas.ExecuteDeleteAsync(cancellationToken);
        await dbContext.AiDeviceHealth.ExecuteDeleteAsync(cancellationToken);

        dbContext.AiKpis.AddRange(kpis);
        dbContext.AiActiveAlerts.AddRange(activeAlerts);
        dbContext.AiAreaDistributions.AddRange(areaDistribution);
        dbContext.AiDelayedAlerts.AddRange(delayedAlerts);
        dbContext.AiRecurrentUnits.AddRange(recurrentUnits);
        dbContext.AiHighRiskAreas.AddRange(highRiskAreas);
        if (deviceHealth is not null)
        {
            dbContext.AiDeviceHealth.Add(deviceHealth);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static List<AiKpi> BuildKpis(IReadOnlyList<ExternalEvent> events, DateTime snapshotAt)
    {
        var areas = new[] { "All", "Mining", "Hauling" };
        var results = new List<AiKpi>();

        foreach (var area in areas)
        {
            var areaEvents = area == "All"
                ? events
                : events.Where(e => string.Equals(e.Area, area, StringComparison.OrdinalIgnoreCase)).ToList();

            results.Add(new AiKpi
            {
                Area = area,
                TotalAlarms = areaEvents.Count,
                FollowedUp = areaEvents.Count(e => e.IsFollowedUp),
                WaitingFollowUp = areaEvents.Count(e => !e.IsFollowedUp),
                SnapshotAt = snapshotAt
            });
        }

        return results;
    }

    private static List<AiAreaDistribution> BuildAreaDistribution(
        IReadOnlyList<AiActiveAlert> activeAlerts,
        DateTime snapshotAt)
        => activeAlerts
            .GroupBy(a => new { a.Area, a.Location })
            .Select(group => new AiAreaDistribution
            {
                Area = group.Key.Area,
                Location = group.Key.Location,
                OpenCount = group.Count(),
                SnapshotAt = snapshotAt
            })
            .ToList();

    private static List<AiRecurrentUnit> BuildRecurrentUnits(
        IReadOnlyList<ExternalEvent> events,
        DateTime snapshotAt)
        => events
            .GroupBy(e => new { Unit = e.DeviceName ?? "Unknown", Operator = e.OperatorName ?? "Unknown", Area = e.Area ?? "Unknown" })
            .OrderByDescending(group => group.Count())
            .Select(group => new AiRecurrentUnit
            {
                Unit = group.Key.Unit,
                Operator = group.Key.Operator,
                Area = group.Key.Area,
                EventsCount = group.Count(),
                SnapshotAt = snapshotAt
            })
            .ToList();

    private static List<AiHighRiskArea> BuildHighRiskAreas(
        IReadOnlyList<ExternalEvent> events,
        DateTime snapshotAt)
        => events
            .GroupBy(e => new { Area = e.Area ?? "Unknown", Location = e.LocationLabel ?? "Unknown" })
            .OrderByDescending(group => group.Count())
            .Select(group => new AiHighRiskArea
            {
                Area = group.Key.Area,
                Location = group.Key.Location,
                EventsCount = group.Count(),
                SnapshotAt = snapshotAt
            })
            .ToList();

    private static AiDeviceHealth? BuildDeviceHealth(IReadOnlyList<ExternalEvent> events, DateTime snapshotAt)
    {
        var totalDevices = events
            .Select(e => e.DeviceName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (totalDevices == 0)
        {
            return new AiDeviceHealth
            {
                TotalDevices = 0,
                OnlineDevices = 0,
                OfflineDevices = 0,
                Coverage = 0,
                SnapshotAt = snapshotAt
            };
        }

        var onlineDevices = totalDevices;
        var offlineDevices = 0;
        var coverage = Math.Round(onlineDevices * 100m / totalDevices, 2, MidpointRounding.AwayFromZero);

        return new AiDeviceHealth
        {
            TotalDevices = totalDevices,
            OnlineDevices = onlineDevices,
            OfflineDevices = offlineDevices,
            Coverage = coverage,
            SnapshotAt = snapshotAt
        };
    }

    private static AiActiveAlert CreateActiveAlert(
        ExternalEvent @event,
        DateTime snapshotAt,
        IReadOnlyDictionary<string, int> unitCounts)
    {
        var openMinutes = 0;
        if (@event.DeviceTime.HasValue)
        {
            var diff = snapshotAt - @event.DeviceTime.Value;
            openMinutes = Math.Max(0, (int)Math.Floor(diff.TotalMinutes));
        }

        var unit = @event.DeviceName ?? "Unknown";
        var eventCount = unitCounts.TryGetValue(unit, out var count) ? count : 1;

        return new AiActiveAlert
        {
            AlarmId = @event.ExternalId,
            Unit = unit,
            Operator = @event.OperatorName ?? "Unknown",
            Area = @event.Area ?? "Unknown",
            Location = @event.LocationLabel ?? "Unknown",
            OpenedAt = @event.DeviceTime,
            OpenMinutes = openMinutes,
            SpeedKph = @event.SpeedKph,
            AlarmType = @event.Name ?? @event.AlarmType ?? "Fatigue",
            Latitude = @event.Latitude,
            Longitude = @event.Longitude,
            Status = @event.IsFollowedUp ? "Followed Up" : "Open",
            EventCount = eventCount,
            SnapshotAt = snapshotAt
        };
    }

    private static ExternalEvent MapExternalEvent(ExternalEventDto dto)
    {
        var area = ResolveArea(dto.DeviceName, dto.DeviceGroupName);
        var location = ResolveLocation(dto.DeviceGroupName, dto.Latitude, dto.Longitude);
        var operatorName = ResolveOperator(dto);

        return new ExternalEvent
        {
            ExternalId = dto.ExternalId,
            Identity = dto.Identity,
            Name = dto.Name,
            AlarmType = dto.AlarmType,
            DeviceTime = dto.DeviceTime,
            ServerTime = dto.ServerTime,
            Shift = dto.Shift,
            ShiftDate = dto.ShiftDate,
            Level = dto.Level,
            SpeedKph = dto.SpeedKph,
            IsFollowedUp = dto.IsFollowedUp,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            GeofenceId = dto.GeofenceId,
            DeviceId = dto.DeviceId,
            DeviceName = dto.DeviceName,
            DeviceGroupName = dto.DeviceGroupName,
            ManualVerificationBy = dto.ManualVerificationBy,
            ManualVerificationTime = dto.ManualVerificationTime,
            ManualVerificationMemo = dto.ManualVerificationMemo,
            ManualVerificationWaitingDuration = dto.ManualVerificationWaitingDuration,
            UploadAt = dto.UploadAt,
            UpdatedAt = dto.UpdatedAt,
            Area = area,
            LocationLabel = location,
            OperatorName = operatorName
        };
    }

    private static string ResolveOperator(ExternalEventDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.ManualVerificationBy))
        {
            return dto.ManualVerificationBy;
        }

        return "Unknown";
    }

    private static string ResolveArea(string? deviceName, string? groupName)
    {
        var name = (deviceName ?? string.Empty).Trim().ToUpperInvariant();
        if (name.StartsWith("DT", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("HD", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("WT", StringComparison.OrdinalIgnoreCase))
        {
            return "Hauling";
        }

        if (name.StartsWith("EX", StringComparison.OrdinalIgnoreCase))
        {
            return "Mining";
        }

        if (!string.IsNullOrWhiteSpace(groupName) &&
            groupName.Contains("HAUL", StringComparison.OrdinalIgnoreCase))
        {
            return "Hauling";
        }

        return "Mining";
    }

    private static string ResolveLocation(string? groupName, double? latitude, double? longitude)
    {
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            return TrimToLength(groupName, 256);
        }

        if (latitude.HasValue && longitude.HasValue)
        {
            var formatted = string.Format(
                CultureInfo.InvariantCulture,
                "{0:0.000000}, {1:0.000000}",
                latitude.Value,
                longitude.Value);
            return TrimToLength(formatted, 256);
        }

        return "Unknown";
    }

    private static string TrimToLength(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];

    private static TimeZoneInfo ResolveJakartaTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Local;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Local;
        }
    }
}
