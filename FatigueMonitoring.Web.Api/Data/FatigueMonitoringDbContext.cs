using Microsoft.EntityFrameworkCore;
using FatigueMonitoring.Web.Api.Models;

namespace FatigueMonitoring.Web.Api.Data;

public class FatigueMonitoringDbContext(DbContextOptions<FatigueMonitoringDbContext> options) : DbContext(options)
{
    public DbSet<AI_FatigueEvent_T> FatigueEvents => Set<AI_FatigueEvent_T>();
    public DbSet<AI_DashboardSummary_T> DashboardSummaries => Set<AI_DashboardSummary_T>();
    public DbSet<AI_AreaDistribution_T> AreaDistributions => Set<AI_AreaDistribution_T>();
    public DbSet<AI_ActiveAlert_T> ActiveAlerts => Set<AI_ActiveAlert_T>();
    public DbSet<AI_DelayedFollowUp_T> DelayedFollowUps => Set<AI_DelayedFollowUp_T>();
    public DbSet<AI_RecurrentUnit_T> RecurrentUnits => Set<AI_RecurrentUnit_T>();
    public DbSet<AI_HighRiskArea_T> HighRiskAreas => Set<AI_HighRiskArea_T>();
    public DbSet<AI_TokenCache_T> TokenCache => Set<AI_TokenCache_T>();
    public DbSet<AI_SyncState_T> SyncStates => Set<AI_SyncState_T>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // AI_FatigueEvent_T
        modelBuilder.Entity<AI_FatigueEvent_T>(entity =>
        {
            entity.ToTable("AI_FatigueEvent_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.HasIndex(e => e.EventTime);
            entity.HasIndex(e => e.IsFollowedUp);
            entity.HasIndex(e => e.Area);
            entity.Property(e => e.Speed).HasPrecision(10, 2);
            entity.Property(e => e.Latitude).HasPrecision(12, 8);
            entity.Property(e => e.Longitude).HasPrecision(12, 8);
        });

        // AI_DashboardSummary_T
        modelBuilder.Entity<AI_DashboardSummary_T>(entity =>
        {
            entity.ToTable("AI_DashboardSummary_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FilterType);
        });

        // AI_AreaDistribution_T
        modelBuilder.Entity<AI_AreaDistribution_T>(entity =>
        {
            entity.ToTable("AI_AreaDistribution_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Area, e.Location });
        });

        // AI_ActiveAlert_T
        modelBuilder.Entity<AI_ActiveAlert_T>(entity =>
        {
            entity.ToTable("AI_ActiveAlert_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FatigueEventId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Area);
            entity.Property(e => e.Speed).HasPrecision(10, 2);
            entity.Property(e => e.Latitude).HasPrecision(12, 8);
            entity.Property(e => e.Longitude).HasPrecision(12, 8);
        });

        // AI_DelayedFollowUp_T
        modelBuilder.Entity<AI_DelayedFollowUp_T>(entity =>
        {
            entity.ToTable("AI_DelayedFollowUp_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.FatigueEventId);
            entity.HasIndex(e => e.DelayMinutes);
            entity.Property(e => e.Speed).HasPrecision(10, 2);
            entity.Property(e => e.Latitude).HasPrecision(12, 8);
            entity.Property(e => e.Longitude).HasPrecision(12, 8);
        });

        // AI_RecurrentUnit_T
        modelBuilder.Entity<AI_RecurrentUnit_T>(entity =>
        {
            entity.ToTable("AI_RecurrentUnit_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UnitName);
            entity.HasIndex(e => e.EventCount);
        });

        // AI_HighRiskArea_T
        modelBuilder.Entity<AI_HighRiskArea_T>(entity =>
        {
            entity.ToTable("AI_HighRiskArea_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Location);
            entity.HasIndex(e => e.EventCount);
        });

        // AI_TokenCache_T
        modelBuilder.Entity<AI_TokenCache_T>(entity =>
        {
            entity.ToTable("AI_TokenCache_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.TokenType);
        });

        // AI_SyncState_T
        modelBuilder.Entity<AI_SyncState_T>(entity =>
        {
            entity.ToTable("AI_SyncState_T");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.SyncType).IsUnique();
        });
    }
}
