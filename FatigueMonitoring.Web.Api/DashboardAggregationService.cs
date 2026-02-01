using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api;

public sealed class DashboardAggregationService(
    DashboardDbContext dbContext,
    AreaClassifier areaClassifier,
    TimeZoneProvider timeZoneProvider,
    TimeProvider timeProvider,
    JsonSerializerOptions jsonOptions,
    IOptions<DashboardOptions> options,
    ILogger<DashboardAggregationService> logger)
{
    private readonly DashboardOptions _options = options.Value;

    public async Task PersistSnapshotAsync(CancellationToken ct)
    {
        var snapshot = await BuildSnapshotAsync(ct);
        var payload = JsonSerializer.Serialize(snapshot, jsonOptions);

        var entity = await dbContext.DashboardSnapshots.SingleOrDefaultAsync(ct);
        if (entity is null)
        {
            entity = new DashboardSnapshot
            {
                GeneratedAtUtc = snapshot.GeneratedAtUtc.UtcDateTime,
                PayloadJson = payload
            };
            dbContext.DashboardSnapshots.Add(entity);
        }
        else
        {
            entity.GeneratedAtUtc = snapshot.GeneratedAtUtc.UtcDateTime;
            entity.PayloadJson = payload;
        }

        await dbContext.SaveChangesAsync(ct);
        logger.LogInformation("Dashboard snapshot persisted at {Timestamp}.", snapshot.GeneratedAtUtc);
    }

    private async Task<DashboardSnapshotDto> BuildSnapshotAsync(CancellationToken ct)
    {
        var nowUtc = timeProvider.GetUtcNow();
        var (dayStartUtc, dayEndUtc) = GetDayBoundsUtc(nowUtc.UtcDateTime);

        var rawEvents = await dbContext.RawEvents
            .AsNoTracking()
            .Where(e => e.DeviceTimeUtc >= dayStartUtc && e.DeviceTimeUtc < dayEndUtc)
            .ToListAsync(ct);

        var enriched = rawEvents
            .Select(e => new EnrichedEvent(
                e,
                areaClassifier.Resolve(e.DeviceGroup, e.DeviceName),
                ResolveLocation(e)))
            .ToList();

        var unitCounts = enriched
            .GroupBy(e => e.Unit)
            .ToDictionary(g => g.Key, g => g.Count());

        var activeAlerts = enriched
            .Where(e => !e.Raw.IsFollowedUp)
            .OrderByDescending(e => e.Raw.DeviceTimeUtc)
            .Take(_options.MaxActiveAlerts)
            .Select(e => ToAlertDto(e, unitCounts))
            .ToList();

        var delayedAlerts = activeAlerts
            .Where(alert => (nowUtc - alert.OpenedAtUtc).TotalMinutes >= _options.DelayedMinutes)
            .Take(_options.MaxDelayedAlerts)
            .ToList();

        var areaKpis = BuildAreaKpis(enriched);
        var areaDistribution = BuildAreaDistribution(enriched);
        var recurrentUnits = BuildRecurrentUnits(enriched);
        var highRiskAreas = BuildHighRiskAreas(enriched);
        var deviceHealth = BuildDeviceHealth(rawEvents, nowUtc.UtcDateTime);

        return new DashboardSnapshotDto(
            GeneratedAtUtc: nowUtc,
            DeviceHealth: deviceHealth,
            AreaKpis: areaKpis,
            AreaDistribution: areaDistribution,
            ActiveAlerts: activeAlerts,
            DelayedAlerts: delayedAlerts,
            RecurrentUnits: recurrentUnits,
            HighRiskAreas: highRiskAreas);
    }

    private (DateTime startUtc, DateTime endUtc) GetDayBoundsUtc(DateTime utcNow)
    {
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, timeZoneProvider.Jakarta);
        var localStart = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, timeZoneProvider.Jakarta);
        return (startUtc, startUtc.AddDays(1));
    }

    private DeviceHealthDto BuildDeviceHealth(IEnumerable<RawEvent> events, DateTime utcNow)
    {
        var windowStart = utcNow.AddMinutes(-_options.OnlineWindowMinutes);
        var units = events.Select(GetUnitName).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct().ToList();
        var onlineUnits = events
            .Where(e => e.DeviceTimeUtc >= windowStart)
            .Select(GetUnitName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct()
            .ToList();

        var total = units.Count;
        var online = onlineUnits.Count;
        var offline = Math.Max(0, total - online);
        var coverage = total == 0 ? 0 : (int)Math.Round((double)online / total * 100, MidpointRounding.AwayFromZero);

        return new DeviceHealthDto(total, online, offline, coverage);
    }

    private IReadOnlyList<AreaKpiDto> BuildAreaKpis(IReadOnlyList<EnrichedEvent> events)
    {
        var areaGroups = events.GroupBy(e => e.Area).ToDictionary(g => g.Key, g => g.ToList());
        var results = new List<AreaKpiDto>
        {
            BuildKpi("All", events)
        };

        foreach (var area in new[] { "Mining", "Hauling" })
        {
            results.Add(areaGroups.TryGetValue(area, out var list)
                ? BuildKpi(area, list)
                : new AreaKpiDto(area, 0, 0, 0));
        }

        return results;
    }

    private AreaKpiDto BuildKpi(string area, IReadOnlyList<EnrichedEvent> events)
    {
        var total = events.Count;
        var followedUp = events.Count(e => e.Raw.IsFollowedUp);
        var waiting = total - followedUp;
        return new AreaKpiDto(area, total, followedUp, waiting);
    }

    private IReadOnlyList<LocationDistributionDto> BuildAreaDistribution(IReadOnlyList<EnrichedEvent> events)
    {
        var totalByLocation = events
            .GroupBy(e => new { e.Area, e.Location })
            .ToDictionary(g => g.Key, g => g.Count());

        var openByLocation = events
            .Where(e => !e.Raw.IsFollowedUp)
            .GroupBy(e => new { e.Area, e.Location })
            .ToDictionary(g => g.Key, g => g.Count());

        return totalByLocation
            .Select(kvp =>
            {
                var openCount = openByLocation.TryGetValue(kvp.Key, out var open) ? open : 0;
                return new LocationDistributionDto(kvp.Key.Area, kvp.Key.Location, openCount, kvp.Value);
            })
            .OrderByDescending(item => item.OpenCount)
            .ToList();
    }

    private IReadOnlyList<RecurrentUnitDto> BuildRecurrentUnits(IReadOnlyList<EnrichedEvent> events)
    {
        return events
            .GroupBy(e => new { e.Unit, e.Area })
            .Select(group =>
            {
                var latest = group.OrderByDescending(e => e.Raw.DeviceTimeUtc).First();
                return new RecurrentUnitDto(
                    group.Key.Unit,
                    latest.Raw.OperatorName ?? "Unknown",
                    group.Key.Area,
                    group.Count());
            })
            .Where(item => item.Events > 1)
            .OrderByDescending(item => item.Events)
            .Take(_options.MaxRecurrentUnits)
            .ToList();
    }

    private IReadOnlyList<HighRiskAreaDto> BuildHighRiskAreas(IReadOnlyList<EnrichedEvent> events)
    {
        return events
            .GroupBy(e => new { e.Area, e.Location })
            .Select(group => new HighRiskAreaDto(group.Key.Area, group.Key.Location, group.Count()))
            .OrderByDescending(item => item.Events)
            .Take(_options.MaxHighRiskAreas)
            .ToList();
    }

    private AlertDto ToAlertDto(EnrichedEvent enriched, IReadOnlyDictionary<string, int> unitCounts)
    {
        var unit = enriched.Unit;
        var occurrences = unitCounts.TryGetValue(unit, out var count) ? count : 1;
        return new AlertDto(
            Id: enriched.Raw.ExternalId,
            Unit: unit,
            Operator: enriched.Raw.OperatorName ?? "Unknown",
            Area: enriched.Area,
            Location: enriched.Location,
            OpenedAtUtc: new DateTimeOffset(enriched.Raw.DeviceTimeUtc, TimeSpan.Zero),
            Status: enriched.Raw.IsFollowedUp ? "Followed Up" : "Open",
            SpeedKph: enriched.Raw.SpeedKph,
            AlarmType: string.IsNullOrWhiteSpace(enriched.Raw.AlarmName) ? "Fatigue" : enriched.Raw.AlarmName!,
            Latitude: enriched.Raw.Latitude,
            Longitude: enriched.Raw.Longitude,
            Occurrences: occurrences);
    }

    private static string ResolveLocation(RawEvent e)
    {
        if (!string.IsNullOrWhiteSpace(e.GeofenceName))
        {
            return e.GeofenceName;
        }

        if (!string.IsNullOrWhiteSpace(e.DeviceGroup))
        {
            return e.DeviceGroup;
        }

        if (!string.IsNullOrWhiteSpace(e.DeviceName))
        {
            return e.DeviceName;
        }

        if (e.Latitude.HasValue && e.Longitude.HasValue)
        {
            return string.Format(CultureInfo.InvariantCulture, "Lat {0:0.000}, Lon {1:0.000}", e.Latitude.Value, e.Longitude.Value);
        }

        return "Unknown";
    }

    private static string GetUnitName(RawEvent e)
        => string.IsNullOrWhiteSpace(e.DeviceName) ? e.DeviceId ?? "Unknown" : e.DeviceName;

    private sealed record EnrichedEvent(RawEvent Raw, string Area, string Location)
    {
        public string Unit => string.IsNullOrWhiteSpace(Raw.DeviceName) ? Raw.DeviceId ?? "Unknown" : Raw.DeviceName;
    }
}
