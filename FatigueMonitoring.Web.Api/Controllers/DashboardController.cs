using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController(
    DashboardDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<SseOptions> sseOptions,
    ILogger<DashboardController> logger) : ControllerBase
{
    private readonly SseOptions _sseOptions = sseOptions.Value;

    [HttpGet("stream")]
    public async Task Stream(CancellationToken ct)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        await Response.Body.FlushAsync(ct);

        var dataInterval = TimeSpan.FromSeconds(Math.Max(2, _sseOptions.DataIntervalSeconds));
        var heartbeatInterval = TimeSpan.FromSeconds(Math.Max(5, _sseOptions.HeartbeatSeconds));
        var lastHeartbeat = timeProvider.GetUtcNow();
        var lastRowVersion = Array.Empty<byte>();

        logger.LogInformation("SSE connection opened.");

        try
        {
            using var timer = new PeriodicTimer(dataInterval);
            while (await timer.WaitForNextTickAsync(ct))
            {
                var now = timeProvider.GetUtcNow();
                if (now - lastHeartbeat >= heartbeatInterval)
                {
                    await Response.WriteAsync(": heartbeat\n\n", ct);
                    await Response.Body.FlushAsync(ct);
                    lastHeartbeat = now;
                }

                var snapshot = await dbContext.DashboardSnapshots
                    .AsNoTracking()
                    .OrderByDescending(x => x.GeneratedAtUtc)
                    .FirstOrDefaultAsync(ct);

                if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.PayloadJson))
                {
                    continue;
                }

                if (lastRowVersion.Length > 0 && snapshot.RowVersion.SequenceEqual(lastRowVersion))
                {
                    continue;
                }

                lastRowVersion = snapshot.RowVersion;
                await Response.WriteAsync($"data: {snapshot.PayloadJson}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            }
        }
        finally
        {
            logger.LogInformation("SSE connection closed.");
        }
    }

    [HttpGet("snapshot")]
    public async Task<IActionResult> Snapshot(CancellationToken ct)
    {
        var snapshot = await dbContext.DashboardSnapshots
            .AsNoTracking()
            .OrderByDescending(x => x.GeneratedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.PayloadJson))
        {
            return NoContent();
        }

        return Content(snapshot.PayloadJson, "application/json", Encoding.UTF8);
    }
}
