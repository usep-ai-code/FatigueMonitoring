using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("AI_ActiveAlert_T")]
public class AI_ActiveAlert_T
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EventId { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Operator { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty; // Mining or Hauling
    public string Location { get; set; } = string.Empty;
    public DateTime Time { get; set; }
    public string Status { get; set; } = "Open"; // Open or Followed Up
    public double Speed { get; set; }
    public int Count { get; set; } // Count of events for this unit today
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int OpenDurationMinutes { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
