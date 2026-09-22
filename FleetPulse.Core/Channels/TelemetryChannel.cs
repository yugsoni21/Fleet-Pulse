using System.Threading.Channels;
using FleetPulse.Core.Models;

namespace FleetPulse.Core.Channels;

/// <summary>
/// Bounded, single-reader/multi-writer channel connecting the simulator (producer) to the
/// ingestion pipeline (consumer). Bounded so a slow consumer applies backpressure to
/// producers instead of the process growing memory unbounded.
/// </summary>
public class TelemetryChannel
{
    public const int Capacity = 10_000;

    private readonly Channel<TelemetryReading> _channel;

    public TelemetryChannel()
    {
        _channel = Channel.CreateBounded<TelemetryReading>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ChannelWriter<TelemetryReading> Writer => _channel.Writer;

    public ChannelReader<TelemetryReading> Reader => _channel.Reader;

    /// <summary>Readings currently buffered and waiting to be consumed.</summary>
    public int QueueDepth => _channel.Reader.Count;
}
