using Microsoft.EntityFrameworkCore;

namespace FatigueMonitoring.Web.Api;

public sealed class DashboardDbContext(DbContextOptions<DashboardDbContext> options) : DbContext(options)
{
    public DbSet<RawEvent> RawEvents => Set<RawEvent>();
    public DbSet<DashboardSnapshot> DashboardSnapshots => Set<DashboardSnapshot>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RawEvent>(entity =>
        {
            entity.ToTable("RawEvents");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ExternalId).IsUnique();
            entity.Property(e => e.ExternalId).HasMaxLength(64).IsRequired();
            entity.Property(e => e.DeviceName).HasMaxLength(128);
            entity.Property(e => e.DeviceGroup).HasMaxLength(128);
            entity.Property(e => e.DeviceId).HasMaxLength(64);
            entity.Property(e => e.OperatorName).HasMaxLength(128);
            entity.Property(e => e.AlarmType).HasMaxLength(64);
            entity.Property(e => e.AlarmName).HasMaxLength(128);
            entity.Property(e => e.ManualVerificationMemo).HasMaxLength(256);
            entity.Property(e => e.GeofenceName).HasMaxLength(128);
        });

        modelBuilder.Entity<DashboardSnapshot>(entity =>
        {
            entity.ToTable("AI_DashboardSnapshot");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PayloadJson).IsRequired();
            entity.Property(e => e.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<SyncState>(entity =>
        {
            entity.ToTable("SyncStates");
            entity.HasKey(e => e.Id);
        });
    }
}
