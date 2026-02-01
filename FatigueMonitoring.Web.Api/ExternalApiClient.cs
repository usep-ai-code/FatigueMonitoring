using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace FatigueMonitoring.Web.Api;

public sealed class ExternalApiClient(
    HttpClient httpClient,
    ILogger<ExternalApiClient> logger,
    IOptions<ExternalApiOptions> options,
    TimeProvider timeProvider,
    TimeZoneProvider timeZoneProvider)
{
    private readonly ExternalApiOptions _options = options.Value;
    private ExternalAuthResponseData? _cachedAuth;
    private DateTimeOffset _tokenExpiresAt = DateTimeOffset.MinValue;

    public async Task<IReadOnlyList<ExternalEvent>> FetchEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            logger.LogWarning("External API BaseUrl is empty. Skipping fetch.");
            return [];
        }

        var results = new List<ExternalEvent>();
        var page = 1;
        var pageSize = Math.Max(1, _options.PageSize);

        while (!ct.IsCancellationRequested)
        {
            var payload = new Dictionary<string, object?>
            {
                ["range_date_start"] = ConvertToLocalString(startUtc),
                ["range_date_end"] = ConvertToLocalString(endUtc),
                ["range_date_columns"] = "device_time",
                ["page"] = page,
                ["page_size"] = pageSize,
                ["filter_columns"] = _options.FilterColumn,
                ["filter_value"] = _options.FilterValue
            };

            var response = await SendAuthenticatedAsync(HttpMethod.Post, "api/v1/events/", payload, ct);
            if (response is null)
            {
                break;
            }

            if (response.Data?.List is { Count: > 0 } list)
            {
                results.AddRange(list);
            }

            var totalPages = response.Data?.Pagination?.TotalPages ?? 1;
            if (page >= totalPages)
            {
                break;
            }

            page++;
        }

        return results;
    }

    private string ConvertToLocalString(DateTime utc)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZoneProvider.Jakarta);
        return local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private async Task<ExternalApiResponse<ExternalEventListData>?> SendAuthenticatedAsync(
        HttpMethod method,
        string url,
        object payload,
        CancellationToken ct)
    {
        var auth = await GetAuthAsync(ct);
        if (auth is null)
        {
            return null;
        }

        var request = new HttpRequestMessage(method, url)
        {
            Content = JsonContent.Create(payload)
        };

        ApplyAuthHeaders(request, auth);

        var response = await httpClient.SendAsync(request, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
        {
            _cachedAuth = null;
            auth = await GetAuthAsync(ct);
            if (auth is null)
            {
                return null;
            }

            request = new HttpRequestMessage(method, url)
            {
                Content = JsonContent.Create(payload)
            };
            ApplyAuthHeaders(request, auth);
            response = await httpClient.SendAsync(request, ct);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("External API call failed: {StatusCode}", response.StatusCode);
            return null;
        }

        return await response.Content.ReadFromJsonAsync<ExternalApiResponse<ExternalEventListData>>(cancellationToken: ct);
    }

    private async Task<ExternalAuthResponseData?> GetAuthAsync(CancellationToken ct)
    {
        if (_cachedAuth is not null && timeProvider.GetUtcNow() < _tokenExpiresAt)
        {
            return _cachedAuth;
        }

        var payload = new
        {
            username = _options.Username,
            password = _options.Password
        };

        var response = await httpClient.PostAsJsonAsync("api/v1/vss/auth", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("External API auth failed: {StatusCode}", response.StatusCode);
            return null;
        }

        var authResponse = await response.Content.ReadFromJsonAsync<ExternalApiResponse<ExternalAuthResponseData>>(cancellationToken: ct);
        if (authResponse?.Data?.AccessToken is null)
        {
            logger.LogWarning("External API auth response is missing access token.");
            return null;
        }

        _cachedAuth = authResponse.Data;
        _tokenExpiresAt = timeProvider.GetUtcNow().AddMinutes(Math.Max(5, _options.AuthCacheMinutes));
        return _cachedAuth;
    }

    private void ApplyAuthHeaders(HttpRequestMessage request, ExternalAuthResponseData auth)
    {
        if (!string.IsNullOrWhiteSpace(_options.AuthHeaderName))
        {
            if (!string.IsNullOrWhiteSpace(_options.AuthHeaderScheme))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                    _options.AuthHeaderScheme,
                    auth.AccessToken);
            }
            else
            {
                request.Headers.TryAddWithoutValidation(_options.AuthHeaderName, auth.AccessToken);
            }
        }

        if (!string.IsNullOrWhiteSpace(_options.TokenHeaderName) && !string.IsNullOrWhiteSpace(auth.Token))
        {
            request.Headers.TryAddWithoutValidation(_options.TokenHeaderName, auth.Token);
        }

        if (!string.IsNullOrWhiteSpace(_options.PidHeaderName) && !string.IsNullOrWhiteSpace(auth.Pid))
        {
            request.Headers.TryAddWithoutValidation(_options.PidHeaderName, auth.Pid);
        }
    }
}
