namespace FatigueMonitoring.Web.Api.Models;

// Login Request/Response
public class LoginRequest(string username, string password)
{
    public string Username { get; set; } = username;
    public string Password { get; set; } = password;
}

public class LoginResponse
{
    public int Code { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public LoginData? Data { get; set; }
}

public class LoginData
{
    public string? Access_Token { get; set; }
    public string? Token { get; set; }
    public string? Pid { get; set; }
    public CompanyInfo? Company { get; set; }
}

public class CompanyInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

// Event Request/Response
public class EventRequest
{
    public string Range_Date_Start { get; set; } = string.Empty;
    public string Range_Date_End { get; set; } = string.Empty;
    public string Range_Date_Columns { get; set; } = "device_time";
    public int Page { get; set; } = 1;
    public int Page_Size { get; set; } = 100;
    public string? Filter_Columns { get; set; }
    public string? Filter_Value { get; set; }
}

public class EventResponse
{
    public int Code { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public EventData? Data { get; set; }
}

public class EventData
{
    public List<EventItem>? List { get; set; }
    public PaginationInfo? Pagination { get; set; }
}

public class EventItem
{
    public string? Id { get; set; }
    public string? Identity { get; set; }
    public string? Name { get; set; }
    public string? Alarm_Type { get; set; }
    public string? Time { get; set; }
    public string? Server_Time { get; set; }
    public string? Shift { get; set; }
    public string? Shift_Date { get; set; }
    public int Level { get; set; }
    public double Speed { get; set; }
    public bool Is_Followed_Up { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Geofence_Id { get; set; }
    public string? Device_Id { get; set; }
    public string? Driver_Id { get; set; }
    public string? Manual_Verification_By { get; set; }
    public string? Manual_Verification_Time { get; set; }
    public string? Manual_Verification_Memo { get; set; }
    public bool TakeType { get; set; }
    public int Manual_Verification_Waiting_Duration { get; set; }
    public int Satellites { get; set; }
    public string? Upload_At { get; set; }
    public string? Updated_At { get; set; }
    public DeviceInfo? Device { get; set; }
    public DriverInfo? Driver { get; set; }
    public GeofenceInfo? Geofence { get; set; }
}

public class DeviceInfo
{
    public string? Id { get; set; }
    public string? Imei { get; set; }
    public string? Name { get; set; }
    public string? Group_Name { get; set; }
}

public class DriverInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class GeofenceInfo
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}

public class PaginationInfo
{
    public int Total_Count { get; set; }
    public int Total_Pages { get; set; }
    public int Page { get; set; }
    public int Page_Size { get; set; }
}

// Follow Up Response
public class FollowUpResponse
{
    public int Code { get; set; }
    public bool Success { get; set; }
    public string? Message { get; set; }
    public FollowUpData? Data { get; set; }
}

public class FollowUpData
{
    public string? Id { get; set; }
    public string? Alarm_Id { get; set; }
    public FollowUpCategory? Follow_Up_Category { get; set; }
    public string? Follow_Up_Category_Id { get; set; }
    public string? Description { get; set; }
    public string? Evidence { get; set; }
    public string? Evidence_Url { get; set; }
    public string? Supervisor_Name { get; set; }
    public string? Supervisor_Id { get; set; }
    public string? Date { get; set; }
    public string? Status { get; set; }
    public string? Created_At { get; set; }
}

public class FollowUpCategory
{
    public string? Id { get; set; }
    public string? Name { get; set; }
}
