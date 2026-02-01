using Microsoft.EntityFrameworkCore;
using FatigueMonitoring.Web.Api.Models;

namespace FatigueMonitoring.Web.Api.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    // Raw Data Tables
    public DbSet<RawEvent> RawEvents { get; set; }
    public DbSet<RawFollowUp> RawFollowUps { get; set; }

    // Aggregation Tables (AI_ prefix, _T suffix)
    public DbSet<AI_DashboardStats_T> AI_DashboardStats_T { get; set; }
    public DbSet<AI_ActiveAlert_T> AI_ActiveAlert_T { get; set; }
    public DbSet<AI_AreaDistribution_T> AI_AreaDistribution_T { get; set; }
    public DbSet<AI_RecurrentUnit_T> AI_RecurrentUnit_T { get; set; }
    public DbSet<AI_HighRiskArea_T> AI_HighRiskArea_T { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure indexes for better performance
        modelBuilder.Entity<RawEvent>()
            .HasIndex(e => e.Time);
        
        modelBuilder.Entity<RawEvent>()
            .HasIndex(e => e.IsFollowedUp);

        modelBuilder.Entity<RawEvent>()
            .HasIndex(e => e.DeviceId);

        modelBuilder.Entity<AI_ActiveAlert_T>()
            .HasIndex(a => a.Status);

        modelBuilder.Entity<AI_ActiveAlert_T>()
            .HasIndex(a => a.Area);

        modelBuilder.Entity<AI_DashboardStats_T>()
            .HasIndex(s => s.Area);
    }
}
