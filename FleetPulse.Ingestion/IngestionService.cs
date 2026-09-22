using System.Diagnostics;
using FleetPulse.Core.Channels;
using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;
using FleetPulse.Data;
using FleetPulse.Ingestion.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FleetPulse.Ingestion;

/// <summary>
/// Consumer side of the pipeline: drains the shared channel, runs every reading through the
/// registered <see cref="IAlertRule"/>s, batches writes to SQLite, and fans out live updates
/// to dashboard clients over SignalR.
/// </summary>
public class IngestionService : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(500);

    private readonly TelemetryChannel _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEnumerable<IAlertRule> _rules;
    private readonly IHubContext<TelemetryHub> _hub;
    private readonly ILogger<IngestionService> _logger;

    private readonly Dictionary<int, Device> _deviceCache = new();
    private long _totalReadings;
    private long _totalAlerts;

    public IngestionService(
        TelemetryChannel channel,
        IServiceScopeFactory scopeFactory,
        IEnumerable<IAlertRule> rules,
        IHubContext<TelemetryHub> hub,
        ILogger<IngestionService> logger)
    {
        _channel = channel;
        _scopeFactory = scopeFactory;
        _rules = rules;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FleetPulseDbContext>();
        var geofences = await db.Geofences.AsNoTracking().ToListAsync(stoppingToken);

        var readingBuffer = new List<TelemetryReading>(BatchSize);
        var alertBuffer = new List<Alert>();

        var sinceLastFlush = Stopwatch.StartNew();

        try
        {
            await foreach (var reading in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                readingBuffer.Add(reading);

                var context = new RuleEvaluationContext
                {
                    Reading = reading,
                    Geofences = geofences
                };

                foreach (var rule in _rules)
                {
                    alertBuffer.AddRange(rule.Evaluate(context));
                }

                if (readingBuffer.Count < BatchSize && sinceLastFlush.Elapsed < FlushInterval)
                {
                    continue;
                }

                await FlushAsync(db, readingBuffer, alertBuffer, sinceLastFlush.Elapsed, stoppingToken);

                readingBuffer.Clear();
                alertBuffer.Clear();
                sinceLastFlush.Restart();
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task FlushAsync(
        FleetPulseDbContext db,
        List<TelemetryReading> readings,
        List<Alert> alerts,
        TimeSpan window,
        CancellationToken ct)
    {
        foreach (var reading in readings)
        {
            await TrackDeviceStateAsync(db, reading, ct);
        }

        db.TelemetryReadings.AddRange(readings);

        if (alerts.Count > 0)
        {
            db.Alerts.AddRange(alerts);
        }

        await db.SaveChangesAsync(ct);

        // This DbContext lives for the lifetime of the service, so saved readings/alerts must
        // be detached or the change tracker grows without bound and every save gets slower.
        // Devices stay tracked on purpose — they are long-lived rows we keep mutating.
        DetachSaved<TelemetryReading>(db);
        DetachSaved<Alert>(db);

        _totalReadings += readings.Count;
        _totalAlerts += alerts.Count;

        var perSecond = window.TotalSeconds > 0
            ? Math.Round(readings.Count / window.TotalSeconds, 1)
            : 0;

        var stats = new PipelineStats(
            _totalReadings,
            _totalAlerts,
            _channel.QueueDepth,
            perSecond,
            _deviceCache.Count);

        await _hub.Clients.All.SendAsync("telemetry", readings, ct);

        if (alerts.Count > 0)
        {
            await _hub.Clients.All.SendAsync("alerts", alerts, ct);
        }

        await _hub.Clients.All.SendAsync("stats", stats, ct);

        _logger.LogDebug(
            "Flushed {ReadingCount} readings / {AlertCount} alerts (queue depth {QueueDepth}, total {Total})",
            readings.Count, alerts.Count, stats.QueueDepth, _totalReadings);
    }

    private static void DetachSaved<TEntity>(FleetPulseDbContext db) where TEntity : class
    {
        foreach (var entry in db.ChangeTracker.Entries<TEntity>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private async Task TrackDeviceStateAsync(FleetPulseDbContext db, TelemetryReading reading, CancellationToken ct)
    {
        if (!_deviceCache.TryGetValue(reading.DeviceId, out var device))
        {
            device = await db.Devices.FirstOrDefaultAsync(d => d.Id == reading.DeviceId, ct);

            if (device is null)
            {
                device = new Device
                {
                    Id = reading.DeviceId,
                    SerialNumber = DeviceNaming.SerialFor(reading.DeviceId),
                    Name = DeviceNaming.NameFor(reading.DeviceId),
                    VehicleType = "Van"
                };
                db.Devices.Add(device);
            }

            _deviceCache[reading.DeviceId] = device;
        }

        device.CurrentLatitude = reading.Latitude;
        device.CurrentLongitude = reading.Longitude;
        device.CurrentSpeedKph = reading.SpeedKph;
        device.CurrentHeadingDegrees = reading.HeadingDegrees;
        device.IsIdle = reading.SpeedKph < 1.0;
        device.LastSeenUtc = reading.TimestampUtc;
    }
}
