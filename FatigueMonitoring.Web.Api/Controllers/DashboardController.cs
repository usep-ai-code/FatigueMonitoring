using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FatigueMonitoring.Web.Api.Data;

namespace FatigueMonitoring.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats([FromQuery] string area = "All")
    {
        try
        {
            var stats = await context.AI_DashboardStats_T
                .Where(s => s.Area == area)
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                return Ok(new { totalAlarms = 0, followedUp = 0, waitingFollowUp = 0 });
            }

            return Ok(new
            {
                totalAlarms = stats.TotalAlarms,
                followedUp = stats.FollowedUp,
                waitingFollowUp = stats.WaitingFollowUp,
                lastUpdated = stats.LastUpdated
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching stats");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("active-alerts")]
    public async Task<IActionResult> GetActiveAlerts([FromQuery] string? area = null, [FromQuery] string? location = null)
    {
        try
        {
            var query = context.AI_ActiveAlert_T.Where(a => a.Status == "Open");

            if (!string.IsNullOrEmpty(area) && area != "All")
            {
                query = query.Where(a => a.Area == area);
            }

            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(a => a.Location == location);
            }

            var alerts = await query
                .OrderByDescending(a => a.Time)
                .ToListAsync();

            // Update open duration
            var result = alerts.Select(a => new
            {
                a.Id,
                a.EventId,
                a.Unit,
                a.Operator,
                a.Type,
                a.Area,
                a.Location,
                a.Time,
                a.Status,
                Speed = $"{a.Speed:F0} km/h",
                a.Count,
                a.Latitude,
                a.Longitude,
                OpenDurationMinutes = (int)(DateTime.UtcNow - a.Time).TotalMinutes
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching active alerts");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("area-distribution")]
    public async Task<IActionResult> GetAreaDistribution([FromQuery] string? area = null)
    {
        try
        {
            var query = context.AI_AreaDistribution_T.AsQueryable();

            if (!string.IsNullOrEmpty(area) && area != "All")
            {
                query = query.Where(d => d.Area == area);
            }

            var distribution = await query
                .OrderByDescending(d => d.AlertCount)
                .ToListAsync();

            var result = distribution
                .GroupBy(d => d.Area)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(d => new { d.Location, d.AlertCount }).ToList()
                );

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching area distribution");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("recurrent-units")]
    public async Task<IActionResult> GetRecurrentUnits([FromQuery] string? area = null)
    {
        try
        {
            var query = context.AI_RecurrentUnit_T.AsQueryable();

            if (!string.IsNullOrEmpty(area) && area != "All")
            {
                query = query.Where(u => u.Area == area);
            }

            var units = await query
                .OrderByDescending(u => u.EventCount)
                .Take(20)
                .ToListAsync();

            var result = units.Select(u => new
            {
                u.Unit,
                Name = u.OperatorName,
                Events = u.EventCount,
                u.Status
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching recurrent units");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("high-risk-areas")]
    public async Task<IActionResult> GetHighRiskAreas([FromQuery] string? area = null)
    {
        try
        {
            var query = context.AI_HighRiskArea_T.AsQueryable();

            if (!string.IsNullOrEmpty(area) && area != "All")
            {
                query = query.Where(a => a.Area == area);
            }

            var areas = await query
                .OrderByDescending(a => a.EventCount)
                .Take(20)
                .ToListAsync();

            var result = areas.Select(a => new
            {
                a.Location,
                a.Area,
                Count = a.EventCount
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching high risk areas");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("delayed-alerts")]
    public async Task<IActionResult> GetDelayedAlerts([FromQuery] string? area = null)
    {
        try
        {
            var query = context.AI_ActiveAlert_T
                .Where(a => a.Status == "Open");

            if (!string.IsNullOrEmpty(area) && area != "All")
            {
                query = query.Where(a => a.Area == area);
            }

            var alerts = await query.ToListAsync();

            // Filter alerts that have been open for more than 30 minutes
            var delayedAlerts = alerts
                .Where(a => (DateTime.UtcNow - a.Time).TotalMinutes > 30)
                .OrderByDescending(a => (DateTime.UtcNow - a.Time).TotalMinutes)
                .Select(a => new
                {
                    a.Id,
                    a.Unit,
                    a.Operator,
                    a.Location,
                    a.Time,
                    OpenDurationMinutes = (int)(DateTime.UtcNow - a.Time).TotalMinutes
                })
                .ToList();

            return Ok(delayedAlerts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching delayed alerts");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}
