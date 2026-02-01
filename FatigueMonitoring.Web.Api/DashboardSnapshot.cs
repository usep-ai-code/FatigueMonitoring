namespace FatigueMonitoring.Web.Api;

public sealed class DashboardSnapshot
{
    public int Id { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string PayloadJson { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
