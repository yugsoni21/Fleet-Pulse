using FleetPulse.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Web.Endpoints;

public static class GeofenceEndpoints
{
    public static void MapGeofenceEndpoints(this WebApplication app)
    {
        app.MapGet("/api/geofences", async (FleetPulseDbContext db) =>
        {
            var geofences = await db.Geofences.AsNoTracking().OrderBy(g => g.Id).ToListAsync();
            return Results.Ok(geofences);
        });
    }
}
