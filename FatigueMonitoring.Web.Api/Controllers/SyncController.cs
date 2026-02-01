using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.Models;

namespace FatigueMonitoring.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SyncController(ApplicationDbContext context, ILogger<SyncController> logger) : ControllerBase
{
    /// <summary>
    /// Get current sync status and metadata
    /// </summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetSyncStatus()
    {
        try
        {
            var syncMetadata = await context.SyncMetadata
                .Where(s => s.SyncKey == "EventsSync")
                .FirstOrDefaultAsync();

            if (syncMetadata == null)
            {
                return Ok(new
                {
                    syncKey = "EventsSync",
                    status = "Not Initialized",
                    message = "Sync has not been initialized yet. Will be created on first job run."
                });
            }

            return Ok(new
            {
                syncMetadata.SyncKey,
                syncMetadata.LastSyncTime,
                syncMetadata.NextSyncTime,
                syncMetadata.Status,
                syncMetadata.ErrorMessage,
                syncMetadata.UpdatedAt,
                currentTime = DateTime.UtcNow,
                minutesUntilNextSync = syncMetadata.NextSyncTime.HasValue 
                    ? Math.Max(0, (syncMetadata.NextSyncTime.Value - DateTime.UtcNow).TotalMinutes)
                    : 0
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting sync status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update the last sync time (use with caution)
    /// </summary>
    [HttpPost("update-sync-time")]
    public async Task<IActionResult> UpdateSyncTime([FromBody] UpdateSyncTimeRequest request)
    {
        try
        {
            if (!DateTime.TryParse(request.NewSyncTime, out var newSyncTime))
            {
                return BadRequest(new { error = "Invalid date format. Use format: 2026-02-01 00:00:00" });
            }

            // Convert to UTC if not already
            if (newSyncTime.Kind == DateTimeKind.Unspecified)
            {
                newSyncTime = DateTime.SpecifyKind(newSyncTime, DateTimeKind.Utc);
            }
            else if (newSyncTime.Kind == DateTimeKind.Local)
            {
                newSyncTime = newSyncTime.ToUniversalTime();
            }

            var syncMetadata = await context.SyncMetadata
                .FirstOrDefaultAsync(s => s.SyncKey == "EventsSync");

            if (syncMetadata == null)
            {
                // Create new sync metadata
                syncMetadata = new SyncMetadata
                {
                    SyncKey = "EventsSync",
                    LastSyncTime = newSyncTime,
                    NextSyncTime = newSyncTime.AddMinutes(3),
                    Status = "Manually Updated",
                    UpdatedAt = DateTime.UtcNow
                };
                context.SyncMetadata.Add(syncMetadata);
                logger.LogInformation("Created new sync metadata with start time: {StartTime}", newSyncTime);
            }
            else
            {
                var oldSyncTime = syncMetadata.LastSyncTime;
                syncMetadata.LastSyncTime = newSyncTime;
                syncMetadata.NextSyncTime = newSyncTime.AddMinutes(3);
                syncMetadata.Status = "Manually Updated";
                syncMetadata.UpdatedAt = DateTime.UtcNow;
                logger.LogInformation("Updated sync time from {OldTime} to {NewTime}", oldSyncTime, newSyncTime);
            }

            await context.SaveChangesAsync();

            return Ok(new
            {
                message = "Sync time updated successfully",
                syncMetadata.LastSyncTime,
                syncMetadata.NextSyncTime,
                syncMetadata.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating sync time");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Reset sync to start from a specific date
    /// </summary>
    [HttpPost("reset")]
    public async Task<IActionResult> ResetSync([FromBody] ResetSyncRequest request)
    {
        try
        {
            if (!DateTime.TryParse(request.StartTime, out var startTime))
            {
                return BadRequest(new { error = "Invalid date format. Use format: 2026-02-01 00:00:00" });
            }

            // Convert to UTC
            if (startTime.Kind == DateTimeKind.Unspecified)
            {
                startTime = DateTime.SpecifyKind(startTime, DateTimeKind.Utc);
            }
            else if (startTime.Kind == DateTimeKind.Local)
            {
                startTime = startTime.ToUniversalTime();
            }

            var syncMetadata = await context.SyncMetadata
                .FirstOrDefaultAsync(s => s.SyncKey == "EventsSync");

            if (syncMetadata != null)
            {
                context.SyncMetadata.Remove(syncMetadata);
            }

            var newSyncMetadata = new SyncMetadata
            {
                SyncKey = "EventsSync",
                LastSyncTime = startTime,
                NextSyncTime = startTime.AddMinutes(3),
                Status = "Reset",
                UpdatedAt = DateTime.UtcNow
            };

            context.SyncMetadata.Add(newSyncMetadata);
            await context.SaveChangesAsync();

            logger.LogWarning("Sync metadata reset to start time: {StartTime}", startTime);

            return Ok(new
            {
                message = "Sync reset successfully. The background job will start from the specified time.",
                newSyncMetadata.LastSyncTime,
                newSyncMetadata.NextSyncTime,
                warning = "This will cause the system to re-fetch and re-aggregate historical data."
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resetting sync");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get sync history and statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            var totalEvents = await context.RawEvents.CountAsync();
            var eventsToday = await context.RawEvents
                .Where(e => e.Time >= DateTime.UtcNow.Date)
                .CountAsync();
            
            var oldestEvent = await context.RawEvents
                .OrderBy(e => e.Time)
                .Select(e => e.Time)
                .FirstOrDefaultAsync();
            
            var newestEvent = await context.RawEvents
                .OrderByDescending(e => e.Time)
                .Select(e => e.Time)
                .FirstOrDefaultAsync();

            var syncMetadata = await context.SyncMetadata
                .FirstOrDefaultAsync(s => s.SyncKey == "EventsSync");

            return Ok(new
            {
                totalEvents,
                eventsToday,
                oldestEvent,
                newestEvent,
                currentSyncStatus = syncMetadata?.Status ?? "Not Initialized",
                lastSyncTime = syncMetadata?.LastSyncTime,
                nextSyncTime = syncMetadata?.NextSyncTime
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting statistics");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

public class UpdateSyncTimeRequest
{
    public string NewSyncTime { get; set; } = string.Empty;
}

public class ResetSyncRequest
{
    public string StartTime { get; set; } = string.Empty;
}
