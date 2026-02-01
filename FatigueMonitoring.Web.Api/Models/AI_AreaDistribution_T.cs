using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("AI_AreaDistribution_T")]
public class AI_AreaDistribution_T
{
    [Key]
    public int Id { get; set; }
    public string Area { get; set; } = string.Empty; // Mining or Hauling
    public string Location { get; set; } = string.Empty;
    public int AlertCount { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
