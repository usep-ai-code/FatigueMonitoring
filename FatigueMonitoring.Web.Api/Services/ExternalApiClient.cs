using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FatigueMonitoring.Web.Api.Options;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api.Services;

public sealed class ExternalApiClient(
    HttpClient httpClient,
    IOptions<ExternalApiOptions> options,
    ILogger<ExternalApiClient> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ExternalApiOptions _options = options.Value;
    private readonly SemaphoreSlim _loginLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _lastLoginAt = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<ExternalEventDto>> GetEventsAsync(
        DateTime rangeStartLocal,
        DateTime rangeEndLocal,
        CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync(cancellationToken);
        var events = new List<ExternalEventDto>();
        var page = 1;
        var totalPages = 1;

        do
        {
            var payload = new
            {
                range_date_start = rangeStartLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                range_date_end = rangeEndLocal.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                range_date_columns = "device_time",
                page,
                page_size = _options.PageSize,
                filter_columns = _options.FilterColumn,
                filter_value = _options.FilterValue
            };

            var request = new HttpRequestMessage(HttpMethod.Post, _options.EventsPath)
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                token = await RefreshTokenAsync(cancellationToken);
                request = new HttpRequestMessage(HttpMethod.Post, _options.EventsPath)
                {
                    Content = JsonContent.Create(payload, options: JsonOptions)
                };
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                response = await httpClient.SendAsync(request, cancellationToken);
            }

            response.EnsureSuccessStatusCode();

            var payloadResponse =
                await response.Content.ReadFromJsonAsync<ExternalEventsResponse>(JsonOptions, cancellationToken);
            var list = payloadResponse?.Data?.List ?? [];
            totalPages = payloadResponse?.Data?.Pagination?.TotalPages ?? 1;
            events.AddRange(list.Select(MapEvent));
            page++;
        } while (page <= totalPages);

        return events;
    }

    private async Task<string> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        _accessToken = null;
        return await GetAccessTokenAsync(cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && DateTimeOffset.UtcNow - _lastLoginAt < TimeSpan.FromMinutes(50))
        {
            return _accessToken;
        }

        await _loginLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && DateTimeOffset.UtcNow - _lastLoginAt < TimeSpan.FromMinutes(50))
            {
                return _accessToken;
            }

            var sinceLastLogin = DateTimeOffset.UtcNow - _lastLoginAt;
            if (sinceLastLogin < TimeSpan.FromMinutes(1))
            {
                if (_accessToken is not null)
                {
                    return _accessToken;
                }

                var delay = TimeSpan.FromMinutes(1) - sinceLastLogin;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken);
                }
            }

            var authPayload = new { username = _options.Username, password = _options.Password };
            var response = await httpClient.PostAsJsonAsync(_options.AuthPath, authPayload, JsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken);
            var token = authResponse?.Data?.AccessToken;
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("External API token is missing from response.");
            }

            _accessToken = token;
            _lastLoginAt = DateTimeOffset.UtcNow;
            return token;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to authenticate with external API.");
            throw;
        }
        finally
        {
            _loginLock.Release();
        }
    }

    private static ExternalEventDto MapEvent(ExternalEventItem item)
        => new(
            item.Id ?? string.Empty,
            item.Identity,
            item.Name,
            item.AlarmType,
            ParseLocalDateTime(item.Time),
            ParseDateTime(item.ServerTime),
            item.Shift,
            ParseDateTime(item.ShiftDate),
            item.Level,
            item.Speed,
            item.IsFollowedUp ?? false,
            item.Latitude,
            item.Longitude,
            item.GeofenceId,
            item.DeviceId ?? item.Device?.Id,
            item.Device?.Name,
            item.Device?.GroupName,
            item.ManualVerificationBy,
            ParseDateTime(item.ManualVerificationTime),
            item.ManualVerificationMemo,
            item.ManualVerificationWaitingDuration,
            ParseDateTime(item.UploadAt),
            ParseDateTime(item.UpdatedAt));

    private static DateTime? ParseDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            return dto.UtcDateTime;
        }

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt;
        }

        return null;
    }

    private static DateTime? ParseLocalDateTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(
                value,
                "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var dt))
        {
            return dt;
        }

        return ParseDateTime(value);
    }

    private sealed record AuthResponse(
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] AuthData? Data);

    private sealed record AuthData(
        [property: JsonPropertyName("access_token")] string? AccessToken);

    private sealed record ExternalEventsResponse(
        [property: JsonPropertyName("code")] int Code,
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("data")] ExternalEventsData? Data);

    private sealed record ExternalEventsData(
        [property: JsonPropertyName("list")] List<ExternalEventItem>? List,
        [property: JsonPropertyName("pagination")] ExternalPagination? Pagination);

    private sealed record ExternalPagination(
        [property: JsonPropertyName("total_pages")] int TotalPages);

    private sealed record ExternalDevice(
        [property: JsonPropertyName("id")] string? Id,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("group_name")] string? GroupName);

    private sealed record ExternalEventItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("identity")]
        public string? Identity { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("alarm_type")]
        public string? AlarmType { get; init; }

        [JsonPropertyName("time")]
        public string? Time { get; init; }

        [JsonPropertyName("server_time")]
        public string? ServerTime { get; init; }

        [JsonPropertyName("shift")]
        public string? Shift { get; init; }

        [JsonPropertyName("shift_date")]
        public string? ShiftDate { get; init; }

        [JsonPropertyName("level")]
        public int? Level { get; init; }

        [JsonPropertyName("speed")]
        public decimal? Speed { get; init; }

        [JsonPropertyName("is_followed_up")]
        public bool? IsFollowedUp { get; init; }

        [JsonPropertyName("latitude")]
        public double? Latitude { get; init; }

        [JsonPropertyName("longitude")]
        public double? Longitude { get; init; }

        [JsonPropertyName("geofence_id")]
        public string? GeofenceId { get; init; }

        [JsonPropertyName("device_id")]
        public string? DeviceId { get; init; }

        [JsonPropertyName("manual_verification_by")]
        public string? ManualVerificationBy { get; init; }

        [JsonPropertyName("manual_verification_time")]
        public string? ManualVerificationTime { get; init; }

        [JsonPropertyName("manual_verification_memo")]
        public string? ManualVerificationMemo { get; init; }

        [JsonPropertyName("manual_verification_waiting_duration")]
        public int? ManualVerificationWaitingDuration { get; init; }

        [JsonPropertyName("upload_at")]
        public string? UploadAt { get; init; }

        [JsonPropertyName("updated_at")]
        public string? UpdatedAt { get; init; }

        [JsonPropertyName("device")]
        public ExternalDevice? Device { get; init; }
    }
}

public sealed record ExternalEventDto(
    string ExternalId,
    string? Identity,
    string? Name,
    string? AlarmType,
    DateTime? DeviceTime,
    DateTime? ServerTime,
    string? Shift,
    DateTime? ShiftDate,
    int? Level,
    decimal? SpeedKph,
    bool IsFollowedUp,
    double? Latitude,
    double? Longitude,
    string? GeofenceId,
    string? DeviceId,
    string? DeviceName,
    string? DeviceGroupName,
    string? ManualVerificationBy,
    DateTime? ManualVerificationTime,
    string? ManualVerificationMemo,
    int? ManualVerificationWaitingDuration,
    DateTime? UploadAt,
    DateTime? UpdatedAt);
