using System.Collections.Concurrent;
using System.Text.Json;
using FatigueMonitoring.Web.Api.DTOs;

namespace FatigueMonitoring.Web.Api.Services;

public interface ISseConnectionManager
{
    void AddConnection(string connectionId, HttpResponse response);
    void RemoveConnection(string connectionId);
    Task BroadcastAsync(SseEventDto eventData, CancellationToken cancellationToken = default);
    Task SendToConnectionAsync(string connectionId, SseEventDto eventData, CancellationToken cancellationToken = default);
    int GetConnectionCount();
}

public class SseConnectionManager(ILogger<SseConnectionManager> logger) : ISseConnectionManager
{
    private readonly ConcurrentDictionary<string, HttpResponse> _connections = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public void AddConnection(string connectionId, HttpResponse response)
    {
        _connections.TryAdd(connectionId, response);
        logger.LogInformation("SSE connection added: {ConnectionId}. Total connections: {Count}",
            connectionId, _connections.Count);
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
        logger.LogInformation("SSE connection removed: {ConnectionId}. Total connections: {Count}",
            connectionId, _connections.Count);
    }

    public async Task BroadcastAsync(SseEventDto eventData, CancellationToken cancellationToken = default)
    {
        var deadConnections = new List<string>();
        var json = JsonSerializer.Serialize(eventData, JsonOptions);
        var message = $"event: {eventData.EventType}\ndata: {json}\n\n";

        foreach (var (connectionId, response) in _connections)
        {
            try
            {
                await response.WriteAsync(message, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send to connection {ConnectionId}, marking for removal",
                    connectionId);
                deadConnections.Add(connectionId);
            }
        }

        // Remove dead connections
        foreach (var connectionId in deadConnections)
        {
            RemoveConnection(connectionId);
        }
    }

    public async Task SendToConnectionAsync(string connectionId, SseEventDto eventData, CancellationToken cancellationToken = default)
    {
        if (_connections.TryGetValue(connectionId, out var response))
        {
            try
            {
                var json = JsonSerializer.Serialize(eventData, JsonOptions);
                var message = $"event: {eventData.EventType}\ndata: {json}\n\n";
                await response.WriteAsync(message, cancellationToken);
                await response.Body.FlushAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send to connection {ConnectionId}", connectionId);
                RemoveConnection(connectionId);
            }
        }
    }

    public int GetConnectionCount() => _connections.Count;
}
