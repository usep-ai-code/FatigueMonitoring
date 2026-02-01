using FatigueMonitoring.Web.Api.DTOs;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Services;

/// <summary>
/// Background service that periodically broadcasts data updates and heartbeats via SSE
/// </summary>
public class SseBroadcastService(
    ISseConnectionManager connectionManager,
    IDataAggregationService aggregationService,
    IOptions<BackgroundJobSettings> settings,
    ILogger<SseBroadcastService> logger) : BackgroundService
{
    private readonly BackgroundJobSettings _settings = settings.Value;
    private DateTime _lastDataBroadcast = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SSE broadcast service starting. Heartbeat interval: {Interval}s",
            _settings.HeartbeatIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (connectionManager.GetConnectionCount() > 0)
                {
                    // Send heartbeat
                    var heartbeat = new SseEventDto(
                        "heartbeat",
                        new HeartbeatDto("alive", DateTime.UtcNow),
                        DateTime.UtcNow
                    );
                    await connectionManager.BroadcastAsync(heartbeat, stoppingToken);

                    // Check if we need to broadcast data update
                    var timeSinceLastBroadcast = DateTime.UtcNow - _lastDataBroadcast;
                    if (timeSinceLastBroadcast.TotalSeconds >= _settings.DataFetchIntervalSeconds)
                    {
                        await BroadcastDashboardDataAsync(stoppingToken);
                        _lastDataBroadcast = DateTime.UtcNow;
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in SSE broadcast cycle");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.HeartbeatIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        logger.LogInformation("SSE broadcast service stopped");
    }

    private async Task BroadcastDashboardDataAsync(CancellationToken cancellationToken)
    {
        try
        {
            var data = await aggregationService.GetDashboardDataAsync("All", cancellationToken);
            
            var eventData = new SseEventDto(
                "dashboard_update",
                data,
                DateTime.UtcNow
            );

            await connectionManager.BroadcastAsync(eventData, cancellationToken);
            logger.LogDebug("Broadcasted dashboard data to {Count} connections",
                connectionManager.GetConnectionCount());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error broadcasting dashboard data");
        }
    }
}
