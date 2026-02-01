using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("RawFollowUps")]
public class RawFollowUp(string id, string alarmId, string followUpCategoryId)
{
    [Key]
    public string Id { get; set; } = id;
    public string AlarmId { get; set; } = alarmId;
    public string FollowUpCategoryId { get; set; } = followUpCategoryId;
    public string? FollowUpCategoryName { get; set; }
    public string? Description { get; set; }
    public string? Evidence { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? SupervisorName { get; set; }
    public string? SupervisorId { get; set; }
    public DateTime? Date { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
