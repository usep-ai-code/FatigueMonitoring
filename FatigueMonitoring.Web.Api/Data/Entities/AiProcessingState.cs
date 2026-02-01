namespace FatigueMonitoring.Web.Api.Data.Entities;

public sealed class AiProcessingState
{
    public int Id { get; set; }
    public DateTime LastProcessedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
