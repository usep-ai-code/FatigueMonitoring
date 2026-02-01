using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FatigueMonitoring.Web.Api.Data;

namespace FatigueMonitoring.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SseController(ApplicationDbContext context, ILogger<SseController> logger) : ControllerBase
{
    [HttpGet("stream")]
    public async Task StreamAsync()
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("Access-Control-Allow-Origin", "*");

        var clientId = Guid.NewGuid().ToString();
        logger.LogInformation("SSE client {ClientId} connected", clientId);

        try
        {
            // Send initial connection message
            await SendEventAsync("connected", new { message = "Connected to SSE stream", clientId });

            var lastUpdate = DateTime.MinValue;
            var heartbeatInterval = TimeSpan.FromSeconds(15);
            var lastHeartbeat = DateTime.UtcNow;

            while (!HttpContext.RequestAborted.IsCancellationRequested)
            {
                try
                {
                    // Send heartbeat to keep connection alive
                    if (DateTime.UtcNow - lastHeartbeat >= heartbeatInterval)
                    {
                        await SendEventAsync("heartbeat", new { timestamp = DateTime.UtcNow });
                        lastHeartbeat = DateTime.UtcNow;
                    }

                    // Check for updates
                    var latestUpdate = await context.AI_ActiveAlert_T
                        .MaxAsync(a => (DateTime?)a.LastUpdated) ?? DateTime.MinValue;

                    if (latestUpdate > lastUpdate)
                    {
                        lastUpdate = latestUpdate;

                        // Fetch all dashboard data
                        var dashboardData = await GetDashboardDataAsync();
                        await SendEventAsync("update", dashboardData);

                        logger.LogInformation("SSE client {ClientId} - Data update sent", clientId);
                    }

                    // Wait before next check
                    await Task.Delay(3000, HttpContext.RequestAborted);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in SSE loop for client {ClientId}", clientId);
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SSE connection error for client {ClientId}", clientId);
        }
        finally
        {
            logger.LogInformation("SSE client {ClientId} disconnected", clientId);
        }
    }

    private async Task SendEventAsync(string eventType, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var message = $"event: {eventType}\ndata: {json}\n\n";
        await Response.WriteAsync(message);
        await Response.Body.FlushAsync();
    }

    private async Task<object> GetDashboardDataAsync()
    {
        // Fetch all aggregated data
        var stats = await context.AI_DashboardStats_T.ToListAsync();
        var activeAlerts = await context.AI_ActiveAlert_T
            .Where(a => a.Status == "Open")
            .OrderByDescending(a => a.Time)
            .ToListAsync();
        
        var areaDistribution = await context.AI_AreaDistribution_T
            .OrderByDescending(d => d.AlertCount)
            .ToListAsync();

        var recurrentUnits = await context.AI_RecurrentUnit_T
            .OrderByDescending(u => u.EventCount)
            .Take(20)
            .ToListAsync();

        var highRiskAreas = await context.AI_HighRiskArea_T
            .OrderByDescending(a => a.EventCount)
            .Take(20)
            .ToListAsync();

        // Calculate delayed alerts (>30 minutes)
        var delayedAlerts = activeAlerts
            .Where(a => (DateTime.UtcNow - a.Time).TotalMinutes > 30)
            .ToList();

        return new
        {
            timestamp = DateTime.UtcNow,
            stats = stats.ToDictionary(s => s.Area, s => new
            {
                totalAlarms = s.TotalAlarms,
                followedUp = s.FollowedUp,
                waitingFollowUp = s.WaitingFollowUp
            }),
            activeAlerts = activeAlerts.Select(a => new
            {
                id = a.Id,
                eventId = a.EventId,
                unit = a.Unit,
                @operator = a.Operator,
                type = a.Type,
                area = a.Area,
                location = a.Location,
                time = a.Time,
                status = a.Status,
                speed = $"{a.Speed:F0} km/h",
                count = a.Count,
                latitude = a.Latitude,
                longitude = a.Longitude,
                openDurationMinutes = (int)(DateTime.UtcNow - a.Time).TotalMinutes
            }),
            areaDistribution = areaDistribution
                .GroupBy(d => d.Area)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(d => new { location = d.Location, alertCount = d.AlertCount }).ToList()
                ),
            recurrentUnits = recurrentUnits.Select(u => new
            {
                unit = u.Unit,
                name = u.OperatorName,
                events = u.EventCount,
                status = u.Status,
                area = u.Area
            }),
            highRiskAreas = highRiskAreas.Select(a => new
            {
                location = a.Location,
                area = a.Area,
                count = a.EventCount
            }),
            delayedAlerts = delayedAlerts.Select(a => new
            {
                id = a.Id,
                unit = a.Unit,
                @operator = a.Operator,
                location = a.Location,
                time = a.Time,
                openDurationMinutes = (int)(DateTime.UtcNow - a.Time).TotalMinutes,
                area = a.Area
            })
        };
    }
}
