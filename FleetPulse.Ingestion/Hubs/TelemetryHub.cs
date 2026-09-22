using Microsoft.AspNetCore.SignalR;

namespace FleetPulse.Ingestion.Hubs;

/// <summary>
/// Push-only hub: the ingestion pipeline broadcasts "telemetry" and "alert" events to every
/// connected dashboard client. Clients never call back into it.
/// </summary>
public class TelemetryHub : Hub
{
}
