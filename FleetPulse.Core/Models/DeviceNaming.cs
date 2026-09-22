namespace FleetPulse.Core.Models;

/// <summary>
/// Both the simulator and the ingestion pipeline derive device serials/names from the
/// device id independently, so no shared device roster needs to be passed between them.
/// </summary>
public static class DeviceNaming
{
    public static string SerialFor(int deviceId) => $"GT-{deviceId:D5}";

    public static string NameFor(int deviceId) => $"Vehicle {deviceId:D3}";
}
