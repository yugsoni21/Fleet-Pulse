using FleetPulse.Core.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FleetPulse.Simulator;

/// <summary>
/// Producer side of the pipeline: simulates N virtual GO devices driving continuous loops
/// and writes their telemetry readings into the shared bounded channel.
/// </summary>
public class DeviceSimulatorService : BackgroundService
{
    private readonly TelemetryChannel _channel;
    private readonly ILogger<DeviceSimulatorService> _logger;
    private readonly SimulatorOptions _options;
    private readonly List<VirtualDevice> _devices = new();
    private readonly Random _random = new();

    public DeviceSimulatorService(
        TelemetryChannel channel,
        IOptions<SimulatorOptions> options,
        ILogger<DeviceSimulatorService> logger)
    {
        _channel = channel;
        _options = options.Value;
        _logger = logger;

        for (var i = 1; i <= _options.DeviceCount; i++)
        {
            var route = RouteCatalog.Routes[i % RouteCatalog.Routes.Count];
            _devices.Add(new VirtualDevice(i, route, _random.NextDouble()));
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickSeconds = _options.TickMilliseconds / 1000.0;

        _logger.LogInformation(
            "Simulator starting {DeviceCount} virtual devices across {RouteCount} routes at {TickMs}ms ticks",
            _devices.Count, RouteCatalog.Routes.Count, _options.TickMilliseconds);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.TickMilliseconds));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                foreach (var device in _devices)
                {
                    var reading = device.Advance(_random, tickSeconds);

                    // TryWrite is the fast path; WriteAsync blocks once the bounded channel
                    // is full, which is exactly the backpressure we want.
                    if (!_channel.Writer.TryWrite(reading))
                    {
                        await _channel.Writer.WriteAsync(reading, stoppingToken);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        finally
        {
            _channel.Writer.TryComplete();
        }
    }
}
