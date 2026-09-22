namespace FleetPulse.Core.Models;

/// <summary>
/// Snapshot of ingestion pipeline health, pushed to dashboards on every flush.
/// <paramref name="QueueDepth"/> is the live backlog in the bounded channel — it stays near
/// zero while the consumer keeps up and climbs when it can't, which is the backpressure
/// signal the whole pipeline is built around.
/// </summary>
public record PipelineStats(
    long TotalReadings,
    long TotalAlerts,
    int QueueDepth,
    double ReadingsPerSecond,
    int ActiveDevices);
