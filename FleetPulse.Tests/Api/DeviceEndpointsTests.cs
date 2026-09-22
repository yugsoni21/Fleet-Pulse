using System.Net;
using System.Net.Http.Json;
using FleetPulse.Core.Models;
using Xunit;

namespace FleetPulse.Tests.Api;

public class DeviceEndpointsTests : IClassFixture<FleetPulseAppFactory>
{
    private readonly FleetPulseAppFactory _factory;

    public DeviceEndpointsTests(FleetPulseAppFactory factory) => _factory = factory;

    [Fact]
    public async Task GetDevices_ReturnsSuccessAndJsonArray()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devices");

        response.EnsureSuccessStatusCode();
        var devices = await response.Content.ReadFromJsonAsync<List<Device>>();
        Assert.NotNull(devices);
    }

    [Fact]
    public async Task GetDeviceById_ReturnsNotFound_ForUnknownId()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devices/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDeviceHistory_Succeeds_WhenTakeIsOmitted()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devices/1/history");

        response.EnsureSuccessStatusCode();
        var history = await response.Content.ReadFromJsonAsync<List<TelemetryReading>>();
        Assert.NotNull(history);
    }

    [Fact]
    public async Task GetDeviceHistory_RespectsTakeLimit()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/devices/1/history?take=5");

        response.EnsureSuccessStatusCode();
        var history = await response.Content.ReadFromJsonAsync<List<TelemetryReading>>();
        Assert.NotNull(history);
        Assert.True(history!.Count <= 5);
    }

    [Fact]
    public async Task GetAlerts_Succeeds_WhenTakeIsOmitted()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/alerts");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetAlertsSummary_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/alerts/summary");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetGeofences_ReturnsSeededZones()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/geofences");

        response.EnsureSuccessStatusCode();
        var geofences = await response.Content.ReadFromJsonAsync<List<Geofence>>();
        Assert.NotNull(geofences);
        Assert.NotEmpty(geofences!);
    }
}
