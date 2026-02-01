using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("AI_RecurrentUnit_T")]
public class AI_RecurrentUnit_T
{
    [Key]
    public int Id { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string OperatorName { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
