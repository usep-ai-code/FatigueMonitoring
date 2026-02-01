using FatigueMonitoring.Web.Api.DTOs;

namespace FatigueMonitoring.Web.Api.Services;

public interface IExternalApiService
{
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
    Task<EventResponse?> GetEventsAsync(DateTime startDate, DateTime endDate, int page = 1, int pageSize = 100, string? filterColumn = null, string? filterValue = null, CancellationToken cancellationToken = default);
    Task<FollowUpResponse?> GetFollowUpAsync(string alarmGuid, CancellationToken cancellationToken = default);
}
