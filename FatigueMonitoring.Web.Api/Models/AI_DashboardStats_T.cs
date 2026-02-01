using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FatigueMonitoring.Web.Api.Models;

[Table("AI_DashboardStats_T")]
public class AI_DashboardStats_T
{
    [Key]
    public int Id { get; set; }
    public string Area { get; set; } = "All"; // All, Mining, Hauling
    public int TotalAlarms { get; set; }
    public int FollowedUp { get; set; }
    public int WaitingFollowUp { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
