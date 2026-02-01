using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api;

public sealed class ExternalEventSyncJob(
    ILogger<ExternalEventSyncJob> logger,
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<BackgroundJobOptions> jobOptions,
    IOptions<DashboardOptions> dashboardOptions,
    TimeZoneProvider timeZoneProvider) : BackgroundService
{
    private readonly BackgroundJobOptions _jobOptions = jobOptions.Value;
    private readonly DashboardOptions _dashboardOptions = dashboardOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, _jobOptions.IntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DashboardDbContext>();
            var apiClient = scope.ServiceProvider.GetRequiredService<ExternalApiClient>();
            var aggregator = scope.ServiceProvider.GetRequiredService<DashboardAggregationService>();

            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var syncState = await dbContext.SyncStates.SingleOrDefaultAsync(ct);
            if (syncState is null)
            {
                syncState = new SyncState { LastEventFetchUtc = nowUtc.AddMinutes(-_dashboardOptions.InitialLookbackMinutes) };
                dbContext.SyncStates.Add(syncState);
                await dbContext.SaveChangesAsync(ct);
            }

            var lastFetch = syncState.LastEventFetchUtc ?? nowUtc.AddMinutes(-_dashboardOptions.InitialLookbackMinutes);
            var startUtc = lastFetch.AddMinutes(-_dashboardOptions.SyncOverlapMinutes);
            var endUtc = nowUtc;
            if (startUtc >= endUtc)
            {
                return;
            }

            var events = await apiClient.FetchEventsAsync(startUtc, endUtc, ct);
            if (events.Count > 0)
            {
                await UpsertEventsAsync(dbContext, events, nowUtc, ct);
            }

            syncState.LastEventFetchUtc = endUtc;
            syncState.LastAggregationUtc = nowUtc;
            await dbContext.SaveChangesAsync(ct);

            await aggregator.PersistSnapshotAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Background sync failed.");
        }
    }

    private async Task UpsertEventsAsync(DashboardDbContext dbContext, IReadOnlyList<ExternalEvent> events, DateTime nowUtc, CancellationToken ct)
    {
        var incoming = events
            .Where(e => !string.IsNullOrWhiteSpace(e.Id))
            .ToDictionary(e => e.Id!, e => e);

        if (incoming.Count == 0)
        {
            return;
        }

        var existing = await dbContext.RawEvents
            .Where(e => incoming.Keys.Contains(e.ExternalId))
            .ToListAsync(ct);

        foreach (var raw in existing)
        {
            if (!incoming.TryGetValue(raw.ExternalId, out var external))
            {
                continue;
            }

            raw.IsFollowedUp = external.IsFollowedUp;
            raw.SpeedKph = external.Speed ?? raw.SpeedKph;
            raw.Latitude = external.Latitude ?? raw.Latitude;
            raw.Longitude = external.Longitude ?? raw.Longitude;
            raw.OperatorName = external.Driver?.Name ?? external.ManualVerificationBy ?? raw.OperatorName;
            raw.ManualVerificationMemo = external.ManualVerificationMemo ?? raw.ManualVerificationMemo;
            raw.GeofenceName = external.Geofence?.Name ?? raw.GeofenceName;
            raw.UpdatedAtUtc = nowUtc;
        }

        var existingIds = existing.Select(e => e.ExternalId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newEvents = incoming.Values
            .Where(e => !existingIds.Contains(e.Id!))
            .Select(e => MapRawEvent(e, nowUtc))
            .ToList();

        if (newEvents.Count > 0)
        {
            dbContext.RawEvents.AddRange(newEvents);
        }
    }

    private RawEvent MapRawEvent(ExternalEvent external, DateTime nowUtc)
    {
        return new RawEvent
        {
            ExternalId = external.Id ?? Guid.NewGuid().ToString("N"),
            AlarmName = external.Name,
            AlarmType = external.AlarmType,
            DeviceTimeUtc = ParseDeviceTime(external.Time, nowUtc),
            ServerTimeUtc = external.ServerTime,
            IsFollowedUp = external.IsFollowedUp,
            SpeedKph = external.Speed ?? 0,
            Latitude = external.Latitude,
            Longitude = external.Longitude,
            DeviceId = external.Device?.Id,
            DeviceName = external.Device?.Name,
            DeviceGroup = external.Device?.GroupName,
            OperatorName = external.Driver?.Name ?? external.ManualVerificationBy,
            ManualVerificationMemo = external.ManualVerificationMemo,
            GeofenceName = external.Geofence?.Name,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        };
    }

    private DateTime ParseDeviceTime(string? value, DateTime fallbackUtc)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallbackUtc;
        }

        if (DateTime.TryParseExact(
            value,
            "yyyy-MM-dd HH:mm:ss",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var local))
        {
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), timeZoneProvider.Jakarta);
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
        {
            return parsed.ToUniversalTime();
        }

        return fallbackUtc;
    }
}
