using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("SyncMetadata")]
public class SyncMetadata
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string SyncKey { get; set; } = string.Empty;
    
    public DateTime LastSyncTime { get; set; }
    
    public DateTime? NextSyncTime { get; set; }
    
    public string? Status { get; set; } // Success, Running, Failed
    
    public string? ErrorMessage { get; set; }
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
