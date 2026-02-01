using System.Text.Json;
using FatigueMonitoring.Web.Api.Models;
using FatigueMonitoring.Web.Api.Options;
using FatigueMonitoring.Web.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(
    DashboardSnapshotService snapshotService,
    IOptions<SseOptions> sseOptions,
    ILogger<DashboardController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [HttpGet("snapshot")]
    public async Task<ActionResult<DashboardSnapshotDto>> GetSnapshot(CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        return Ok(snapshot);
    }

    [HttpGet("status")]
    public async Task<ActionResult<DashboardStatusDto>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await snapshotService.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        await Response.WriteAsync("retry: 10000\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);

        var heartbeatInterval = TimeSpan.FromSeconds(sseOptions.Value.HeartbeatSeconds);
        var refreshInterval = TimeSpan.FromSeconds(sseOptions.Value.RefreshSeconds);
        using var heartbeatTimer = new PeriodicTimer(heartbeatInterval);
        using var refreshTimer = new PeriodicTimer(refreshInterval);

        await SendSnapshotAsync(cancellationToken);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var heartbeatTask = heartbeatTimer.WaitForNextTickAsync(cancellationToken).AsTask();
                var refreshTask = refreshTimer.WaitForNextTickAsync(cancellationToken).AsTask();

                var completed = await Task.WhenAny(heartbeatTask, refreshTask);
                if (completed == heartbeatTask && await heartbeatTask)
                {
                    await SendEventAsync("heartbeat", new { ts = DateTimeOffset.UtcNow }, cancellationToken);
                }

                if (completed == refreshTask && await refreshTask)
                {
                    await SendSnapshotAsync(cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SSE connection closed by client.");
        }
    }

    private async Task SendSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshot = await snapshotService.GetSnapshotAsync(cancellationToken);
        await SendEventAsync("snapshot", snapshot, cancellationToken);
    }

    private async Task SendEventAsync(string eventName, object payload, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        await Response.WriteAsync($"event: {eventName}\n", cancellationToken);
        await Response.WriteAsync($"data: {json}\n\n", cancellationToken);
        await Response.Body.FlushAsync(cancellationToken);
    }
}
