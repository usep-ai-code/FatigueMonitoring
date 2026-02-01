using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FatigueMonitoring.Web.Api.Data;
using FatigueMonitoring.Web.Api.DTOs;
using FatigueMonitoring.Web.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Services;

public class ExternalApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int TokenRefreshIntervalMinutes { get; set; } = 1;
}

public class ExternalApiService(
    HttpClient httpClient,
    IOptions<ExternalApiSettings> settings,
    IServiceScopeFactory scopeFactory,
    ILogger<ExternalApiService> logger) : IExternalApiService
{
    private readonly ExternalApiSettings _settings = settings.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true 
    };

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FatigueMonitoringDbContext>();

            // Check for existing valid token
            var cachedToken = await dbContext.TokenCache
                .Where(t => t.TokenType == "ExternalApi" && t.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken);

            if (cachedToken != null)
            {
                logger.LogDebug("Using cached token, expires at {ExpiresAt}", cachedToken.ExpiresAt);
                return cachedToken.AccessToken;
            }

            // Need to get new token
            logger.LogInformation("Requesting new access token from external API");

            var authRequest = new AuthRequest(_settings.Username, _settings.Password);
            var content = new StringContent(
                JsonSerializer.Serialize(authRequest),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync($"{_settings.BaseUrl}/vss/auth", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to authenticate with external API. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                return null;
            }

            var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, JsonOptions);
            
            if (authResponse?.Success != true || authResponse.Data == null)
            {
                logger.LogError("Authentication failed. Message: {Message}", authResponse?.Message);
                return null;
            }

            // Store token in database
            var newToken = new AI_TokenCache_T
            {
                TokenType = "ExternalApi",
                AccessToken = authResponse.Data.AccessToken,
                Token = authResponse.Data.Token,
                Pid = authResponse.Data.Pid,
                CompanyId = authResponse.Data.Company.Id,
                CompanyName = authResponse.Data.Company.Name,
                // Token valid for configured interval (default 1 minute restriction from API)
                ExpiresAt = DateTime.UtcNow.AddMinutes(_settings.TokenRefreshIntervalMinutes - 0.1),
                CreatedAt = DateTime.UtcNow
            };

            dbContext.TokenCache.Add(newToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Clean up old tokens
            var oldTokens = await dbContext.TokenCache
                .Where(t => t.ExpiresAt < DateTime.UtcNow.AddHours(-1))
                .ToListAsync(cancellationToken);
            
            if (oldTokens.Count > 0)
            {
                dbContext.TokenCache.RemoveRange(oldTokens);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            logger.LogInformation("Successfully obtained new access token");
            return newToken.AccessToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task<EventResponse?> GetEventsAsync(
        DateTime startDate, 
        DateTime endDate, 
        int page = 1, 
        int pageSize = 100,
        string? filterColumn = null, 
        string? filterValue = null, 
        CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            logger.LogError("Cannot fetch events: no valid access token");
            return null;
        }

        var request = new EventRequest(
            RangeDateStart: startDate.ToString("yyyy-MM-dd HH:mm:ss"),
            RangeDateEnd: endDate.ToString("yyyy-MM-dd HH:mm:ss"),
            RangeDateColumns: "device_time",
            Page: page,
            PageSize: pageSize,
            FilterColumns: filterColumn,
            FilterValue: filterValue
        );

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/events/")
        {
            Content = content
        };
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Failed to fetch events. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                return null;
            }

            var eventResponse = JsonSerializer.Deserialize<EventResponse>(responseContent, JsonOptions);
            logger.LogInformation("Successfully fetched {Count} events from external API", 
                eventResponse?.Data?.List?.Count ?? 0);
            
            return eventResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching events from external API");
            return null;
        }
    }

    public async Task<FollowUpResponse?> GetFollowUpAsync(string alarmGuid, CancellationToken cancellationToken = default)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            logger.LogError("Cannot fetch follow-up: no valid access token");
            return null;
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Get, 
            $"{_settings.BaseUrl}/evidence/{alarmGuid}/follow-ups");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var response = await httpClient.SendAsync(httpRequest, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Failed to fetch follow-up for alarm {AlarmGuid}. Status: {StatusCode}",
                    alarmGuid, response.StatusCode);
                return null;
            }

            return JsonSerializer.Deserialize<FollowUpResponse>(responseContent, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching follow-up for alarm {AlarmGuid}", alarmGuid);
            return null;
        }
    }
}
