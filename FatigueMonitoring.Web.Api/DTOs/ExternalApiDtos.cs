using System.Text.Json.Serialization;

namespace FatigueMonitoring.Web.Api.DTOs;

#region Auth DTOs

public record AuthRequest(
    [property: JsonPropertyName("username")] string Username,
    [property: JsonPropertyName("password")] string Password
);

public record AuthResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] AuthData? Data,
    [property: JsonPropertyName("error")] string? Error
);

public record AuthData(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("pid")] string Pid,
    [property: JsonPropertyName("company")] CompanyInfo Company
);

public record CompanyInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

#endregion

#region Event DTOs

public record EventRequest(
    [property: JsonPropertyName("range_date_start")] string RangeDateStart,
    [property: JsonPropertyName("range_date_end")] string RangeDateEnd,
    [property: JsonPropertyName("range_date_columns")] string RangeDateColumns,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("page_size")] int PageSize,
    [property: JsonPropertyName("filter_columns")] string? FilterColumns = null,
    [property: JsonPropertyName("filter_value")] string? FilterValue = null
);

public record EventResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] EventDataWrapper? Data,
    [property: JsonPropertyName("error")] string? Error
);

public record EventDataWrapper(
    [property: JsonPropertyName("list")] List<EventData> List,
    [property: JsonPropertyName("pagination")] PaginationInfo Pagination
);

public record EventData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("identity")] string? Identity,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("alarm_type")] string? AlarmType,
    [property: JsonPropertyName("time")] string? Time,
    [property: JsonPropertyName("server_time")] string? ServerTime,
    [property: JsonPropertyName("shift")] string? Shift,
    [property: JsonPropertyName("shift_date")] string? ShiftDate,
    [property: JsonPropertyName("level")] int? Level,
    [property: JsonPropertyName("speed")] decimal? Speed,
    [property: JsonPropertyName("is_followed_up")] bool IsFollowedUp,
    [property: JsonPropertyName("latitude")] decimal? Latitude,
    [property: JsonPropertyName("longitude")] decimal? Longitude,
    [property: JsonPropertyName("geofence_id")] string? GeofenceId,
    [property: JsonPropertyName("device_id")] string? DeviceId,
    [property: JsonPropertyName("driver_id")] string? DriverId,
    [property: JsonPropertyName("manual_verification_by")] string? ManualVerificationBy,
    [property: JsonPropertyName("manual_verification_time")] string? ManualVerificationTime,
    [property: JsonPropertyName("manual_verification_memo")] string? ManualVerificationMemo,
    [property: JsonPropertyName("manual_verification_waiting_duration")] int? ManualVerificationWaitingDuration,
    [property: JsonPropertyName("alarm_file")] List<AlarmFile>? AlarmFile,
    [property: JsonPropertyName("device")] DeviceInfo? Device,
    [property: JsonPropertyName("driver")] DriverInfo? Driver
);

public record AlarmFile(
    [property: JsonPropertyName("downUrl")] string DownUrl,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("fileGuid")] string FileGuid,
    [property: JsonPropertyName("fileType")] string? FileType,
    [property: JsonPropertyName("upload_at")] string? UploadAt
);

public record DeviceInfo(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("imei")] string Imei,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("group_name")] string GroupName
);

public record DriverInfo(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("name")] string? Name
);

public record PaginationInfo(
    [property: JsonPropertyName("total_count")] int TotalCount,
    [property: JsonPropertyName("total_pages")] int TotalPages,
    [property: JsonPropertyName("page")] int Page,
    [property: JsonPropertyName("page_size")] int PageSize
);

#endregion

#region Follow Up DTOs

public record FollowUpResponse(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("success")] bool Success,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] FollowUpData? Data,
    [property: JsonPropertyName("error")] string? Error
);

public record FollowUpData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("alarm_id")] string AlarmId,
    [property: JsonPropertyName("follow_up_category")] FollowUpCategory? FollowUpCategory,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("evidence")] string? Evidence,
    [property: JsonPropertyName("evidence_url")] string? EvidenceUrl,
    [property: JsonPropertyName("supervisor_name")] string? SupervisorName,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("created_at")] string CreatedAt
);

public record FollowUpCategory(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

#endregion
