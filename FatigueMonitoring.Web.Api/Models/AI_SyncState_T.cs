namespace FatigueMonitoring.Web.Api.Models;

/// <summary>
/// Stores the synchronization state for external API data fetching
/// </summary>
public class AI_SyncState_T
{
    public int Id { get; set; }
    
    /// <summary>
    /// Type of sync (e.g., "ExternalApiEvents")
    /// </summary>
    public string SyncType { get; set; } = "ExternalApiEvents";
    
    /// <summary>
    /// Last successfully processed timestamp
    /// </summary>
    public DateTime LastSyncTime { get; set; }
    
    /// <summary>
    /// Number of records processed in last sync
    /// </summary>
    public int LastSyncRecordCount { get; set; }
    
    /// <summary>
    /// Status of last sync: Success, Failed, InProgress
    /// </summary>
    public string LastSyncStatus { get; set; } = "Success";
    
    /// <summary>
    /// Error message if last sync failed
    /// </summary>
    public string? LastSyncError { get; set; }
    
    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
