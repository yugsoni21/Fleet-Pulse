using FleetPulse.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetPulse.Web.Endpoints;

public static class AlertEndpoints
{
    public static void MapAlertEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/alerts");

        group.MapGet("/", async (int? take, FleetPulseDbContext db) =>
        {
            var limit = Math.Clamp(take ?? 100, 1, 2000);

            var alerts = await db.Alerts
                .AsNoTracking()
                .OrderByDescending(a => a.TimestampUtc)
                .Take(limit)
                .ToListAsync();

            return Results.Ok(alerts);
        });

        group.MapGet("/summary", async (FleetPulseDbContext db) =>
        {
            var summary = await db.Alerts
                .AsNoTracking()
                .GroupBy(a => a.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToListAsync();

            return Results.Ok(summary);
        });
    }
}
