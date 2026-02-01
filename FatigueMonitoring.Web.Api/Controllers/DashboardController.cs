using FatigueMonitoring.Web.Api.DTOs;
using FatigueMonitoring.Web.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace FatigueMonitoring.Web.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(
    IDataAggregationService aggregationService,
    ISseConnectionManager connectionManager,
    ILogger<DashboardController> logger) : ControllerBase
{
    /// <summary>
    /// Get dashboard data with optional area filter
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<DashboardDataDto>> GetDashboardData(
        [FromQuery] string area = "All",
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await aggregationService.GetDashboardDataAsync(area, cancellationToken);
            return Ok(data);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting dashboard data");
            return StatusCode(500, new { error = "Failed to get dashboard data" });
        }
    }

    /// <summary>
    /// SSE endpoint for real-time dashboard updates
    /// </summary>
    [HttpGet("stream")]
    public async Task StreamDashboardUpdates(CancellationToken cancellationToken)
    {
        var connectionId = Guid.NewGuid().ToString();
        
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no"); // For nginx proxies

        logger.LogInformation("New SSE connection: {ConnectionId}", connectionId);

        // Add connection to manager
        connectionManager.AddConnection(connectionId, Response);

        try
        {
            // Send initial connection confirmation
            var connectEvent = new SseEventDto(
                "connected",
                new { connectionId, serverTime = DateTime.UtcNow },
                DateTime.UtcNow
            );
            
            var json = System.Text.Json.JsonSerializer.Serialize(connectEvent, 
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            await Response.WriteAsync($"event: connected\ndata: {json}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            // Send initial dashboard data
            var data = await aggregationService.GetDashboardDataAsync("All", cancellationToken);
            var dataEvent = new SseEventDto("dashboard_update", data, DateTime.UtcNow);
            var dataJson = System.Text.Json.JsonSerializer.Serialize(dataEvent,
                new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
            await Response.WriteAsync($"event: dashboard_update\ndata: {dataJson}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            // Keep connection alive until cancelled
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SSE connection cancelled: {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SSE stream for connection {ConnectionId}", connectionId);
        }
        finally
        {
            connectionManager.RemoveConnection(connectionId);
            logger.LogInformation("SSE connection closed: {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Get current SSE connection count (for monitoring)
    /// </summary>
    [HttpGet("connections")]
    public ActionResult<object> GetConnectionCount()
    {
        return Ok(new { count = connectionManager.GetConnectionCount() });
    }
}
