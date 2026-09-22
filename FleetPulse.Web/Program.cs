using System.Text.Json.Serialization;
using FleetPulse.Core.Channels;
using FleetPulse.Core.Models;
using FleetPulse.Core.Rules;
using FleetPulse.Data;
using FleetPulse.Ingestion;
using FleetPulse.Ingestion.Hubs;
using FleetPulse.Ingestion.Rules;
using FleetPulse.Simulator;
using FleetPulse.Web.Endpoints;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<FleetPulseDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.Configure<SimulatorOptions>(builder.Configuration.GetSection("Simulator"));

builder.Services.AddSingleton<TelemetryChannel>();

// Alert rules are plain singletons — adding a new alert type means adding one class here,
// with no changes to the ingestion loop.
builder.Services.AddSingleton<IAlertRule, SpeedingRule>();
builder.Services.AddSingleton<IAlertRule, HarshEventRule>();
builder.Services.AddSingleton<IAlertRule, GeofenceRule>();
builder.Services.AddSingleton<IAlertRule, IdleRule>();

builder.Services.AddHostedService<DeviceSimulatorService>();
builder.Services.AddHostedService<IngestionService>();

// Enums are serialized as strings on both transports so the dashboard can switch on
// "Speeding" / "Critical" rather than opaque integers.
builder.Services.AddSignalR().AddJsonProtocol(options =>
{
    options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FleetPulseDbContext>();
    db.Database.EnsureCreated();

    // The ingestion loop writes continuously while dashboard requests read. WAL lets those
    // overlap instead of readers hitting "database is locked".
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");

    if (!db.Geofences.Any())
    {
        db.Geofences.AddRange(
            new Geofence { Name = "Downtown Depot", CenterLatitude = 43.6532, CenterLongitude = -79.3832, RadiusMeters = 700 },
            new Geofence { Name = "Airport Yard", CenterLatitude = 43.6777, CenterLongitude = -79.6248, RadiusMeters = 1400 },
            new Geofence { Name = "North Distribution", CenterLatitude = 43.7400, CenterLongitude = -79.3400, RadiusMeters = 900 }
        );
        db.SaveChanges();
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<TelemetryHub>("/hubs/telemetry");

app.MapDeviceEndpoints();
app.MapAlertEndpoints();
app.MapGeofenceEndpoints();

app.Run();

// Exposed so WebApplicationFactory<Program> can boot this host in integration tests.
public partial class Program { }
