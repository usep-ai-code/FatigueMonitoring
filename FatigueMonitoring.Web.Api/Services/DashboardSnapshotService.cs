using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FatigueMonitoring.Web.Api.Services;

public sealed class DashboardSnapshotService(DashboardDbContext dbContext)
{
    public async Task<DashboardSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var kpis = await dbContext.AiKpis
            .AsNoTracking()
            .OrderBy(k => k.Area)
            .ToListAsync(cancellationToken);
        var activeAlerts = await dbContext.AiActiveAlerts
            .AsNoTracking()
            .OrderByDescending(a => a.OpenedAt)
            .ToListAsync(cancellationToken);
        var areaDistributions = await dbContext.AiAreaDistributions
            .AsNoTracking()
            .OrderByDescending(a => a.OpenCount)
            .ToListAsync(cancellationToken);
        var delayedAlerts = await dbContext.AiDelayedAlerts
            .AsNoTracking()
            .OrderByDescending(a => a.OpenMinutes)
            .ToListAsync(cancellationToken);
        var recurrentUnits = await dbContext.AiRecurrentUnits
            .AsNoTracking()
            .OrderByDescending(r => r.EventsCount)
            .ToListAsync(cancellationToken);
        var highRiskAreas = await dbContext.AiHighRiskAreas
            .AsNoTracking()
            .OrderByDescending(r => r.EventsCount)
            .ToListAsync(cancellationToken);
        var deviceHealth = await dbContext.AiDeviceHealth
            .AsNoTracking()
            .OrderByDescending(d => d.SnapshotAt)
            .FirstOrDefaultAsync(cancellationToken);

        var snapshotAt = kpis.FirstOrDefault()?.SnapshotAt
            ?? activeAlerts.FirstOrDefault()?.SnapshotAt
            ?? DateTime.UtcNow;

        return new DashboardSnapshotDto(
            snapshotAt,
            kpis.Select(k => new KpiDto(
                k.Area,
                k.TotalAlarms,
                k.FollowedUp,
                k.WaitingFollowUp)).ToList(),
            activeAlerts.Select(a => new ActiveAlertDto(
                a.AlarmId,
                a.Unit,
                a.Operator,
                a.Area,
                a.Location,
                a.OpenedAt,
                a.OpenMinutes,
                a.SpeedKph,
                a.AlarmType,
                a.Latitude,
                a.Longitude,
                a.Status,
                a.EventCount)).ToList(),
            areaDistributions.Select(a => new AreaDistributionDto(
                a.Area,
                a.Location,
                a.OpenCount)).ToList(),
            delayedAlerts.Select(a => new DelayedAlertDto(
                a.AlarmId,
                a.Unit,
                a.Operator,
                a.Area,
                a.Location,
                a.OpenedAt,
                a.OpenMinutes,
                a.SpeedKph,
                a.AlarmType,
                a.Latitude,
                a.Longitude,
                a.Status,
                a.EventCount)).ToList(),
            recurrentUnits.Select(r => new RecurrentUnitDto(
                r.Unit,
                r.Operator,
                r.Area,
                r.EventsCount)).ToList(),
            highRiskAreas.Select(r => new HighRiskAreaDto(
                r.Area,
                r.Location,
                r.EventsCount)).ToList(),
            deviceHealth is null
                ? null
                : new DeviceHealthDto(
                    deviceHealth.TotalDevices,
                    deviceHealth.OnlineDevices,
                    deviceHealth.OfflineDevices,
                    deviceHealth.Coverage));
    }
}
