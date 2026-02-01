using FatigueMonitoring.Web.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FatigueMonitoring.Web.Api.Data;

public sealed class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<ExternalEvent> ExternalEvents => Set<ExternalEvent>();
    public DbSet<AiKpi> AiKpis => Set<AiKpi>();
    public DbSet<AiActiveAlert> AiActiveAlerts => Set<AiActiveAlert>();
    public DbSet<AiAreaDistribution> AiAreaDistributions => Set<AiAreaDistribution>();
    public DbSet<AiDelayedAlert> AiDelayedAlerts => Set<AiDelayedAlert>();
    public DbSet<AiRecurrentUnit> AiRecurrentUnits => Set<AiRecurrentUnit>();
    public DbSet<AiHighRiskArea> AiHighRiskAreas => Set<AiHighRiskArea>();
    public DbSet<AiDeviceHealth> AiDeviceHealth => Set<AiDeviceHealth>();
    public DbSet<AiProcessingState> AiProcessingStates => Set<AiProcessingState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ExternalEvent>(entity =>
        {
            entity.ToTable("RawEvents");
            entity.HasKey(e => e.ExternalId);
            entity.Property(e => e.ExternalId).HasMaxLength(64);
            entity.Property(e => e.Identity).HasMaxLength(128);
            entity.Property(e => e.Name).HasMaxLength(128);
            entity.Property(e => e.AlarmType).HasMaxLength(32);
            entity.Property(e => e.DeviceName).HasMaxLength(64);
            entity.Property(e => e.DeviceGroupName).HasMaxLength(128);
            entity.Property(e => e.OperatorName).HasMaxLength(128);
            entity.Property(e => e.Area).HasMaxLength(32);
            entity.Property(e => e.LocationLabel).HasMaxLength(256);
            entity.Property(e => e.SpeedKph).HasColumnType("decimal(10,2)");
            entity.HasIndex(e => e.DeviceTime);
            entity.HasIndex(e => e.IsFollowedUp);
            entity.HasIndex(e => e.Area);
        });

        modelBuilder.Entity<AiKpi>(entity =>
        {
            entity.ToTable("AI_Kpi_T");
            entity.Property(e => e.Area).HasMaxLength(32);
            entity.HasIndex(e => e.Area);
        });

        modelBuilder.Entity<AiActiveAlert>(entity =>
        {
            entity.ToTable("AI_ActiveAlerts_T");
            entity.Property(e => e.AlarmId).HasMaxLength(64);
            entity.Property(e => e.Unit).HasMaxLength(64);
            entity.Property(e => e.Operator).HasMaxLength(128);
            entity.Property(e => e.Area).HasMaxLength(32);
            entity.Property(e => e.Location).HasMaxLength(256);
            entity.Property(e => e.AlarmType).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(32);
            entity.Property(e => e.SpeedKph).HasColumnType("decimal(10,2)");
            entity.HasIndex(e => e.Area);
        });

        modelBuilder.Entity<AiAreaDistribution>(entity =>
        {
            entity.ToTable("AI_AreaDistribution_T");
            entity.Property(e => e.Area).HasMaxLength(32);
            entity.Property(e => e.Location).HasMaxLength(256);
            entity.HasIndex(e => e.Area);
        });

        modelBuilder.Entity<AiDelayedAlert>(entity =>
        {
            entity.ToTable("AI_DelayedAlerts_T");
            entity.Property(e => e.AlarmId).HasMaxLength(64);
            entity.Property(e => e.Unit).HasMaxLength(64);
            entity.Property(e => e.Operator).HasMaxLength(128);
            entity.Property(e => e.Area).HasMaxLength(32);
            entity.Property(e => e.Location).HasMaxLength(256);
            entity.Property(e => e.AlarmType).HasMaxLength(64);
            entity.Property(e => e.Status).HasMaxLength(32);
            entity.Property(e => e.SpeedKph).HasColumnType("decimal(10,2)");
            entity.HasIndex(e => e.Area);
        });

        modelBuilder.Entity<AiRecurrentUnit>(entity =>
        {
            entity.ToTable("AI_RecurrentUnits_T");
            entity.Property(e => e.Unit).HasMaxLength(64);
            entity.Property(e => e.Operator).HasMaxLength(128);
            entity.Property(e => e.Area).HasMaxLength(32);
            entity.HasIndex(e => e.Area);
        });

        modelBuilder.Entity<AiHighRiskArea>(entity =>
        {
            entity.ToTable("AI_HighRiskAreas_T");
            entity.Property(e => e.Area).HasMaxLength(32);
            entity.Property(e => e.Location).HasMaxLength(256);
            entity.HasIndex(e => e.Area);
        });

        modelBuilder.Entity<AiDeviceHealth>(entity =>
        {
            entity.ToTable("AI_DeviceHealth_T");
            entity.Property(e => e.Coverage).HasColumnType("decimal(18,2)");
        });

        modelBuilder.Entity<AiProcessingState>(entity =>
        {
            entity.ToTable("AI_ProcessingState_T");
        });
    }
}
