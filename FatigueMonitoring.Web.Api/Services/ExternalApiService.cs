using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FatigueMonitoring.Web.Api.Models;

namespace FatigueMonitoring.Web.Api.Services;

public class ExternalApiService(IConfiguration configuration, ILogger<ExternalApiService> logger)
{
    private readonly HttpClient _httpClient = new();
    private readonly string _baseUrl = "https://api-platform-integrator.transtrack.co/api/v1";
    private readonly string _username = configuration["ExternalApi:Username"] ?? "sis@mdvr";
    private readonly string _password = configuration["ExternalApi:Password"] ?? "Sis@mdvr12345";
    
    private string? _accessToken;
    private DateTime _tokenExpiry = DateTime.MinValue;
    private DateTime _lastLoginAttempt = DateTime.MinValue;
    private readonly TimeSpan _minLoginInterval = TimeSpan.FromMinutes(1);

    public async Task<string?> GetAccessTokenAsync()
    {
        // Check if token is still valid
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiry)
        {
            return _accessToken;
        }

        // Check rate limit (1 request per minute)
        var timeSinceLastLogin = DateTime.UtcNow - _lastLoginAttempt;
        if (timeSinceLastLogin < _minLoginInterval)
        {
            logger.LogWarning("Login rate limit enforced. Waiting {RemainingSeconds}s", 
                (_minLoginInterval - timeSinceLastLogin).TotalSeconds);
            await Task.Delay(_minLoginInterval - timeSinceLastLogin);
        }

        try
        {
            _lastLoginAttempt = DateTime.UtcNow;

            var loginRequest = new LoginRequest(_username, _password);
            var json = JsonSerializer.Serialize(loginRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/vss/auth", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (loginResponse?.Success == true && loginResponse.Data?.Access_Token != null)
            {
                _accessToken = loginResponse.Data.Access_Token;
                _tokenExpiry = DateTime.UtcNow.AddMinutes(55); // Assume 1 hour expiry, refresh at 55 min
                logger.LogInformation("Successfully obtained access token");
                return _accessToken;
            }

            logger.LogError("Login failed: {Message}", loginResponse?.Message);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during login");
            return null;
        }
    }

    public async Task<List<EventItem>> GetEventsAsync(DateTime startDate, DateTime endDate, string? filterColumn = null, string? filterValue = null)
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            logger.LogError("Cannot fetch events: No valid access token");
            return [];
        }

        try
        {
            var request = new EventRequest
            {
                Range_Date_Start = startDate.ToString("yyyy-MM-dd HH:mm:ss"),
                Range_Date_End = endDate.ToString("yyyy-MM-dd HH:mm:ss"),
                Range_Date_Columns = "device_time",
                Page = 1,
                Page_Size = 100,
                Filter_Columns = filterColumn,
                Filter_Value = filterValue
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/events/")
            {
                Content = content
            };
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var eventResponse = JsonSerializer.Deserialize<EventResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            if (eventResponse?.Success == true && eventResponse.Data?.List != null)
            {
                logger.LogInformation("Successfully fetched {Count} events", eventResponse.Data.List.Count);
                return eventResponse.Data.List;
            }

            logger.LogWarning("No events found or API call failed");
            return [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching events");
            return [];
        }
    }

    public async Task<FollowUpData?> GetFollowUpAsync(string alarmId)
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/evidence/{alarmId}/follow-ups");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(httpRequest);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var followUpResponse = JsonSerializer.Deserialize<FollowUpResponse>(responseJson, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            return followUpResponse?.Data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching follow-up for alarm {AlarmId}", alarmId);
            return null;
        }
    }
}
