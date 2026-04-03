using IoTHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoTHub.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TelemetryReading> TelemetryReadings => Set<TelemetryReading>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Device>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.HardwareId).IsUnique();
            e.Property(d => d.AlertThreshold).HasColumnType("float");
            e.Property(d => d.Latitude).HasColumnType("float");
            e.Property(d => d.Longitude).HasColumnType("float");
        });

        model.Entity<TelemetryReading>(e =>
        {
            e.HasKey(r => r.Id);
            e.Property(r => r.Id).ValueGeneratedOnAdd();
            e.HasIndex(r => new { r.DeviceId, r.Timestamp });
            e.HasOne(r => r.Device).WithMany(d => d.Readings).HasForeignKey(r => r.DeviceId);
        });

        model.Entity<Alert>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => new { a.DeviceId, a.TriggeredAt });
            e.HasOne(a => a.Device).WithMany(d => d.Alerts).HasForeignKey(a => a.DeviceId);
        });
    }
}
