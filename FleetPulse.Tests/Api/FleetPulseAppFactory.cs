using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetPulse.Tests.Api;

/// <summary>
/// Boots the real host against a throwaway SQLite file so integration tests never touch the
/// development database, and with a tiny fleet so the simulator doesn't dominate the run.
/// </summary>
public class FleetPulseAppFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"fleetpulse_test_{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Simulator:DeviceCount", "2");
        builder.UseSetting("Simulator:TickMilliseconds", "250");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        foreach (var suffix in new[] { "", "-wal", "-shm" })
        {
            try
            {
                File.Delete(_dbPath + suffix);
            }
            catch (IOException)
            {
                // Best-effort cleanup of a temp file; a leftover doesn't fail the test run.
            }
        }
    }
}
