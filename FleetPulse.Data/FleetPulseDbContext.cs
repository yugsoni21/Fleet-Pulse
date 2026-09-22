using FleetPulse.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FleetPulse.Data;

public class FleetPulseDbContext : DbContext
{
    /// <summary>
    /// SQLite has no native date type and refuses to ORDER BY a DateTimeOffset. Everything
    /// the pipeline records is already UTC, so storing the UTC DateTime keeps the domain
    /// model expressive while giving SQLite a column it can sort and index.
    /// </summary>
    private static readonly ValueConverter<DateTimeOffset, DateTime> UtcTimestampConverter = new(
        offset => offset.UtcDateTime,
        value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));

    public FleetPulseDbContext(DbContextOptions<FleetPulseDbContext> options) : base(options)
    {
    }

    public DbSet<Device> Devices => Set<Device>();
    public DbSet<TelemetryReading> TelemetryReadings => Set<TelemetryReading>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Geofence> Geofences => Set<Geofence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Device ids are assigned deterministically by the simulator (1..N), not by the
        // database, so identity generation must be turned off for this key.
        modelBuilder.Entity<Device>()
            .Property(d => d.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<Device>()
            .HasIndex(d => d.SerialNumber)
            .IsUnique();

        modelBuilder.Entity<TelemetryReading>()
            .HasIndex(r => new { r.DeviceId, r.TimestampUtc });

        modelBuilder.Entity<Alert>()
            .HasIndex(a => new { a.DeviceId, a.TimestampUtc });

        // /api/alerts pages the newest alerts fleet-wide, with no device filter.
        modelBuilder.Entity<Alert>()
            .HasIndex(a => a.TimestampUtc);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                {
                    property.SetValueConverter(UtcTimestampConverter);
                }
            }
        }
    }
}
