using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("AI_HighRiskArea_T")]
public class AI_HighRiskArea_T
{
    [Key]
    public int Id { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty; // Mining or Hauling
    public int EventCount { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
