using FleetPulse.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Web.Endpoints;

public static class DeviceEndpoints
{
    public static void MapDeviceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/devices");

        group.MapGet("/", async (FleetPulseDbContext db) =>
        {
            var devices = await db.Devices
                .AsNoTracking()
                .OrderBy(d => d.Id)
                .ToListAsync();

            return Results.Ok(devices);
        });

        group.MapGet("/{id:int}", async (int id, FleetPulseDbContext db) =>
        {
            var device = await db.Devices.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
            return device is null ? Results.NotFound() : Results.Ok(device);
        });

        // `take` is nullable so the parameter stays optional — a non-nullable int would make
        // the query string required and return 400 when it is omitted.
        group.MapGet("/{id:int}/history", async (int id, int? take, FleetPulseDbContext db) =>
        {
            var limit = Math.Clamp(take ?? 200, 1, 2000);

            var history = await db.TelemetryReadings
                .AsNoTracking()
                .Where(r => r.DeviceId == id)
                .OrderByDescending(r => r.TimestampUtc)
                .Take(limit)
                .ToListAsync();

            return Results.Ok(history);
        });
    }
}
