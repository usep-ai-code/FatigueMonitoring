using System.Text.Json.Serialization;

namespace FatigueMonitoring.Web.Api;

public sealed class ExternalApiResponse<T>
{
    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class ExternalAuthResponseData
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("pid")]
    public string? Pid { get; set; }
}

public sealed class ExternalEventListData
{
    [JsonPropertyName("list")]
    public List<ExternalEvent> List { get; set; } = [];

    [JsonPropertyName("pagination")]
    public ExternalPagination? Pagination { get; set; }
}

public sealed class ExternalPagination
{
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; }
}

public sealed class ExternalEvent
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("alarm_type")]
    public string? AlarmType { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }

    [JsonPropertyName("server_time")]
    public DateTime? ServerTime { get; set; }

    [JsonPropertyName("is_followed_up")]
    public bool IsFollowedUp { get; set; }

    [JsonPropertyName("speed")]
    public decimal? Speed { get; set; }

    [JsonPropertyName("latitude")]
    public double? Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double? Longitude { get; set; }

    [JsonPropertyName("manual_verification_by")]
    public string? ManualVerificationBy { get; set; }

    [JsonPropertyName("manual_verification_memo")]
    public string? ManualVerificationMemo { get; set; }

    [JsonPropertyName("device")]
    public ExternalDevice? Device { get; set; }

    [JsonPropertyName("geofence")]
    public ExternalGeofence? Geofence { get; set; }

    [JsonPropertyName("driver")]
    public ExternalDriver? Driver { get; set; }
}

public sealed class ExternalDevice
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("imei")]
    public string? Imei { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("group_name")]
    public string? GroupName { get; set; }
}

public sealed class ExternalGeofence
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public sealed class ExternalDriver
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
